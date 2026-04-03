using ClientDesktop.Core.Events;
using ClientDesktop.Infrastructure.Logger;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.Net.NetworkInformation;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Background service that listens to OS-level events (Network, Power) and broadcasts them via Event Aggregator.
    /// </summary>
    public class SystemMonitorService
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SystemMonitorService class and starts OS hooks.
        /// </summary>
        public SystemMonitorService()
        {
            try
            {
                StartMonitoring();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SystemMonitorService), ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers listeners for Windows OS events like Wi-Fi toggle and Sleep Mode.
        /// </summary>
        private void StartMonitoring()
        {
            try
            {
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(StartMonitoring), ex);
            }
        }

        /// <summary>
        /// Handles the OS network availability change event.
        /// </summary>
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            try
            {
                string status = e.IsAvailable ? "Network restored." : "Network connection lost.";
                FileLogger.Log("Network", status);

                // Broadcast event to all subscribers (ViewModels/Services)
                WeakReferenceMessenger.Default.Send(new NetworkStateEvent(e.IsAvailable));
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(NetworkChange_NetworkAvailabilityChanged), ex);
            }
        }

        /// <summary>
        /// Handles the OS power mode change event (Sleep/Wake).
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                if (e.Mode == PowerModes.Suspend)
                {
                    FileLogger.Log("System", "PC is going to sleep mode.");
                    WeakReferenceMessenger.Default.Send(new SystemPowerStateEvent(false));
                }
                else if (e.Mode == PowerModes.Resume)
                {
                    FileLogger.Log("System", "PC woke up from sleep mode.");
                    WeakReferenceMessenger.Default.Send(new SystemPowerStateEvent(true));
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SystemEvents_PowerModeChanged), ex);
            }
        }

        #endregion
    }
}