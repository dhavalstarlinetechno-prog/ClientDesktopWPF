using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using ClientDesktop.Infrastructure.Logger; // Your Logger Namespace
using ClientDesktop.Core.Config; // Assuming AppConfig is here

namespace ClientDesktop.ViewModel
{
    public class JournalViewModel
    {
        // Collection that notifies the UI when items are added
        public ObservableCollection<JournalLogModel> Journals { get; set; }

        // Lock object for thread safety
        private readonly object _lock = new object();

        public JournalViewModel()
        {
            Journals = new ObservableCollection<JournalLogModel>();

            // Allow accessing collection from cross-threads (optional but good for WPF)
            BindingOperations.EnableCollectionSynchronization(Journals, _lock);

            // 1. Load purana logs from file
            LoadExistingLogs();

            // 2. Listen for naye logs (Live Updates)
            FileLogger.OnLogReceived += HandleNewLog;
        }

        private void LoadExistingLogs()
        {
            try
            {
                string logDir = Path.Combine(Directory.GetParent(AppConfig.AppDataPath).FullName, "Logs");
                string fileName = $"{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(logDir, fileName);

                if (File.Exists(fullPath))
                {
                    var lines = File.ReadAllLines(fullPath);
                    foreach (var line in lines)
                    {
                        ParseAndAddLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading logs: {ex.Message}");
            }
        }

        private void HandleNewLog(string time, string source, string message)
        {
            // UI must be updated on the Main Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                Journals.Insert(0, new JournalLogModel
                {
                    Time = time,
                    Source = source,
                    Message = message
                });
            });
        }

        private void ParseAndAddLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Your FileLogger uses '\t' (Tab) to separate items
            var parts = line.Split('\t');

            if (parts.Length >= 3)
            {
                // Add to the START of the list (Insert 0) so newest is top
                Journals.Insert(0, new JournalLogModel
                {
                    Time = parts[0],
                    Source = parts[1],
                    Message = parts[2]
                });
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