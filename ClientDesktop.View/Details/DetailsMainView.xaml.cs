using ClientDesktop.Core.Events;
using ClientDesktop.Infrastructure.Logger;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class DetailsMainView : UserControl
    {
        public DetailsMainView()
        {
            try
            {
                InitializeComponent();
                UpdateTabsVisibility(false);
                RegisterMessenger();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DetailsMainView), ex);
            }
        }

        /// <summary>
        /// Registers listeners for application-wide signals using the Event Aggregator.
        /// </summary>
        private void RegisterMessenger()
        {
            try
            {
                WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
                {
                    try
                    {
                        if (message.IsLoggedIn)
                        {
                            UpdateTabsVisibility(true);
                        }
                        else
                        {
                            UpdateTabsVisibility(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(RegisterMessenger) + "_Callback", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(RegisterMessenger), ex);
            }
        }

        private void UpdateTabsVisibility(bool isLoggedIn)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateTabsVisibility), ex);
            }
        }
    }
}