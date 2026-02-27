using ClientDesktop.Core.Events;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;

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
                    Log("Starting SignalR...");
                    await _connection.StartAsync();
                    Log("SignalR started successfully");
                }
                catch (HttpRequestException httpEx)
                {
                    Log($"Network error starting SignalR: {httpEx.Message}");
                    OnConnectionError?.Invoke($"Network error: {httpEx.Message}");
                }
                catch (TaskCanceledException cancelEx)
                {
                    Log($"SignalR connection attempt timed out or was canceled: {cancelEx.Message}");
                    OnConnectionError?.Invoke("SignalR connection timed out. Retrying...");

                    await Task.Delay(3000);
                    await StartAsync();
                }
                catch (Exception ex)
                {
                    Log($"Error starting SignalR: {ex.Message}");
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
                    Log("Stopping connection...");
                    await _connection.StopAsync();
                    Log("Connection stopped");
                }
            }
            catch (Exception ex)
            {
                Log($"Error stopping connection: {ex.Message}");
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
                    Log($"Cannot invoke {method} - connection is {_connection.State}. Request dropped.");
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

                Log($"Successfully invoked {method}");
            }
            catch (Exception ex)
            {
                Log($"Invoke error for {method}: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up resources, detaches from Event Aggregator, and disposes the connection.
        /// </summary>
        public async Task DisposeAsync()
        {
            _isDisposing = true;

            // Unsubscribe from all messages to prevent memory leaks
            WeakReferenceMessenger.Default.UnregisterAll(this);

            _connectionMonitorTimer?.Stop();
            _connectionMonitorTimer?.Dispose();

            try
            {
                await StopAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log($"Error during disposal: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers listeners for System Power and Network events broadcasted by the SystemMonitorService.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<NetworkStateEvent>(this, async (recipient, message) =>
            {
                if (message.IsConnected && !_isDisposing)
                {
                    Log("Network restored via Messenger. Forcing immediate reconnect...");
                    await Task.Delay(1000); // Give NIC 1 second to stabilize IP
                    await MonitorConnectionAsync();
                }
            });

            WeakReferenceMessenger.Default.Register<SystemPowerStateEvent>(this, async (recipient, message) =>
            {
                if (message.IsWakingUp && !_isDisposing)
                {
                    Log("PC Woke up from sleep via Messenger. Forcing immediate reconnect...");
                    await Task.Delay(2000); // Wait for Windows to reconnect Wi-Fi
                    await MonitorConnectionAsync();
                }
            });
        }

        /// <summary>
        /// Registers internal SignalR events like Closed, Reconnected, and Data receive.
        /// </summary>
        private void RegisterEvents()
        {
            _connection.Closed += async (error) =>
            {
                if (_isDisposing) return;

                Log($"Connection closed: {error?.Message}");
                OnDisconnected?.Invoke();

                await Task.Delay(5000);
                if (!_isDisposing)
                {
                    Log("Attempting to reconnect after connection closed...");
                    await StartAsync();
                }
            };

            _connection.Reconnected += async (connectionId) =>
            {
                Log($"Reconnected (ID: {connectionId})");
                OnReconnected?.Invoke();
                await Task.Delay(500);
                await StartAsync();
            };

            _connection.On<string>("SendMessage", (data) =>
            {
                OnMessageReceived?.Invoke(data);
            });
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
                        Log("Monitor: Still reconnecting…");
                        return;

                    case HubConnectionState.Disconnected:
                        Log("Monitor: Hard reconnect");
                        await StartAsync();

                        if (_connection.State == HubConnectionState.Connected)
                            OnReconnected?.Invoke();

                        return;
                }
            }
            catch (Exception ex)
            {
                Log($"Monitor error: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs messages to the console with a timestamp.
        /// </summary>
        private void Log(string msg)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] SignalR Log: {msg}");
        }

        #endregion
    }
}