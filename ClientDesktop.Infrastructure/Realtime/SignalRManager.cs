using Microsoft.AspNetCore.SignalR.Client;

namespace ClientDesktop.Infrastructure.Realtime
{
    public class SignalRManager
    {
        #region Private Fields
        private readonly HubConnection _connection;
        private readonly System.Timers.Timer _connectionMonitorTimer;
        private bool _isDisposing = false;
        #endregion

        #region Public Properties And Events
        public HubConnectionState ConnectionState => _connection.State;

        public event Action<string> OnMessageReceived;
        public event Action OnReconnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;
        #endregion

        #region Constructor And Initialization
        public SignalRManager(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            RegisterEvents();

            // Monitor connection every 10 seconds
            _connectionMonitorTimer = new System.Timers.Timer(10000);
            _connectionMonitorTimer.Elapsed += async (s, e) => await MonitorConnectionAsync();
            _connectionMonitorTimer.AutoReset = true;
            _connectionMonitorTimer.Start();
        }
        #endregion

        #region Logging And Event Registration
        private void Log(string msg) =>
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] SignalR Log: {msg}");

        private void RegisterEvents()
        {
            _connection.Closed += async (error) =>
            {
                if (_isDisposing) return;

                Log($"Connection closed: {error?.Message}");
                OnDisconnected?.Invoke();

                // Wait before attempting reconnect
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
        #endregion

        #region Connection Lifecycle
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

                    // Optional: retry logic
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

        public async Task DisposeAsync()
        {
            _isDisposing = true;

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

        #region Invocation And Monitoring
        public async Task SafeInvokeAsync(string method, params object[] args)
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected)
                {
                    Log($"Cannot invoke {method} - connection is {_connection.State}. Retrying...");
                    await WaitForConnectionAsync();
                }

                if (method.Equals("AddToGroup"))
                    await _connection.InvokeAsync(method, args);
                else
                    await _connection.InvokeAsync(method, args[0]);

                Log($"Successfully invoked {method}");
            }
            catch (Exception ex)
            {
                Log($"Invoke error for {method}: {ex.Message}");
            }
        }

        private async Task WaitForConnectionAsync()
        {
            while (_connection.State != HubConnectionState.Connected)
            {
                Log("Waiting for connection...");
                await Task.Delay(500);
            }
            Log("SignalR connection established.");
        }

        // Connection watch (handles sleep / long Wi-Fi off)
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
        #endregion
    }
}
