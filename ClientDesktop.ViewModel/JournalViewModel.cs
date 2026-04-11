using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config; // Assuming AppConfig is here
using ClientDesktop.Infrastructure.Logger; // Your Logger Namespace
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace ClientDesktop.ViewModel
{
    public class JournalViewModel : ViewModelBase
    {
        // Collection that notifies the UI when items are added
        public ObservableCollection<JournalLogModel> Journals { get; set; }

        // Lock object for thread safety
        private readonly object _lock = new object();

        public JournalViewModel()
        {
            try
            {
                Journals = new ObservableCollection<JournalLogModel>();

                // Thread safety for cross-thread collection updates
                BindingOperations.EnableCollectionSynchronization(Journals, _lock);

                // 1. Load logs completely in the BACKGROUND without freezing the UI!
                Task.Run(() => LoadExistingLogsAsync());

                // 2. Listen for new logs
                FileLogger.OnLogReceived += HandleNewLog;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(JournalViewModel), ex);
            }
        }

        private void LoadExistingLogsAsync()
        {
            try
            {
                string logDir = Path.Combine(Directory.GetParent(AppConfig.AppDataPath).FullName, "Logs");
                string fileName = $"{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(logDir, fileName);

                if (File.Exists(fullPath))
                {
                    // Read all lines in the background thread
                    var lines = File.ReadAllLines(fullPath);

                    // Prepare a temporary list first so we don't spam the UI with updates
                    var tempList = new List<JournalLogModel>();

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split('\t');
                        if (parts.Length >= 3)
                        {
                            tempList.Add(new JournalLogModel
                            {
                                Time = parts[0],
                                Source = parts[1],
                                Message = parts[2]
                            });
                        }
                    }

                    // Reverse to put newest on top
                    tempList.Reverse();

                    SafeUIInvoke(() =>
                    {
                        foreach (var log in tempList)
                        {
                            Journals.Add(log);
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadExistingLogsAsync), ex);
            }
        }

        private void HandleNewLog(string time, string source, string message)
        {
            try
            {
                // UI must be updated on the Main Thread
                SafeUIInvoke(() =>
                {
                    Journals.Insert(0, new JournalLogModel
                    {
                        Time = time,
                        Source = source,
                        Message = message
                    });
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleNewLog), ex);
            }
        }

        private void ParseAndAddLine(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line)) return;

                // Your FileLogger uses '\t' (Tab) to separate items
                var parts = line.Split('\t');

                if (parts.Length >= 3)
                {
                    SafeUIInvoke(() =>
                    {
                        Journals.Insert(0, new JournalLogModel
                        {
                            Time = parts[0],
                            Source = parts[1],
                            Message = parts[2]
                        });
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ParseAndAddLine), ex);
            }
        }
    }

    public class JournalLogModel
    {
        public string Time { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }
}