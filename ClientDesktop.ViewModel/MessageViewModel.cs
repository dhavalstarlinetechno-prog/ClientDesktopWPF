using ClientDesktop.Core.Base;
using ClientDesktop.ViewModel; // RelayCommand ke liye
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        // Collection for Grid
        public ObservableCollection<MessageItem> Messages { get; set; }

        // Date Filters
        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(); }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(); }
        }

        public ICommand FilterCommand { get; set; }

        public MessageViewModel()
        {
            Messages = new ObservableCollection<MessageItem>();
            FilterCommand = new RelayCommand(o => LoadDummyData());

            // Load Dummy Data directly
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            Messages.Clear();

            // Dummy Rows
            Messages.Add(new MessageItem { IsSelected = false, From = "Admin", Title = "Maintenance", Message = "Server maintenance at 12:00 AM", Date = DateTime.Now });
            Messages.Add(new MessageItem { IsSelected = false, From = "System", Title = "Login", Message = "New login detected from IP 192.168.1.1", Date = DateTime.Now.AddHours(-2) });
            Messages.Add(new MessageItem { IsSelected = true, From = "Broker", Title = "Margin Alert", Message = "Your margin level is below 100%", Date = DateTime.Now.AddDays(-1) });

            OnPropertyChanged(nameof(Messages));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MessageItem
    {
        public bool IsSelected { get; set; }
        public string From { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }
}