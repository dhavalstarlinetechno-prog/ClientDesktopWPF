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

        private PositionGridRow _positionGridRow;
        private TradeOrderType _currentOrderType = TradeOrderType.Market;
        private List<string> _availableSymbols;
        private string _selectedSymbol;

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
                    }
                }
            }
        }

        public TradeWindowMode CurrentWindowMode { get; set; } = TradeWindowMode.FromTradeButton;
        public TradeOrderType CurrentOrderType
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
            set => SetProperty(ref _selectedSymbol, value);
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
        public bool IsLimitActive => CurrentOrderType == TradeOrderType.Limit;
        public bool IsMarketActive => CurrentOrderType == TradeOrderType.Market;
        public bool IsStopLimitActive => CurrentOrderType == TradeOrderType.StopLimit;
        public bool IsExpiryVisible => CurrentOrderType != TradeOrderType.Market;
        public bool IsSpecificDateVisible => SelectedExpiry == "Specific Date";
        public string SellButtonText => CurrentOrderType == TradeOrderType.Market ? "SELL" :
                                        CurrentOrderType == TradeOrderType.Limit ? "SELL LIMIT" : "SELL STOPLIMIT";

        public string BuyButtonText => CurrentOrderType == TradeOrderType.Market ? "BUY" :
                                       CurrentOrderType == TradeOrderType.Limit ? "BUY LIMIT" : "BUY STOPLIMIT";

        public string RateLabelText => CurrentOrderType == TradeOrderType.Market ? "Rate :" : "Limit Rate :";
        #endregion

        #region 4. Commands
        public ICommand ChangeOrderTypeCommand => new RelayCommand(param =>
        {
            if (param is string typeString && Enum.TryParse<TradeOrderType>(typeString, true, out var newType))
            {
                SetOrderType(newType);
            }
        });
        #endregion

        #region 5. Constructor
        public TradeViewModel(SessionService sessionService, TradeService tradeService)
        {
            _sessionService = sessionService;
            _tradeService = tradeService;
            SetUserAccountInfo();
        }

        private void SetUserAccountInfo()
        {
            if (_sessionService != null)
            {
                var client = _sessionService.ClientListData.Find(c => c.ClientId == _sessionService.UserId);
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
            AvailableSymbols = data.symbols.Select(s => s.symbolName).ToList();
        }

        private void SetOrderType(TradeOrderType newType)
        {
            CurrentOrderType = newType;
        }
        #endregion
    }
}