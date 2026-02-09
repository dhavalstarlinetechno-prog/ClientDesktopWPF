using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClientDesktop.ViewModel
{
    public class JournalViewModel : INotifyPropertyChanged
    {
        // Collection for Grid
        public ObservableCollection<JournalItem> Journals { get; set; }

        public JournalViewModel()
        {
            Journals = new ObservableCollection<JournalItem>();
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            Journals.Clear();

            // Dummy Rows
            Journals.Add(new JournalItem { Time = DateTime.Now, Dealer = "Dealer 1", Login = "User101", Request = "Order Placed", Answer = "Success" });
            Journals.Add(new JournalItem { Time = DateTime.Now.AddMinutes(-5), Dealer = "Dealer 2", Login = "User101", Request = "Modify Order", Answer = "Rejected" });
            Journals.Add(new JournalItem { Time = DateTime.Now.AddMinutes(-15), Dealer = "System", Login = "User101", Request = "Login", Answer = "Authorized" });
            Journals.Add(new JournalItem { Time = DateTime.Now.AddHours(-1), Dealer = "Dealer 1", Login = "User101", Request = "Logout", Answer = "Done" });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class JournalItem
    {
        public DateTime Time { get; set; }
        public string Dealer { get; set; }
        public string Login { get; set; }
        public string Request { get; set; }
        public string Answer { get; set; }
    }
}