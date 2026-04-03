using ClientDesktop.Core.Events;
using ClientDesktop.Infrastructure.Logger;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientDesktop.Infrastructure.Realtime
{
    /// <summary>
    /// Manages the SignalR connection, auto-reconnection, and listens to OS-level network/power events via Messenger.
    /// </summary>
    public class SignalRManager
    {
        #region Fields

        private readonly HubConnection _connection;
        private readonly System.Timers.Timer _connectionMonitorTimer;
        private bool _isDisposing = false;

        #endregion

        #region Properties & Events

        /// <summary>
        /// Gets the current state of the SignalR hub connection.
        /// </summary>
        public HubConnectionState ConnectionState => _connection.State;

        public event Action<string> OnMessageReceived;
        public event Action OnReconnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SignalRManager class and sets up Event Aggregator listeners.
        /// </summary>
        public SignalRManager(string hubUrl)
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                RegisterEvents();
                RegisterMessenger();

                // Fallback: Monitor connection every 10 seconds just in case OS events are missed
                _connectionMonitorTimer = new System.Timers.Timer(10000);
                _connectionMonitorTimer.Elapsed += async (s, e) => await MonitorConnectionAsync();
                _connectionMonitorTimer.AutoReset = true;
                _connectionMonitorTimer.Start();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SignalRManager), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the SignalR connection.
        /// </summary>
        public async Task StartAsync()
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    FileLogger.ApplicationLog(nameof(StartAsync), "Starting SignalR...");
                    await _connection.StartAsync();
                    FileLogger.ApplicationLog(nameof(StartAsync), "SignalR started successfully");
                }
                catch (HttpRequestException httpEx)
                {
                    FileLogger.ApplicationLog(nameof(StartAsync), httpEx);
                    OnConnectionError?.Invoke($"Network error: {httpEx.Message}");
                }
                catch (TaskCanceledException cancelEx)
                {
                    FileLogger.ApplicationLog(nameof(StartAsync), cancelEx);
                    OnConnectionError?.Invoke("SignalR connection timed out. Retrying...");

                    await Task.Delay(3000);
                    await StartAsync();
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(StartAsync), ex);
                    OnConnectionError?.Invoke($"Connection error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stops the active SignalR connection.
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    FileLogger.ApplicationLog(nameof(StopAsync), "Stopping connection...");
                    await _connection.StopAsync();
                    FileLogger.ApplicationLog(nameof(StopAsync), "Connection stopped");
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(StopAsync), ex);
            }
        }

        /// <summary>
        /// Safely invokes a method on the SignalR server, dropping it immediately if disconnected to avoid UI deadlocks.
        /// </summary>
        public async Task SafeInvokeAsync(string method, params object[] args)
        {
            try
            {
                // Fix for Deadlock: Never use while loop here. Just drop the request if not connected.
                if (_connection.State != HubConnectionState.Connected)
                {
                    FileLogger.ApplicationLog(nameof(SafeInvokeAsync), $"Cannot invoke {method} - connection is {_connection.State}. Request dropped.");
                    return;
                }

                if (method.Equals("AddToGroup"))
                {
                    await _connection.InvokeAsync(method, args);
                }
                else
                {
                    await _connection.InvokeAsync(method, args[0]);
                }

                FileLogger.ApplicationLog(nameof(SafeInvokeAsync), $"Successfully invoked {method}");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SafeInvokeAsync), ex);
            }
        }

        /// <summary>
        /// Cleans up resources, detaches from Event Aggregator, and disposes the connection.
        /// </summary>
        public async Task DisposeAsync()
        {
            try
            {
                _isDisposing = true;

                // Unsubscribe from all messages to prevent memory leaks
                WeakReferenceMessenger.Default.UnregisterAll(this);

                _connectionMonitorTimer?.Stop();
                _connectionMonitorTimer?.Dispose();

                await StopAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DisposeAsync), ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers listeners for System Power and Network events broadcasted by the SystemMonitorService.
        /// </summary>
        private void RegisterMessenger()
        {
            try
            {
                WeakReferenceMessenger.Default.Register<NetworkStateEvent>(this, async (recipient, message) =>
                {
                    try
                    {
                        if (message.IsConnected && !_isDisposing)
                        {
                            FileLogger.ApplicationLog("NetworkStateEvent_Callback", "Network restored via Messenger. Forcing immediate reconnect...");
                            await Task.Delay(1000); // Give NIC 1 second to stabilize IP
                            await MonitorConnectionAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("NetworkStateEvent_Callback", ex);
                    }
                });

                WeakReferenceMessenger.Default.Register<SystemPowerStateEvent>(this, async (recipient, message) =>
                {
                    try
                    {
                        if (message.IsWakingUp && !_isDisposing)
                        {
                            FileLogger.ApplicationLog("SystemPowerStateEvent_Callback", "PC Woke up from sleep via Messenger. Forcing immediate reconnect...");
                            await Task.Delay(2000); // Wait for Windows to reconnect Wi-Fi
                            await MonitorConnectionAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("SystemPowerStateEvent_Callback", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(RegisterMessenger), ex);
            }
        }

        /// <summary>
        /// Registers internal SignalR events like Closed, Reconnected, and Data receive.
        /// </summary>
        private void RegisterEvents()
        {
            try
            {
                _connection.Closed += async (error) =>
                {
                    try
                    {
                        if (_isDisposing) return;

                        FileLogger.ApplicationLog("Connection_Closed", $"Connection closed: {error?.Message}");
                        OnDisconnected?.Invoke();

                        await Task.Delay(5000);
                        if (!_isDisposing)
                        {
                            FileLogger.ApplicationLog("Connection_Closed", "Attempting to reconnect after connection closed...");
                            await StartAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("Connection_Closed", ex);
                    }
                };

                _connection.Reconnected += async (connectionId) =>
                {
                    try
                    {
                        FileLogger.ApplicationLog("Connection_Reconnected", $"Reconnected (ID: {connectionId})");
                        OnReconnected?.Invoke();
                        await Task.Delay(500);
                        await StartAsync();
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("Connection_Reconnected", ex);
                    }
                };

                _connection.On<string>("SendMessage", (data) =>
                {
                    try
                    {
                        OnMessageReceived?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("SendMessage_Callback", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(RegisterEvents), ex);
            }
        }

        /// <summary>
        /// Monitors the connection state and forces a reconnect if disconnected.
        /// </summary>
        private async Task MonitorConnectionAsync()
        {
            if (_isDisposing) return;

            try
            {
                switch (_connection.State)
                {
                    case HubConnectionState.Connected:
                        return;

                    case HubConnectionState.Reconnecting:
                        FileLogger.ApplicationLog(nameof(MonitorConnectionAsync), "Monitor: Still reconnecting…");
                        return;

                    case HubConnectionState.Disconnected:
                        FileLogger.ApplicationLog(nameof(MonitorConnectionAsync), "Monitor: Hard reconnect");
                        await StartAsync();

                        if (_connection.State == HubConnectionState.Connected)
                            OnReconnected?.Invoke();

                        return;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MonitorConnectionAsync), ex);
            }
        }

        #endregion
    }
}