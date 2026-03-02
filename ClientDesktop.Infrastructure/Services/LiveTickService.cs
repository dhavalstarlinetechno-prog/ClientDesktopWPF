using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Globalization;

namespace ClientDesktop.Infrastructure.Services
{
    public class LiveTickService
    {
        #region Fields

        private readonly ConcurrentDictionary<string, int> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
        private SignalRManager _signalR;

        #endregion

        #region Events

        public event Action<TickData> OnTickReceived;
        public event Action OnReconnected;
        public event Action OnConnected;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes and starts the SignalR connection.
        /// </summary>
        public async Task InitializeAndStartAsync(string signalRUrl)
        {
            if (_signalR != null)
            {
                await _signalR.StopAsync();
                await _signalR.DisposeAsync();
                _signalR.OnMessageReceived -= HandleTick;
                _signalR.OnReconnected -= SignalR_OnReconnected;
            }

            _signalR = new SignalRManager(signalRUrl);
            _signalR.OnMessageReceived += HandleTick;
            _signalR.OnReconnected += SignalR_OnReconnected;

            await _signalR.StartAsync();

            if (_signalR.ConnectionState == HubConnectionState.Connected)
            {
                OnConnected?.Invoke();
            }
        }

        /// <summary>
        /// Subscribes to a specific symbol for real-time updates.
        /// </summary>
        public async Task<bool> SubscribeSymbolAsync(string symbolName)
        {
            if (string.IsNullOrWhiteSpace(symbolName) || _signalR == null) return false;

            if (_signalR.ConnectionState != HubConnectionState.Connected) return false;

            _subscriptions.AddOrUpdate(symbolName, 1, (key, count) => count + 1);

            if (_subscriptions[symbolName] == 1)
            {
                await _signalR.SafeInvokeAsync("GetLastTickBySymbol", symbolName);
                await _signalR.SafeInvokeAsync("AddToGroup", symbolName);
            }

            return true;
        }

        /// <summary>
        /// Unsubscribes from a specific symbol to stop receiving updates.
        /// </summary>
        public async Task<bool> UnsubscribeSymbolAsync(string symbolName)
        {
            if (string.IsNullOrWhiteSpace(symbolName) || _signalR == null) return false;

            if (_subscriptions.ContainsKey(symbolName))
            {
                _subscriptions[symbolName]--;

                if (_subscriptions[symbolName] <= 0)
                {
                    _subscriptions.TryRemove(symbolName, out _);
                    await _signalR.SafeInvokeAsync("RemoveFromGroup", symbolName);
                }
            }

            return true;
        }

        /// <summary>
        /// Stops the active SignalR connection and clears all subscriptions.
        /// </summary>
        public async Task StopConnectionAsync()
        {
            if (_signalR != null)
            {
                _signalR.OnMessageReceived -= HandleTick;
                _signalR.OnReconnected -= SignalR_OnReconnected;

                await _signalR.StopAsync();
                await _signalR.DisposeAsync();

                _signalR = null;
            }

            _subscriptions.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes incoming raw tick data and triggers the tick event.
        /// </summary>
        private void HandleTick(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return;
            var parts = rawData.Split('|');
            if (parts.Length < 12) return;

            bool TryParseDouble(string val, out double result) =>
                double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            long.TryParse(parts[7], out var updateTime);

            var tick = new TickData
            {
                SymbolName = parts[0],
                Bid = TryParseDouble(parts[2], out var bid) ? bid : 0,
                Ask = TryParseDouble(parts[3], out var ask) ? ask : 0,
                Ltp = TryParseDouble(parts[4], out var ltp) ? ltp : 0,
                High = TryParseDouble(parts[5], out var high) ? high : 0,
                Low = TryParseDouble(parts[6], out var low) ? low : 0,
                Open = TryParseDouble(parts[10], out var open) ? open : 0,
                PreviousClose = TryParseDouble(parts[11], out var close) ? close : 0,
                UpdateTime = updateTime
            };

            OnTickReceived?.Invoke(tick);
        }

        /// <summary>
        /// Handles the reconnection event and triggers the subscriber event.
        /// </summary>
        private void SignalR_OnReconnected()
        {
            _subscriptions.Clear();
            OnReconnected?.Invoke();
        }

        #endregion
    }
}