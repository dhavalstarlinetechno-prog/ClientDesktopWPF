using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClientDesktop.ViewModel
{
    public class MarketWatchViewModel : ViewModelBase
    {
        private string _currentTime;
        private string _searchText;
        private int _selectedFontSize;
        private ICollectionView _marketView;
        private MarketItem _selectedMarketItem;

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

        public ObservableCollection<MarketItem> MarketItems { get; set; }
        public ObservableCollection<int> FontSizes { get; set; }

        public MarketItem SelectedMarketItem
        {
            get => _selectedMarketItem;
            set => SetProperty(ref _selectedMarketItem, value);
        }

        // --- Commands ---
        public ICommand HideSymbolCommand { get; }
        public ICommand HideAllCommand { get; }
        public ICommand ShowAllCommand { get; }

        public MarketWatchViewModel()
        {
            // Initialize Collections
            MarketItems = new ObservableCollection<MarketItem>();
            FontSizes = new ObservableCollection<int>();

            // Populate Font Sizes
            for (int i = 10; i <= 30; i += 2) FontSizes.Add(i);
            SelectedFontSize = 12;

            // Load Data
            LoadDummyData();

            // Setup Filtering
            _marketView = CollectionViewSource.GetDefaultView(MarketItems);
            _marketView.Filter = FilterMarketItems;

            // Initialize Commands
            HideSymbolCommand = new RelayCommand(HideSymbol);
            HideAllCommand = new RelayCommand(_ => MarketItems.Clear());
            ShowAllCommand = new RelayCommand(_ => LoadDummyData());

            // Start Timer
            StartTimer();
        }

        private void HideSymbol(object parameter)
        {
            var item = parameter as MarketItem ?? SelectedMarketItem;
            if (item != null && MarketItems.Contains(item))
            {
                MarketItems.Remove(item);
            }
        }

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

        public ICollectionView MarketView => _marketView;

        private void StartTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        }

        private bool FilterMarketItems(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var marketItem = item as MarketItem;
            return marketItem != null && marketItem.SymbolName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LoadDummyData()
        {
            MarketItems.Clear();
            MarketItems.Add(new MarketItem { SymbolName = "XAUUSD-M", Bid = 4908.27, Ask = 4908.57, Ltp = 4908.27, High = 5000.85, Low = 4858.35, Open = 4995.60, Close = 4991.41, Spread = 30.00, Dcp = "-1.69%", Dcv = -83.14, Time = "12:58:13", IsUp = true });
            MarketItems.Add(new MarketItem { SymbolName = "XAGUSD-M", Bid = 74.4798, Ask = 74.5456, Ltp = 74.4798, High = 76.8790, Low = 72.8080, Open = 76.5374, Close = 76.5810, Spread = 658.00, Dcp = "-2.82%", Dcv = -2.1012, Time = "12:58:13", IsUp = true });
            MarketItems.Add(new MarketItem { SymbolName = "SIMAR", Bid = 74.2850, Ask = 74.3450, Ltp = 74.3200, High = 78.4200, Low = 72.5050, Open = 77.5500, Close = 77.9640, Spread = 600.00, Dcp = "-4.95%", Dcv = -3.6790, Time = "12:58:12", IsUp = false });
            MarketItems.Add(new MarketItem { SymbolName = "SILVERMAR", Bid = 233476, Ask = 233771, Ltp = 233700, High = 237720, Low = 229352, Open = 235207, Close = 239891, Spread = 29500, Dcp = "-2.75%", Dcv = -6415.00, Time = "12:58:12", IsUp = true });
            MarketItems.Add(new MarketItem { SymbolName = "HGH26", Bid = 5.7080, Ask = 5.7090, Ltp = 5.7085, High = 5.8020, Low = 5.6805, Open = 5.7855, Close = 5.8030, Spread = 10.00, Dcp = "-1.66%", Dcv = -0.0950, Time = "12:58:12", IsUp = false });
            MarketItems.Add(new MarketItem { SymbolName = "NIKKIMAR", Bid = 56590.00, Ask = 56600.00, Ltp = 56605.00, High = 57770.00, Low = 56190.00, Open = 57595.00, Close = 57630.00, Spread = 1000.00, Dcp = "-1.84%", Dcv = -1040.00, Time = "12:58:09", IsUp = false });
            MarketItems.Add(new MarketItem { SymbolName = "DOWJONSMAR", Bid = 49377.00, Ask = 49379.00, Ltp = 49379.00, High = 49820.00, Low = 49359.00, Open = 49549.00, Close = 49569.00, Spread = 200.00, Dcp = "-0.39%", Dcv = -192.00, Time = "12:58:14", IsUp = false });
            MarketItems.Add(new MarketItem { SymbolName = "NASDAQMAR", Bid = 24593.75, Ask = 24595.00, Ltp = 24594.25, High = 24922.25, Low = 24558.25, Open = 24789.75, Close = 24803.25, Spread = 125.00, Dcp = "-0.85%", Dcv = -209.50, Time = "12:58:13", IsUp = true });
        }
    }
}
