using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using System.Collections.Generic;

namespace ClientDesktop.ViewModel
{
    public partial class TradeViewModel
    {
        #region 2. Public Data Properties & Backing Fields
        private PositionGridRow _positionGridRow;
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
                        _ = GetSymbolDataAsync(value.SymbolId);
                        UpdateCloseButtonCaption();
                    }
                }
            }
        }

        private EnumTradeOrderType _currentOrderType = EnumTradeOrderType.Market;
        public EnumTradeOrderType CurrentOrderTypeEnum
        {
            get => _currentOrderType;
            set
            {
                if (SetProperty(ref _currentOrderType, value))
                {
                    OnPropertyChanged(nameof(IsModifyDeleteMode));
                    OnPropertyChanged(nameof(LeftActionText));
                    OnPropertyChanged(nameof(RightActionText));
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

        private EnumTradeWindowMode _currentWindowModeEnum = EnumTradeWindowMode.FromTradeButton;
        public EnumTradeWindowMode CurrentWindowModeEnum
        {
            get => _currentWindowModeEnum;
            set
            {
                if (SetProperty(ref _currentWindowModeEnum, value))
                {
                    OnPropertyChanged(nameof(IsModifyDeleteMode));
                    OnPropertyChanged(nameof(LeftActionText));
                    OnPropertyChanged(nameof(RightActionText));
                    OnPropertyChanged(nameof(IsCloseButtonVisible));
                }
            }
        }

        private EnumTradeOrderType? _originalOrderType = null;
        public EnumTradeOrderType? OriginalOrderType
        {
            get => _originalOrderType;
            set
            {
                SetProperty(ref _originalOrderType, value);
                OnPropertyChanged(nameof(IsModifyDeleteMode));
            }
        }

        private List<string> _availableSymbols;
        public List<string> AvailableSymbols { get => _availableSymbols; set => SetProperty(ref _availableSymbols, value); }

        private string _selectedSymbol;
        public string SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                if (SetProperty(ref _selectedSymbol, value))
                {
                    if (!string.IsNullOrEmpty(value) && _symbolMap.TryGetValue(value, out var symbolInfo))
                    {
                        _currentDigits = symbolInfo.Digits;
                        _ = GetSymbolDataAsync(symbolInfo.Id);
                    }
                    _ = ManageLiveTicksAsync(value);
                }
            }
        }

        public SymbolData _currentSelectedSymbol { get; set; }
        public List<string> ExpiryOptions { get; } = new List<string> { "GTC", "Today", "Specific Date" };

        private string _selectedExpiry = "GTC";
        public string SelectedExpiry
        {
            get => _selectedExpiry;
            set { if (SetProperty(ref _selectedExpiry, value)) OnPropertyChanged(nameof(IsSpecificDateVisible)); }
        }

        private string _liveBid = "0.00";
        public string LiveBid { get => _liveBid; set => SetProperty(ref _liveBid, value); }

        private string _liveAsk = "0.00";
        public string LiveAsk { get => _liveAsk; set => SetProperty(ref _liveAsk, value); }

        private string _quantity = "0.00";
        public string Quantity
        {
            get => _quantity;
            set { if (SetProperty(ref _quantity, value)) UpdateCloseButtonCaption(); }
        }

        private string _limitRate = "0.00";
        public string LimitRate { get => _limitRate; set => SetProperty(ref _limitRate, value); }

        private string _closeButtonText;
        public string CloseButtonText { get => _closeButtonText; set => SetProperty(ref _closeButtonText, value); }

        // Account & Symbol Details Fields
        private string _userName; public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
        private string _balance; public string Balance { get => _balance; set => SetProperty(ref _balance, value); }
        private string _credit; public string Credit { get => _credit; set => SetProperty(ref _credit, value); }
        private string _occupiedMargin; public string OccupiedMargin { get => _occupiedMargin; set => SetProperty(ref _occupiedMargin, value); }
        private string _freeMargin; public string FreeMargin { get => _freeMargin; set => SetProperty(ref _freeMargin, value); }
        private string _minValue = "0.00"; public string MinValue { get => _minValue; set => SetProperty(ref _minValue, value); }
        private string _stepValue = "0.00"; public string StepValue { get => _stepValue; set => SetProperty(ref _stepValue, value); }
        private string _oneValue = "0.00"; public string OneValue { get => _oneValue; set => SetProperty(ref _oneValue, value); }
        private string _totalValue = "0.00"; public string TotalValue { get => _totalValue; set => SetProperty(ref _totalValue, value); }
        private string _limitStopValue = "0.00"; public string LimitStopValue { get => _limitStopValue; set => SetProperty(ref _limitStopValue, value); }

        #endregion

        #region 3. UI State & Logic Properties (Read-Only)
        public bool IsModifyDeleteMode => CurrentWindowModeEnum == EnumTradeWindowMode.FromOrder && OriginalOrderType.HasValue && OriginalOrderType.Value == CurrentOrderTypeEnum;
        public string LeftActionText => IsModifyDeleteMode ? "MODIFY" : CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "SELL" : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "SELL LIMIT" : "SELL STOPLIMIT";
        public string RightActionText => IsModifyDeleteMode ? "DELETE" : CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "BUY" : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "BUY LIMIT" : "BUY STOPLIMIT";
        public bool IsCloseButtonVisible => positionGridRow != null && positionGridRow.IsPosition && CurrentOrderTypeEnum == EnumTradeOrderType.Market;
        public bool IsSymbolEditable => EnumTradeWindowMode.FromTradeButton == CurrentWindowModeEnum;
        public bool IsLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.Limit;
        public bool IsMarketActive => CurrentOrderTypeEnum == EnumTradeOrderType.Market;
        public bool IsStopLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit;
        public bool IsExpiryVisible => CurrentOrderTypeEnum != EnumTradeOrderType.Market;
        public bool IsSpecificDateVisible => SelectedExpiry == "Specific Date";
        public string SellButtonText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "SELL" : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "SELL LIMIT" : "SELL STOPLIMIT";
        public string BuyButtonText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "BUY" : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "BUY LIMIT" : "BUY STOPLIMIT";
        public string RateLabelText => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "Rate :" : "Limit Rate :";

        private DateTime? _selectedExpiryDate = DateTime.Now; 
        public DateTime? SelectedExpiryDate
        {
            get => _selectedExpiryDate;
            set => SetProperty(ref _selectedExpiryDate, value);
        }

        // Execution UI States
        private bool _isProcessingOrDone;
        public bool IsProcessingOrDone
        {
            get => _isProcessingOrDone;
            set
            {
                if (SetProperty(ref _isProcessingOrDone, value))
                {
                    OnPropertyChanged(nameof(IsFormEnabled));
                    OnPropertyChanged(nameof(ShowTradeActions));
                    OnPropertyChanged(nameof(ShowOkPanel));
                }
            }
        }
        public bool IsFormEnabled => !IsProcessingOrDone;
        public bool ShowTradeActions => !IsProcessingOrDone;
        public bool ShowOkPanel => IsProcessingOrDone;

        private string _tradeResultMessage = "Order Placed !";
        public string TradeResultMessage { get => _tradeResultMessage; set => SetProperty(ref _tradeResultMessage, value); }
        #endregion
    }

}