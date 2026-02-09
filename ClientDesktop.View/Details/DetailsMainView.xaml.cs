using ClientDesktop.Infrastructure; // SessionManager ke liye
using ClientDesktop.Infrastructure.Services;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class DetailsMainView : UserControl
    {
        public DetailsMainView()
        {
            InitializeComponent();

            UpdateTabsVisibility(false);

            // 2. Events Subscribe karo
            SessionManager.OnLoginSuccess += () => Dispatcher.Invoke(() => UpdateTabsVisibility(true));
            SessionManager.OnLogout += () => Dispatcher.Invoke(() => UpdateTabsVisibility(false));
        }

        private void UpdateTabsVisibility(bool isLoggedIn)
        {
            if (isLoggedIn)
            {
                if (TabPosition != null) TabPosition.Visibility = Visibility.Visible;
                if (TabHistory != null) TabHistory.Visibility = Visibility.Visible;

                if (MainTabControl != null && TabPosition != null)
                {
                    MainTabControl.SelectedItem = TabPosition;
                }
            }
            else
            {
                if (TabPosition != null) TabPosition.Visibility = Visibility.Collapsed;
                if (TabHistory != null) TabHistory.Visibility = Visibility.Collapsed;

                if (MainTabControl != null && TabJournal != null)
                {
                    MainTabControl.SelectedItem = TabJournal;
                }
            }
        }
    }
}