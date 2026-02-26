using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class TradeViewModel : ViewModelBase
    {
        #region 1. Private Fields & Injected Services

        private readonly SessionService _sessionService;
        private readonly TradeService _tradeService;
        private readonly LiveTickService _liveTickService;

        private PositionGridRow _positionGridRow;
        private EnumTradeOrderType _currentOrderType = EnumTradeOrderType.Market;
        private List<string> _availableSymbols;
        private string _selectedSymbol;
        private Dictionary<string, (int Id, int Digits)> _symbolMap = new Dictionary<string, (int Id, int Digits)>(); private string _currentTickSymbol;
        private int _currentDigits = 2;

        private string _liveBid = "0.00";
        public string LiveBid { get => _liveBid; set => SetProperty(ref _liveBid, value); }

        private string _liveAsk = "0.00";
        public string LiveAsk { get => _liveAsk; set => SetProperty(ref _liveAsk, value); }

        // --- ACCOUNT DETAILS ---
        private string _userName;
        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }

        private string _balance;
        public string Balance { get => _balance; set => SetProperty(ref _balance, value); }

        private string _credit;
        public string Credit { get => _credit; set => SetProperty(ref _credit, value); }

        private string _occupiedMargin;
        public string OccupiedMargin { get => _occupiedMargin; set => SetProperty(ref _occupiedMargin, value); }

        private string _freeMargin;
        public string FreeMargin { get => _freeMargin; set => SetProperty(ref _freeMargin, value); }

        // --- SYMBOL DETAILS ---

        private string _minValue = "0.00";
        public string MinValue { get => _minValue; set => SetProperty(ref _minValue, value); }

        private string _stepValue = "0.00";
        public string StepValue { get => _stepValue; set => SetProperty(ref _stepValue, value); }

        private string _oneValue = "0.00";
        public string OneValue { get => _oneValue; set => SetProperty(ref _oneValue, value); }

        private string _totalValue = "0.00";
        public string TotalValue { get => _totalValue; set => SetProperty(ref _totalValue, value); }

        private string _limitStopValue = "0.00";
        public string LimitStopValue { get => _limitStopValue; set => SetProperty(ref _limitStopValue, value); }
        #endregion

        #region 2. Public Data Properties

        public PositionGridRow positionGridRow
        {
            get => _positionGridRow;
            set
            {
                if (SetProperty(ref _positionGridRow, value))
                {
                    OnPropertyChanged(nameof(IsSymbolEditable));

                    if (value != null)
                    {
                        SelectedSymbol = value.SymbolName;
                        GetSymbolDataAsync(value.SymbolId);
                    }
                }
            }
        }

        public EnumTradeWindowMode CurrentWindowModeEnum { get; set; } = EnumTradeWindowMode.FromTradeButton;
        public EnumTradeOrderType CurrentOrderTypeEnum
        {
            get => _currentOrderType;
            set
            {
                if (SetProperty(ref _currentOrderType, value))
                {
                    // Trigger UI updates when Order Type changes
                    OnPropertyChanged(nameof(IsLimitActive));
                    OnPropertyChanged(nameof(IsMarketActive));
                    OnPropertyChanged(nameof(IsStopLimitActive));
                    OnPropertyChanged(nameof(SellButtonText));
                    OnPropertyChanged(nameof(BuyButtonText));
                    OnPropertyChanged(nameof(RateLabelText));
                    OnPropertyChanged(nameof(IsExpiryVisible));
                }
            }
        }

        public List<string> AvailableSymbols
        {
            get => _availableSymbols;
            set => SetProperty(ref _availableSymbols, value);
        }

        public string SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                if (SetProperty(ref _selectedSymbol, value))
                {
                    if (!string.IsNullOrEmpty(value) && _symbolMap.TryGetValue(value, out var symbolInfo))
                    {
                        _currentDigits = symbolInfo.Digits; // Naye symbol ke hisaab se digits set kar do
                        GetSymbolDataAsync(symbolInfo.Id);
                    }

                    ManageLiveTicksAsync(value);
                }
            }
        }

        public List<string> ExpiryOptions { get; } = new List<string> { "GTC", "Today", "Specific Date" };

        private string _selectedExpiry = "GTC"; 
        public string SelectedExpiry
        {
            get => _selectedExpiry;
            set
            {
                if (SetProperty(ref _selectedExpiry, value))
                {
                    OnPropertyChanged(nameof(IsSpecificDateVisible));
                }
            }
        }
        #endregion

        #region 3. UI State & Logic Properties (Read-Only)
        public bool IsSymbolEditable => positionGridRow == null;
        public bool IsLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.Limit;
        public bool IsMarketActive => CurrentOrderTypeEnum == EnumTradeOrderType.Market;
        public bool IsStopLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit;
        public bool IsExpiryVisible => CurrentOrderTypeEnum != EnumTradeOrderType.Market;
        public bool IsSpecificDateVisible => SelectedExpiry == "Specific Date";
        public string SellButtonText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "SELL" :
                                        CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "SELL LIMIT" : "SELL STOPLIMIT";

        public string BuyButtonText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "BUY" :
                                       CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "BUY LIMIT" : "BUY STOPLIMIT";

        public string RateLabelText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "Rate :" : "Limit Rate :";
        #endregion

        #region 4. Commands
        public ICommand ChangeOrderTypeCommand => new RelayCommand(param =>
        {
            if (param is string typeString && Enum.TryParse<EnumTradeOrderType>(typeString, true, out var newType))
            {
                SetOrderType(newType);
            }
        });
        #endregion

        #region 5. Constructor
        public TradeViewModel(SessionService sessionService, TradeService tradeService , LiveTickService liveTickService)
        {
            _sessionService = sessionService;
            _tradeService = tradeService;
            _liveTickService = liveTickService;

            SetUserAccountInfo();
            _liveTickService.OnTickReceived += HandleLiveTick;
        }

        private void SetUserAccountInfo()
        {
            if (_sessionService != null)
            {
                var client = _sessionService.ClientListData?.Find(c => c.ClientId == _sessionService.UserId);
                if (client != null)
                {
                    UserName = client.ClientName;
                    Balance = client.UplineAmount.ToString("F2");
                    Credit = client.CreditAmount.ToString("F2");
                    OccupiedMargin = client.OccupiedMarginAmount.ToString("F2");
                    FreeMargin = client.FreeMarginAmount.ToString("F2");
                }
            }
        }
        #endregion

        #region 6. Public & Private Methods
        public async Task LoadSymbolListAsync()
        {
            var data = await _tradeService.GetMarketWatchDataAsync();

            // Yahan ID aur Digits dono ko map mein daal rahe hain
            _symbolMap = data.symbols.ToDictionary(
                s => (string)s.symbolName,
                s => ((int)s.symbolId, (int)s.symbolDigits)
            );

            AvailableSymbols = _symbolMap.Keys.ToList();
        }

        public async void GetSymbolDataAsync(int symbolId)
        {
            var result = await _tradeService.GetSymbolDataAsync(_sessionService.UserId, symbolId);
            
            if (result.Success && result.SymbolData != null)
            {
                MinValue = result.SymbolData.SymbolMinimumValue.ToString("F2");
                StepValue = result.SymbolData.SymbolStepValue.ToString("F2");
                OneValue = result.SymbolData.SymbolOneClickValue.ToString("F2");
                TotalValue = result.SymbolData.SymbolTotalValue.ToString("F2");
                LimitStopValue = result.SymbolData.SymbolLimitstoplevel.ToString("F2");
            }
        }

        private void SetOrderType(EnumTradeOrderType newType)
        {
            CurrentOrderTypeEnum = newType;
        }

        private async void ManageLiveTicksAsync(string newSymbol)
        {
            if (string.IsNullOrWhiteSpace(newSymbol) || _currentTickSymbol == newSymbol) return;

            if (!string.IsNullOrEmpty(_currentTickSymbol))
            {
                await _liveTickService.UnsubscribeSymbolAsync(_currentTickSymbol);
            }

            _currentTickSymbol = newSymbol;
            await _liveTickService.SubscribeSymbolAsync(_currentTickSymbol);
        }

        private void HandleLiveTick(TickData tick)
        {
            if (tick.SymbolName != _currentTickSymbol) return;

            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LiveBid = tick.Bid.ToString($"F{_currentDigits}");
                LiveAsk = tick.Ask.ToString($"F{_currentDigits}");
            });
        }

        public async void Cleanup()
        {
            if (!string.IsNullOrEmpty(_currentTickSymbol))
            {
                await _liveTickService.UnsubscribeSymbolAsync(_currentTickSymbol);
            }
            _liveTickService.OnTickReceived -= HandleLiveTick;
        }

        #endregion
    }
}