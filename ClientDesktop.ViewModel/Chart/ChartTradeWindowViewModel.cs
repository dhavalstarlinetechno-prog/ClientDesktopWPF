using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ClientDesktop.ViewModel.Chart
{
    public class ChartTradeWindowViewModel : ViewModelBase
    {
        private readonly IChartService _chartService;
        private readonly LiveTickService _liveTickService;

        private int _symbolId;
        private string _symbolName;
        private string _masterSymbolName;
        private int _symbolDigits;

        private Func<string, Task> _executeScriptAsync;

        private bool _isTabActive = true;

        private long _lastSentTimeSeconds = 0;
        private double _lastSentClose = 0;

        private readonly ConcurrentQueue<string> _tickBuffer = new ConcurrentQueue<string>();

        private int _flushPending = 0;

        public const string VirtualHostName = "chartassets.local";

        public int SymbolId { get => _symbolId; set { SetProperty(ref _symbolId, value); OnPropertyChanged(nameof(ChartTitle)); } }
        public string SymbolName { get => _symbolName; set { SetProperty(ref _symbolName, value); OnPropertyChanged(nameof(ChartTitle)); } }

        public string MasterSymbolName => _masterSymbolName;
        public int SymbolDigits => _symbolDigits;
        public string ChartTitle => _symbolName;

        public string ChartUrl
        {
            get
            {
                var builder = new UriBuilder("http", VirtualHostName)
                {
                    Path = "index.html",
                    Query = $"symbol={Uri.EscapeDataString(_symbolName)}" +
                            $"&masterSymbol={Uri.EscapeDataString(_masterSymbolName)}" +
                            $"&digits={_symbolDigits}"
                };
                return builder.ToString();
            }
        }


        public ChartTradeWindowViewModel(int symbolId, string symbolName, string masterSymbolName, int symbolDigits, IChartService chartService, LiveTickService liveTickService)
        {
            _symbolId = symbolId;
            _symbolName = symbolName;
            _masterSymbolName = masterSymbolName ?? symbolName;
            _symbolDigits = symbolDigits;
            _chartService = chartService;
            _liveTickService = liveTickService;
        }

        public void RegisterScriptExecutor(Func<string, Task> executor)
        {
            _executeScriptAsync = executor;
            if (_liveTickService != null)
            {
                _liveTickService.OnTickReceived += HandleLiveTickFromService;              
                Task.Run(async () =>
                {
                    await _liveTickService.SubscribeSymbolAsync(_symbolName);
                });
            }
            Console.WriteLine($"[CHART] Subscribed to Live Tick Service: {_symbolName}");
        }

        private void HandleLiveTickFromService(TickData tick)
        {
            try
            {
                if (tick == null || _executeScriptAsync == null) return;
              
                string normIncoming = NormalizeSymbol(tick.SymbolName);
                string normCurrent = NormalizeSymbol(_symbolName);
                string normMaster = NormalizeSymbol(_masterSymbolName);

                if (!normIncoming.Equals(normCurrent, StringComparison.OrdinalIgnoreCase) &&
                    !normIncoming.Equals(normMaster, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                long timeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                double currentPrice = tick.Bid > 0 ? tick.Bid : (tick.Ltp > 0 ? tick.Ltp : tick.Ask);
                if (currentPrice <= 0) return;
              
                if (timeSeconds == _lastSentTimeSeconds && Math.Abs(currentPrice - _lastSentClose) < 0.000001)
                    return;

                double open = _lastSentClose > 0 ? _lastSentClose : currentPrice;
                double close = currentPrice;
                double high = Math.Max(open, close);
                double low = Math.Min(open, close);
                double volume = 0;

                var tickData = new
                {
                    time = timeSeconds,
                    open = open,
                    high = high,
                    low = low,
                    close = close,
                    volume = volume
                };

                _lastSentTimeSeconds = timeSeconds;
                _lastSentClose = close;

                string json = JsonConvert.SerializeObject(tickData);
                string escaped = json.Replace("\\", "\\\\").Replace("'", "\\'");

                string script = $"if(typeof window.updateTick==='function') window.updateTick('{escaped}')";

                if (_isTabActive)
                {                   
                    _executeScriptAsync(script);
                }
                else
                {                   
                    while (_tickBuffer.TryDequeue(out _)) { }
                    _tickBuffer.Enqueue(script);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHART] HandleLiveTickFromService Error: {ex.Message}");
            }
        }

        public async Task SetTabActive(bool isActive)
        {
            _isTabActive = isActive;
            if (isActive)
                await FlushTickBufferAsync();
        }

        public async Task FlushOnAppRestore()
        {
            await FlushTickBufferAsync();
        }

        private async Task FlushTickBufferAsync()
        {
            if (Interlocked.Exchange(ref _flushPending, 1) == 1)
                return;

            try
            {
                if (_executeScriptAsync == null) return;

                await Task.Delay(300);

                string lastScript = null;
                while (_tickBuffer.TryDequeue(out var s))
                    lastScript = s;

                if (lastScript != null)
                {
                    await _executeScriptAsync(lastScript);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHART] FlushBuffer Error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _flushPending, 0);
            }
        }

        public async Task<(string ReqId, string JsonData)> HandleHistoryRequestAsync(string jsonMessage)
        {
            string reqId = "unknown";
            try
            {
                var request = JsonConvert.DeserializeObject<HistoryRequest>(jsonMessage);
                if (request == null) return (reqId, "[]");

                reqId = request.ReqId ?? "unknown";

                var bars = await _chartService.GetHistoryAsync(
                    request.Symbol, request.From, request.To, request.Resolution);

                return (reqId, JsonConvert.SerializeObject(bars));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HISTORY] Error: {ex.Message}");
                return (reqId, "[]");
            }
        }

        private string NormalizeSymbol(string s)
            => (s ?? "").Replace("-", "").Replace("_", "").Replace("/", "").Trim().ToUpper();

        public void Cleanup()
        {           
            if (_liveTickService != null)
            {
                _liveTickService.OnTickReceived -= HandleLiveTickFromService;

                Task.Run(async () => {
                    await _liveTickService.UnsubscribeSymbolAsync(_symbolName);
                });
            }
            _executeScriptAsync = null;
        }
    }
    public class HistoryRequest
    {
        public string Type { get; set; }
        public string ReqId { get; set; }
        public string Symbol { get; set; }
        public string Resolution { get; set; }
        public long From { get; set; }
        public long To { get; set; }
    }
}
