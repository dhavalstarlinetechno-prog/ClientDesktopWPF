using ClientDesktop.Core.Events;
using CommunityToolkit.Mvvm.Messaging;
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
            RegisterMessenger();
        }

        /// <summary>
        /// Registers listeners for application-wide signals using the Event Aggregator.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    UpdateTabsVisibility(true);
                }
                else
                {
                    UpdateTabsVisibility(false);
                }
            });
        }

        private void UpdateTabsVisibility(bool isLoggedIn)
        {
            if (isLoggedIn)
            {
                if (TabPosition != null) TabPosition.Visibility = Visibility.Visible;
                if (TabHistory != null) TabHistory.Visibility = Visibility.Visible;
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