using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the Market Watch view, including real-time ticks and symbol visibility.
    /// </summary>
    public class MarketWatchViewModel : ViewModelBase
    {
        #region Fields

        private readonly MarketWatchService _marketWatchService;
        private readonly SessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly LiveTickService _liveTickService;

        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private readonly HashSet<string> _nativeVisibleSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _currentlySubscribed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int _debounceId = 0;
        private string _currentTime;
        private string _searchText;
        private int _selectedFontSize;
        private string _newSymbolSearchText;
        private string _symbolCountText;
        private bool _isSuggestionOpen;
        private ICollectionView _marketView;
        private MarketWatchSymbols _selectedMarketItem;

        private bool _showLtp = false;
        private bool _showHighLow = false;
        private bool _showOpen = false;
        private bool _showClose = false;
        private bool _showSpread = false;
        private bool _showDcp = false;
        private bool _showDcv = false;
        private bool _showTime = true;

        #endregion

        #region Properties

        public ObservableCollection<MarketWatchSymbols> MarketWatchSymbolsCollection { get; set; }
        public ObservableCollection<MarketWatchSymbols> HiddenSymbolsCollection { get; set; }
        public ObservableCollection<MarketWatchSymbols> SuggestedSymbols { get; set; }
        public ObservableCollection<int> FontSizes { get; set; }

        public ICollectionView MarketView => _marketView;

        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _marketView.Refresh();
            }
        }

        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set { _selectedFontSize = value; OnPropertyChanged(); }
        }

        public string NewSymbolSearchText
        {
            get => _newSymbolSearchText;
            set
            {
                SetProperty(ref _newSymbolSearchText, value);
                SearchHiddenSymbols(value);
            }
        }

        public string SymbolCountText
        {
            get => _symbolCountText;
            set => SetProperty(ref _symbolCountText, value);
        }

        public bool IsSuggestionOpen
        {
            get => _isSuggestionOpen;
            set => SetProperty(ref _isSuggestionOpen, value);
        }

        public MarketWatchSymbols SelectedMarketItem
        {
            get => _selectedMarketItem;
            set => SetProperty(ref _selectedMarketItem, value);
        }

        public bool ShowLtp { get => _showLtp; set => SetProperty(ref _showLtp, value); }
        public bool ShowHighLow { get => _showHighLow; set => SetProperty(ref _showHighLow, value); }
        public bool ShowOpen { get => _showOpen; set => SetProperty(ref _showOpen, value); }
        public bool ShowClose { get => _showClose; set => SetProperty(ref _showClose, value); }
        public bool ShowSpread { get => _showSpread; set => SetProperty(ref _showSpread, value); }
        public bool ShowDcp { get => _showDcp; set => SetProperty(ref _showDcp, value); }
        public bool ShowDcv { get => _showDcv; set => SetProperty(ref _showDcv, value); }
        public bool ShowTime { get => _showTime; set => SetProperty(ref _showTime, value); }

        #endregion

        #region Commands

        public ICommand HideSymbolCommand { get; }
        public ICommand HideAllCommand { get; }
        public ICommand ShowAllCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand ShowSpecification { get; }
        public ICommand ShowMarketSymbol { get; }
        public ICommand AddSymbolCommand { get; }
        public ICommand ItemDoubleClickCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MarketWatchViewModel class.
        /// </summary>
        public MarketWatchViewModel(MarketWatchService marketWatchService, SessionService sessionService, IDialogService dialogService, LiveTickService liveTickService)
        {
            _marketWatchService = marketWatchService;
            _sessionService = sessionService;
            _dialogService = dialogService;

            _liveTickService = liveTickService;
            _liveTickService.OnTickReceived += HandleLiveTick;
            _liveTickService.OnReconnected += HandleLiveReconnected;

            MarketWatchSymbolsCollection = new ObservableCollection<MarketWatchSymbols>();
            HiddenSymbolsCollection = new ObservableCollection<MarketWatchSymbols>();
            FontSizes = new ObservableCollection<int>();
            SuggestedSymbols = new ObservableCollection<MarketWatchSymbols>();

            for (int i = 10; i <= 30; i += 2) FontSizes.Add(i);

            _marketView = CollectionViewSource.GetDefaultView(MarketWatchSymbolsCollection);
            _marketView.Filter = FilterMarketItems;

            ShowSpecification = new RelayCommand(ShowSpecificationView);
            ShowMarketSymbol = new RelayCommand(ShowMarketwatchSymbol);
            HideSymbolCommand = new AsyncRelayCommand(async (param) => await HideSymbolAsync(param));
            HideAllCommand = new AsyncRelayCommand(async (_) => await HideAllSymbolsAsync());
            ShowAllCommand = new AsyncRelayCommand(async (_) => await ShowAllSymbolsAsync());
            SaveProfileCommand = new AsyncRelayCommand(async (_) => await SaveClientWatchProfileAsync());
            AddSymbolCommand = new AsyncRelayCommand(async (param) => await AddSymbolAsync(param as MarketWatchSymbols));
            ItemDoubleClickCommand = new RelayCommand(param => OpenTradeWindowFromGrid(param as MarketWatchSymbols ?? SelectedMarketItem));

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            _marketWatchService.OnDataUpdated += UpdateMarketData;

            RegisterMessenger();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads market data locally without forcing synchronization.
        /// </summary>
        public void LoadLocalData()
        {
            LoadData(forceSync: false);
        }

        /// <summary>
        /// Ensures an empty row exists at the bottom of the grid.
        /// </summary>
        public void EnsureEmptyRow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MarketWatchSymbolsCollection == null) return;

                var emptyRows = MarketWatchSymbolsCollection.Where(x => string.IsNullOrWhiteSpace(x.SymbolName)).ToList();
                foreach (var row in emptyRows)
                {
                    MarketWatchSymbolsCollection.Remove(row);
                }

                MarketWatchSymbolsCollection.Add(new MarketWatchSymbols { SymbolName = "" });
                UpdateSymbolCount();
            });
        }

        /// <summary>
        /// Sets the visibility tracking for a symbol to manage real-time subscriptions.
        /// </summary>
        public void SetSymbolVisibility(string symbolName, bool isVisible)
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable || string.IsNullOrWhiteSpace(symbolName)) return;

            lock (_nativeVisibleSymbols)
            {
                if (isVisible)
                    _nativeVisibleSymbols.Add(symbolName);
                else
                    _nativeVisibleSymbols.Remove(symbolName);
            }

            DebounceSync();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers listeners for application-wide signals using the Event Aggregator.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    lock (_currentlySubscribed) _currentlySubscribed.Clear();

                    string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchSignalRUrl, _sessionService.PrimaryDomain, "sglr");

                    if (!string.IsNullOrEmpty(url))
                    {
                        await _liveTickService.InitializeAndStartAsync(url);
                    }

                    LoadData(forceSync: true);
                }
                else
                {
                    lock (_currentlySubscribed) _currentlySubscribed.Clear();
                    lock (_nativeVisibleSymbols) _nativeVisibleSymbols.Clear();
                    await _liveTickService.StopConnectionAsync();
                }
            });
        }

        /// <summary>
        /// Opens a new trade order window for the specified symbol selected from the market watch grid.
        /// </summary>
        /// <param name="selectedSymbol">The symbol selected from the market watch grid for which to open the trade order window. Cannot be null, and
        /// must have a non-empty SymbolName.</param>
        private void OpenTradeWindowFromGrid(MarketWatchSymbols selectedSymbol)
        {
            if (selectedSymbol == null || string.IsNullOrWhiteSpace(selectedSymbol.SymbolName))
                return;

            _dialogService.ShowDialog<TradeViewModel>(
                "New Trade Order",
                configureViewModel: vm =>
                {
                    vm.CurrentOrderTypeEnum = EnumTradeOrderType.Market;
                    vm.CurrentWindowModeEnum = EnumTradeWindowMode.FromMarketWatch;
                    vm.SelectedSymbol = selectedSymbol.SymbolName;

                    _ = vm.LoadSymbolListAsync();
                }
            );
        }

        #endregion

        #region Real-Time Sync Logic

        /// <summary>
        /// Debounces rapid visibility changes before executing synchronization.
        /// </summary>
        private async void DebounceSync()
        {
            int currentId = Interlocked.Increment(ref _debounceId);

            await Task.Delay(150);

            if (currentId == _debounceId)
            {
                await ExecuteSyncAsync();
            }
        }

        /// <summary>
        /// Executes the synchronization process to subscribe or unsubscribe from symbols.
        /// </summary>
        private async Task ExecuteSyncAsync()
        {
            await _syncLock.WaitAsync();
            try
            {
                HashSet<string> targetVisible;
                lock (_nativeVisibleSymbols)
                {
                    targetVisible = new HashSet<string>(_nativeVisibleSymbols, StringComparer.OrdinalIgnoreCase);
                }

                var toRemove = _currentlySubscribed.Except(targetVisible).ToList();
                var toAdd = targetVisible.Except(_currentlySubscribed).ToList();

                var tasks = new List<Task>();

                foreach (var sym in toRemove)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await _liveTickService.UnsubscribeSymbolAsync(sym))
                        {
                            lock (_currentlySubscribed) _currentlySubscribed.Remove(sym);
                        }
                    }));
                }

                foreach (var sym in toAdd)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (await _liveTickService.SubscribeSymbolAsync(sym))
                        {
                            lock (_currentlySubscribed) _currentlySubscribed.Add(sym);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Processes incoming live tick data and updates the symbol model.
        /// </summary>
        private void HandleLiveTick(TickData tick)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var existing = MarketWatchSymbolsCollection.FirstOrDefault(x => x.SymbolName == tick.SymbolName);
                if (existing != null)
                {
                    if (tick.Bid != existing.Bid)
                    {
                        existing.BidDir = tick.Bid > existing.Bid ? 1 : -1;
                        existing.TimeDir = existing.BidDir;
                    }

                    if (tick.Ask != existing.Ask)
                    {
                        existing.AskDir = tick.Ask > existing.Ask ? 1 : -1;
                    }

                    if (tick.Ltp != existing.Ltp)
                    {
                        existing.LtpDir = tick.Ltp > existing.Ltp ? 1 : -1;
                    }

                    existing.Bid = tick.Bid;
                    existing.Ask = tick.Ask;
                    existing.Ltp = tick.Ltp;
                    existing.High = tick.High;
                    existing.Low = tick.Low;
                    existing.Open = tick.Open;
                    existing.Close = tick.PreviousClose;

                    existing.Spread = (double)GetSpread((decimal)tick.Ask, (decimal)tick.Bid, existing.SymbolDigit);
                    existing.Dcp = GetDailyChangePercent((decimal)tick.Bid, (decimal)tick.PreviousClose).ToString("F2") + "%";
                    existing.Dcv = (double)GetDailyChangeValue((decimal)tick.Bid, (decimal)tick.PreviousClose);
                    existing.Time = ConvertToTime(tick.UpdateTime);
                }
            });
        }

        /// <summary>
        /// Handles real-time reconnection by forcing a full data synchronization.
        /// </summary>
        private void HandleLiveReconnected()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                lock (_currentlySubscribed)
                {
                    _currentlySubscribed.Clear();
                }
                DebounceSync();
            });
        }

        #endregion

        #region API & Data Loading

        /// <summary>
        /// Fetches market data from the service.
        /// </summary>
        private async void LoadData(bool forceSync)
        {
            var data = await _marketWatchService.GetMarketWatchDataAsync(forceSync);

            if (data != null && data.symbols != null && data.symbols.Any())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateMarketData(data));
            }
        }

        /// <summary>
        /// Updates the market data collections based on API response.
        /// </summary>
        private void UpdateMarketData(MarketWatchData marketWatchData)
        {
            if (marketWatchData == null) return;

            SelectedFontSize = !marketWatchData.fontSize.Equals(0) ? marketWatchData.fontSize : 12;
            ApplyColumnVisibility(marketWatchData.displayColumnNames as string);

            if (marketWatchData.symbols != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MarketWatchSymbolsCollection.Clear();

                    var validSymbols = marketWatchData.symbols
                                        .Where(s => s.symbolStatus)
                                        .OrderBy(s => s.displayPosition)
                                        .ToList();

                    foreach (var apiSymbol in validSymbols)
                    {
                        var symbolModel = CreateMarketItem(apiSymbol);

                        if (!apiSymbol.symbolHide)
                        {
                            MarketWatchSymbolsCollection.Add(symbolModel);
                        }
                        else
                        {
                            if (!HiddenSymbolsCollection.Any(r => r.SymbolName == symbolModel.SymbolName))
                            {
                                HiddenSymbolsCollection.Add(symbolModel);
                            }
                        }
                    }
                    EnsureEmptyRow();
                });
            }
        }

        /// <summary>
        /// Saves the current symbol configuration and preferences to the server.
        /// </summary>
        private async Task SaveClientWatchProfileAsync()
        {
            try
            {
                if (MarketWatchSymbolsCollection == null || MarketWatchSymbolsCollection.Count == 0)
                {
                    FileLogger.Log("MarketWatch", CommonMessages.NoSymbolSave);
                    return;
                }

                var apiFields = new List<string>();
                if (ShowLtp) apiFields.Add("ltp");
                if (ShowHighLow) apiFields.Add("hl");
                if (ShowOpen) apiFields.Add("open");
                if (ShowClose) apiFields.Add("close");
                if (ShowTime) apiFields.Add("time");
                if (ShowSpread) apiFields.Add("spread");
                if (ShowDcp) apiFields.Add("dailyChangePercentage");
                if (ShowDcv) apiFields.Add("dailyChangeValue");

                string displayColumns = string.Join(",", apiFields);

                var symbolsConfig = new List<object>();
                int position = 1;

                foreach (var row in MarketWatchSymbolsCollection)
                {
                    if (string.IsNullOrWhiteSpace(row.SymbolName)) continue;

                    symbolsConfig.Add(new
                    {
                        symbolId = row.SymbolId,
                        symbolHide = false,
                        displayPosition = position++
                    });
                }

                var payload = new
                {
                    fontSize = SelectedFontSize,
                    displayColumnNames = displayColumns,
                    symbolsConfig = symbolsConfig
                };

                var apiResp = await _marketWatchService.SaveProfileAsync(payload);

                if (apiResp?.isSuccess == true)
                {
                    FileLogger.Log("Network", apiResp.successMessage ?? CommonMessages.ProfileSaved);
                }
                else
                {
                    FileLogger.Log("Network", CommonMessages.ProfileFailedToSaved);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("SaveClientWatchProfileAsync", ex);
            }
        }

        #endregion

        #region Symbol Operations

        /// <summary>
        /// Hides a specific symbol from the market watch grid.
        /// </summary>
        private async Task HideSymbolAsync(object parameter)
        {
            var item = parameter as MarketWatchSymbols ?? SelectedMarketItem;
            if (item == null || string.IsNullOrWhiteSpace(item.SymbolName)) return;
            await ProcessHideOperationAsync(new List<int> { item.SymbolId }, "Hide");
        }

        /// <summary>
        /// Hides all visible symbols from the market watch grid.
        /// </summary>
        private async Task HideAllSymbolsAsync()
        {
            var visibleSymbols = MarketWatchSymbolsCollection
                .Where(s => !string.IsNullOrWhiteSpace(s.SymbolName))
                .ToList();

            if (visibleSymbols.Count == 0)
            {
                FileLogger.Log("MarketWatch", CommonMessages.NoSymbolHide);
                return;
            }

            var ids = visibleSymbols.Select(s => s.SymbolId).ToList();
            await ProcessHideOperationAsync(ids, "Hide All");
        }

        /// <summary>
        /// Processes API request to hide a collection of symbols.
        /// </summary>
        private async Task ProcessHideOperationAsync(List<int> symbolIds, string operationName)
        {
            if (symbolIds == null || symbolIds.Count == 0) return;

            try
            {
                var response = await _marketWatchService.HideSymbolsAsync(symbolIds);

                if (response != null && response.data != null && response.data.symbolId != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var symbolsToRemove = MarketWatchSymbolsCollection
                            .Where(s => response.data.symbolId.Contains(s.SymbolId))
                            .ToList();

                        foreach (var symbol in symbolsToRemove)
                        {
                            if (!HiddenSymbolsCollection.Contains(symbol))
                            {
                                HiddenSymbolsCollection.Add(symbol);
                            }
                            MarketWatchSymbolsCollection.Remove(symbol);
                        }
                        EnsureEmptyRow();
                    });

                    if (!string.IsNullOrEmpty(response.successMessage))
                    {
                        FileLogger.Log("MarketWatch", response.successMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog($"ProcessHideOperationAsync - {operationName}", ex);
            }
        }

        /// <summary>
        /// Restores all hidden symbols back to the grid.
        /// </summary>
        private async Task ShowAllSymbolsAsync()
        {
            try
            {
                if (HiddenSymbolsCollection.Count == 0)
                {
                    FileLogger.Log("MarketWatch", CommonMessages.NoHiddenSymbolShow);
                    return;
                }

                int restoredCount = 0;
                var symbolsToRestore = HiddenSymbolsCollection.ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var symbol in symbolsToRestore)
                    {
                        var existing = MarketWatchSymbolsCollection.FirstOrDefault(x => x.SymbolName == symbol.SymbolName);

                        if (existing == null)
                        {
                            MarketWatchSymbolsCollection.Add(symbol);
                            restoredCount++;
                        }
                    }

                    HiddenSymbolsCollection.Clear();
                    EnsureEmptyRow();
                });

                FileLogger.Log("MarketWatch", $"{restoredCount} {CommonMessages.HiddenSymbolRestored}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("ShowAllSymbolsAsync", ex);
            }
        }

        /// <summary>
        /// Populates the suggestion list based on the search query.
        /// </summary>
        private void SearchHiddenSymbols(string searchText)
        {
            IsSuggestionOpen = false;

            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
            {
                SuggestedSymbols.Clear();
                return;
            }

            var matches = HiddenSymbolsCollection
                .Where(s => s.SymbolName.StartsWith(searchText.Trim(), StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

            SuggestedSymbols.Clear();
            foreach (var match in matches)
            {
                SuggestedSymbols.Add(match);
            }

            if (SuggestedSymbols.Count > 0)
            {
                IsSuggestionOpen = true;
            }
        }

        /// <summary>
        /// Adds a selected symbol from the hidden list to the visible grid.
        /// </summary>
        private async Task AddSymbolAsync(MarketWatchSymbols symbol)
        {
            if (symbol == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                HiddenSymbolsCollection.Remove(symbol);
                MarketWatchSymbolsCollection.Add(symbol);
                SelectedMarketItem = symbol;

                NewSymbolSearchText = string.Empty;
                IsSuggestionOpen = false;
                SuggestedSymbols.Clear();
                EnsureEmptyRow();
            });

            await Task.CompletedTask;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Applies column visibility settings based on the provided API string.
        /// </summary>
        private void ApplyColumnVisibility(string apiFields)
        {
            if (string.IsNullOrEmpty(apiFields)) return;

            var fields = new HashSet<string>(apiFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim().ToLowerInvariant()));

            if (fields.Contains("ltp")) ShowLtp = true;
            if (fields.Contains("hl")) ShowHighLow = true;
            if (fields.Contains("open")) ShowOpen = true;
            if (fields.Contains("close")) ShowClose = true;
            if (fields.Contains("time")) ShowTime = true;
            if (fields.Contains("spread")) ShowSpread = true;
            if (fields.Contains("dailychangepercentage")) ShowDcp = true;
            if (fields.Contains("dailychangevalue")) ShowDcv = true;
        }

        /// <summary>
        /// Opens the symbol specification dialog for the selected item.
        /// </summary>
        private void ShowSpecificationView(object parameter)
        {
            var item = parameter as MarketWatchSymbols ?? SelectedMarketItem;

            if (item != null && MarketWatchSymbolsCollection.Contains(item))
            {
                _dialogService.ShowDialog<SymbolSpecificationViewModel>(
                    "Symbol Specification",
                    configureViewModel: vm =>
                    {
                        vm.SymbolName = item.SymbolName;
                        vm.SymbolId = item.SymbolId;
                    }
                );
            }
        }

        private void ShowMarketwatchSymbol(object parameter)
        {
            var item = parameter as MarketWatchSymbols ?? SelectedMarketItem;

            if (item != null && MarketWatchSymbolsCollection.Contains(item))
            {
                _dialogService.ShowDialog<SymbolViewModel>(
                    "Symbol",
                    configureViewModel: vm =>
                    {
                        vm.SymbolName = item.SymbolName;
                    }
                );
            }
        }

        /// <summary>
        /// Maps API symbol model to the application symbol model.
        /// </summary>
        private MarketWatchSymbols CreateMarketItem(MarketWatchApiSymbol apiSymbol)
        {
            var book = apiSymbol.symbolBook;
            decimal bid = book?.bid ?? 0;
            decimal ask = book?.ask ?? 0;
            decimal close = book?.previousClose ?? 0;
            decimal ltp = book?.ltp ?? 0;
            long updateTime = book?.updateTime ?? 0;

            return new MarketWatchSymbols
            {
                SymbolId = apiSymbol.symbolId,
                SymbolDigit = apiSymbol.symbolDigits,
                SymbolName = apiSymbol.symbolName,
                Bid = (double)bid,
                Ask = (double)ask,
                Ltp = (double)ltp,
                High = (double)(book?.high ?? 0),
                Low = (double)(book?.low ?? 0),
                Open = (double)(book?.open ?? 0),
                Close = (double)close,
                Spread = (double)GetSpread(ask, bid, apiSymbol.symbolDigits),
                Dcp = GetDailyChangePercent(bid, close).ToString("F2") + "%",
                Dcv = (double)GetDailyChangeValue(bid, close),
                Time = ConvertToTime(updateTime),
            };
        }

        /// <summary>
        /// Updates the displayed symbol count text.
        /// </summary>
        private void UpdateSymbolCount()
        {
            int visibleCount = MarketWatchSymbolsCollection.Count(s => !string.IsNullOrWhiteSpace(s.SymbolName));
            int hiddenCount = HiddenSymbolsCollection.Count;
            int totalCount = visibleCount + hiddenCount;

            SymbolCountText = $"{visibleCount} / {totalCount}";
        }

        /// <summary>
        /// Converts unix timestamp to a local time string format.
        /// </summary>
        private string ConvertToTime(long timestamp)
        {
            if (timestamp <= 0) return "--:--:--";
            try
            {
                return timestamp > 10000000000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime.ToString("HH:mm:ss")
                    : DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime.ToString("HH:mm:ss");
            }
            catch { return "--:--:--"; }
        }

        /// <summary>
        /// Filters market items based on the search input.
        /// </summary>
        private bool FilterMarketItems(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var marketWatchSymbols = item as MarketWatchSymbols;

            if (marketWatchSymbols == null || string.IsNullOrWhiteSpace(marketWatchSymbols.SymbolName))
                return true;

            return marketWatchSymbols != null && marketWatchSymbols.SymbolName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Calculates the spread based on ask and bid prices.
        /// </summary>
        private decimal GetSpread(decimal ask, decimal bid, int symbolDigit)
        {
            try
            {
                decimal multiplier = (decimal)Math.Pow(10, symbolDigit);
                return Math.Round((ask * multiplier) - (bid * multiplier), 2);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Calculates the daily change percentage based on WinForms logic.
        /// </summary>
        private decimal GetDailyChangePercent(decimal bid, decimal close)
        {
            if (bid == 0) return 0;
            return Math.Round((100 * (bid - close)) / bid, 2);
        }

        /// <summary>
        /// Calculates the daily change value difference.
        /// </summary>
        private decimal GetDailyChangeValue(decimal bid, decimal close) => bid - close;

        #endregion
    }
}