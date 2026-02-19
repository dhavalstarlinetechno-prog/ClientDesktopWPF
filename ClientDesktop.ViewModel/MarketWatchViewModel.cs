using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
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
        private bool _showLtp = true;
        private bool _showHighLow = true;
        private bool _showOpen = true;
        private bool _showClose = true;
        private bool _showSpread = true;
        private bool _showDcp = true;
        private bool _showDcv = true;
        private bool _showTime = true;

        public bool ShowLtp { get => _showLtp; set => SetProperty(ref _showLtp, value); }
        public bool ShowHighLow { get => _showHighLow; set => SetProperty(ref _showHighLow, value); }
        public bool ShowOpen { get => _showOpen; set => SetProperty(ref _showOpen, value); }
        public bool ShowClose { get => _showClose; set => SetProperty(ref _showClose, value); }
        public bool ShowSpread { get => _showSpread; set => SetProperty(ref _showSpread, value); }
        public bool ShowDcp { get => _showDcp; set => SetProperty(ref _showDcp, value); }
        public bool ShowDcv { get => _showDcv; set => SetProperty(ref _showDcv, value); }
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

        public MarketWatchViewModel(MarketWatchService marketWatchService, SessionService sessionService)
        {
            _marketWatchService = marketWatchService;
            _sessionService = sessionService;

            MarketItems = new ObservableCollection<MarketItem>();
            FontSizes = new ObservableCollection<int>();

            for (int i = 10; i <= 30; i += 2) FontSizes.Add(i);
            SelectedFontSize = 12;

            // Setup Filtering
            _marketView = CollectionViewSource.GetDefaultView(MarketItems);
            _marketView.Filter = FilterMarketItems;

            // Initialize Commands
            HideSymbolCommand = new RelayCommand(HideSymbol);
            HideAllCommand = new RelayCommand(_ => MarketItems.Clear());
            // ShowAllCommand logic can be implemented to reload or show hidden items

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
            if (marketWatchData?.symbols == null) return;

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

        private MarketItem CreateMarketItem(MarketWatchApiSymbol apiSymbol)
        {
            var book = apiSymbol.symbolBook;
            decimal bid = book?.bid ?? 0;
            decimal ask = book?.ask ?? 0;
            decimal close = book?.previousClose ?? 0;
            decimal ltp = book?.ltp ?? 0;
            string displayTime = "00:00:00";
            long updateTime = book?.updateTime ?? 0;

            try
            {
                if (updateTime > 0)
                {
                    if (updateTime > 10000000000)
                    {
                        displayTime = DateTimeOffset.FromUnixTimeMilliseconds(updateTime).LocalDateTime.ToString("HH:mm:ss");
                    }
                    else
                    {
                        displayTime = DateTimeOffset.FromUnixTimeSeconds(updateTime).LocalDateTime.ToString("HH:mm:ss");
                    }
                }
            }
            catch
            {
                displayTime = "--:--:--";
            }

            return new MarketItem
            {
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
                Time = displayTime,
                IsUp = ltp > close
            };
        }

        private bool FilterMarketItems(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var marketItem = item as MarketItem;
            return marketItem != null && marketItem.SymbolName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void HideSymbol(object parameter)
        {
            var item = parameter as MarketItem ?? SelectedMarketItem;
            if (item != null && MarketItems.Contains(item))
            {
                MarketItems.Remove(item);
            }
        }

        #region Helper Methods

        private decimal GetSpread(decimal ask, decimal bid, int symbolDigit)
        {
            decimal spread = 0;
            try
            {
                decimal multiplier = (decimal)Math.Pow(10, symbolDigit);
                spread = (ask * multiplier) - (bid * multiplier);
                spread = Math.Round(spread, 2);
            }
            catch { spread = 0; }

            return spread;
        }

        private decimal GetDailyChangePercent(decimal bid, decimal close)
        {
            decimal dcp = 0;
            if (bid != 0 && close != 0)
            {
                dcp = (100 * (bid - close)) / close; // Formula adjusted: (Current - Prev) / Prev * 100
                dcp = Math.Round(dcp, 2);
            }
            return dcp;
        }

        private decimal GetDailyChangeValue(decimal bid, decimal close)
        {
            return bid - close;
        }

        #endregion
    }
}
