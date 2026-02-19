using ClientDesktop.Infrastructure; // _sessionService ke liye
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class DetailsMainView : UserControl
    {
        private SessionService _sessionService;

        public DetailsMainView()
        {
            InitializeComponent();

            UpdateTabsVisibility(false);

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();

                if (_sessionService != null)
                {
                    _sessionService.OnLoginSuccess += () => Dispatcher.Invoke(() => UpdateTabsVisibility(true));
                    _sessionService.OnLogout += () => Dispatcher.Invoke(() => UpdateTabsVisibility(false));
                }
            }
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