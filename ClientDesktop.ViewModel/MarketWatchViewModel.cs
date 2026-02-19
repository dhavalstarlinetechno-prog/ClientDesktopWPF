using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClientDesktop.ViewModel
{
    public class MarketWatchViewModel : ViewModelBase
    {
        private readonly MarketWatchService _marketWatchService;
        private readonly SessionService _sessionService;

        private string _currentTime;
        private string _searchText;
        private int _selectedFontSize;
        private ICollectionView _marketView;
        public ICollectionView MarketView => _marketView;
        private MarketItem _selectedMarketItem;
        public ObservableCollection<MarketItem> MarketItems { get; set; }
        public ObservableCollection<int> FontSizes { get; set; }

        // --- Column Visibility Properties ---
        private bool _showLtp = false;
        public bool ShowLtp { get => _showLtp; set => SetProperty(ref _showLtp, value); }
        private bool _showHighLow = false;
        public bool ShowHighLow { get => _showHighLow; set => SetProperty(ref _showHighLow, value); }
        private bool _showOpen = false;
        public bool ShowOpen { get => _showOpen; set => SetProperty(ref _showOpen, value); }
        private bool _showClose = false;
        public bool ShowClose { get => _showClose; set => SetProperty(ref _showClose, value); }
        private bool _showSpread = false;
        public bool ShowSpread { get => _showSpread; set => SetProperty(ref _showSpread, value); }
        private bool _showDcp = false;
        public bool ShowDcp { get => _showDcp; set => SetProperty(ref _showDcp, value); }
        private bool _showDcv = false;
        public bool ShowDcv { get => _showDcv; set => SetProperty(ref _showDcv, value); }
        private bool _showTime = true;
        public bool ShowTime { get => _showTime; set => SetProperty(ref _showTime, value); }

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


        public MarketItem SelectedMarketItem
        {
            get => _selectedMarketItem;
            set => SetProperty(ref _selectedMarketItem, value);
        }

        // --- Commands ---
        public ICommand HideSymbolCommand { get; }
        public ICommand HideAllCommand { get; }
        public ICommand ShowAllCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public MarketWatchViewModel(MarketWatchService marketWatchService, SessionService sessionService)
        {
            _marketWatchService = marketWatchService;
            _sessionService = sessionService;

            MarketItems = new ObservableCollection<MarketItem>();
            FontSizes = new ObservableCollection<int>();

            for (int i = 10; i <= 30; i += 2) FontSizes.Add(i);

            // Setup Filtering
            _marketView = CollectionViewSource.GetDefaultView(MarketItems);
            _marketView.Filter = FilterMarketItems;

            // Initialize Commands
            HideSymbolCommand = new RelayCommand(HideSymbol);
            HideAllCommand = new RelayCommand(_ => MarketItems.Clear());
            // ShowAllCommand logic can be implemented to reload or show hidden items
            SaveProfileCommand = new RelayCommand(async _ => await SaveClientWatchProfileAsync());

            // set up timer to update current time every second
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            _sessionService.OnLoginSuccess += () => LoadData(forceSync: true);
            _marketWatchService.OnDataUpdated += UpdateMarketData;
        }

        public void LoadLocalData()
        {
            LoadData(forceSync: false);
        }

        private async void LoadData(bool forceSync)
        {
            var data = await _marketWatchService.GetMarketWatchDataAsync(forceSync);

            if (data != null && data.symbols != null && data.symbols.Any())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateMarketData(data));
            }
        }

        private void UpdateMarketData(MarketWatchData marketWatchData)
        {
            if (marketWatchData == null) return;
            
           SelectedFontSize = !marketWatchData.fontSize.Equals(0) ? marketWatchData.fontSize : 10;

            ApplyColumnVisibility(marketWatchData.displayColumnNames as string);

            if (marketWatchData.symbols != null)
            {
                var viewModels = marketWatchData.symbols
                                           .Where(x => !x.symbolHide && x.symbolStatus)
                                           .OrderBy(x => x.displayPosition)
                                           .Select(CreateMarketItem).ToList();

                MarketItems.Clear();
                foreach (var item in viewModels)
                {
                    MarketItems.Add(item);
                }
            }
        }

        private void ApplyColumnVisibility(string apiFields)
        {
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

        private void HideSymbol(object parameter)
        {
            var item = parameter as MarketItem ?? SelectedMarketItem;
            if (item != null && MarketItems.Contains(item))
            {
                MarketItems.Remove(item);
            }
        }

        private async Task SaveClientWatchProfileAsync()
        {
            try
            {
                if (MarketItems == null || MarketItems.Count == 0)
                {
                    FileLogger.Log("MarketWatch", CommonMessages.NoSymbolSave);
                    return;
                }

                // 1. Prepare Column String based on Boolean Properties
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

                // 2. Prepare Symbol Config
                var symbolsConfig = new List<object>();
                int position = 1;

                foreach (var row in MarketItems)
                {
                    if (string.IsNullOrWhiteSpace(row.SymbolName)) continue;

                    symbolsConfig.Add(new
                    {
                        symbolId = row.SymbolId,
                        symbolHide = false,
                        displayPosition = position++
                    });
                }

                // 3. Create Payload
                var payload = new
                {
                    fontSize = SelectedFontSize,
                    displayColumnNames = displayColumns,
                    symbolsConfig = symbolsConfig
                };

                // 4. Call Service
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

        #region Helpers
        private MarketItem CreateMarketItem(MarketWatchApiSymbol apiSymbol)
        {
            var book = apiSymbol.symbolBook;
            decimal bid = book?.bid ?? 0;
            decimal ask = book?.ask ?? 0;
            decimal close = book?.previousClose ?? 0;
            decimal ltp = book?.ltp ?? 0;
            long updateTime = book?.updateTime ?? 0;

            return new MarketItem
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
                IsUp = ltp > close
            };
        }

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

        private bool FilterMarketItems(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var marketItem = item as MarketItem;
            return marketItem != null && marketItem.SymbolName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private decimal GetSpread(decimal ask, decimal bid, int symbolDigit)
        {
            try
            {
                decimal multiplier = (decimal)Math.Pow(10, symbolDigit);
                return Math.Round((ask * multiplier) - (bid * multiplier), 2);
            }
            catch { return 0; }
        }

        private decimal GetDailyChangePercent(decimal bid, decimal close)
        {
            if (bid == 0) return 0;
            return Math.Round((100 * (bid - close)) / bid, 2); // WinForms formula
        }

        private decimal GetDailyChangeValue(decimal bid, decimal close) => bid - close;
        #endregion
    }
}
