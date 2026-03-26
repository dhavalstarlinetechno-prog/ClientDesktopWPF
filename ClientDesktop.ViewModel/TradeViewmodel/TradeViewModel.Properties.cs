using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using System;
using System.Collections.Generic;

namespace ClientDesktop.ViewModel
{
    public partial class TradeViewModel
    {
        #region Position & Order Context

        private PositionGridRow _positionGridRow;

        /// <summary>
        /// The grid row that opened this window.
        /// When set, pre-populates symbol, quantity, and triggers symbol data fetch.
        /// </summary>
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

        private EnumTradeOrderType? _originalOrderType = null;

        /// <summary>
        /// The order type as it existed before the user made changes.
        /// Used to determine whether the window is in Modify/Delete mode.
        /// </summary>
        public EnumTradeOrderType? OriginalOrderType
        {
            get => _originalOrderType;
            set
            {
                if (SetProperty(ref _originalOrderType, value))
                    OnPropertyChanged(nameof(IsModifyDeleteMode));
            }
        }

        #endregion

        #region Order Type & Window Mode

        private EnumTradeOrderType _currentOrderType = EnumTradeOrderType.Market;

        /// <summary>The active order type (Market, Limit, or StopLimit).</summary>
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

        /// <summary>Determines whether the window was opened from a trade button or an existing order row.</summary>
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

        #endregion

        #region Symbol & Market Data

        private List<string> _availableSymbols;

        /// <summary>All tradeable symbol names loaded from market watch cache.</summary>
        public List<string> AvailableSymbols
        {
            get => _availableSymbols;
            set => SetProperty(ref _availableSymbols, value);
        }

        private string _selectedSymbol;

        /// <summary>
        /// Currently selected symbol name.
        /// Changing this triggers symbol data fetch and live tick re-subscription.
        /// </summary>
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
                        LiveBid = symbolInfo.SymbolBook?.bid.ToString()
                                  ?? 0m.ToString("F" + _currentDigits);
                        LiveAsk = symbolInfo.SymbolBook?.ask.ToString()
                                  ?? 0m.ToString("F" + _currentDigits);
                    }
                    _ = ManageLiveTicksAsync(value);
                }
            }
        }

        private SymbolData _currentSelectedSymbol;
        public SymbolData CurrentSelectedSymbol
        {
            get => _currentSelectedSymbol;
            set
            {
                if (SetProperty(ref _currentSelectedSymbol, value))
                {
                    OnPropertyChanged(nameof(IsExpiryVisible));
                }
            }
        }

        #endregion

        #region Pricing

        private string _liveBid = "0.00";

        /// <summary>Current live bid price as a formatted string.</summary>
        public string LiveBid
        {
            get => _liveBid;
            set => SetProperty(ref _liveBid, value);
        }

        private string _liveAsk = "0.00";

        /// <summary>Current live ask price as a formatted string.</summary>
        public string LiveAsk
        {
            get => _liveAsk;
            set => SetProperty(ref _liveAsk, value);
        }

        private string _limitRate = "0.00";

        /// <summary>User-entered limit/stop price. Only relevant for Limit and StopLimit orders.</summary>
        public string LimitRate
        {
            get => _limitRate;
            set => SetProperty(ref _limitRate, value);
        }

        #endregion

        #region Order Inputs

        private string _quantity = "0.00";

        /// <summary>Trade volume entered by the user.</summary>
        public string Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    UpdateCloseButtonCaption();
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>Available expiry options shown in the combo box.</summary>
        public List<string> ExpiryOptions { get; } = new List<string> { "GTC", "Today", "Specific Date" };

        private string _selectedExpiry = "GTC";

        /// <summary>User-selected order expiry type.</summary>
        public string SelectedExpiry
        {
            get => _selectedExpiry;
            set
            {
                if (SetProperty(ref _selectedExpiry, value))
                    OnPropertyChanged(nameof(IsSpecificDateVisible));
            }
        }

        private DateTime? _selectedExpiryDate = DateTime.Now;

        /// <summary>The specific expiry date, only relevant when <see cref="SelectedExpiry"/> is "Specific Date".</summary>
        public DateTime? SelectedExpiryDate
        {
            get => _selectedExpiryDate;
            set => SetProperty(ref _selectedExpiryDate, value);
        }

        #endregion

        #region Account Info

        private static readonly System.Globalization.NumberFormatInfo _amountFormat
    = new System.Globalization.NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = 2
    };

        private static string FormatAmount(double value)
            => value.ToString("N2", _amountFormat);

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _balance;
        public string Balance
        {
            get => _balance;
            set => SetProperty(ref _balance, value);
        }

        private string _credit;
        public string Credit
        {
            get => _credit;
            set => SetProperty(ref _credit, value);
        }

        private string _occupiedMargin;
        public string OccupiedMargin
        {
            get => _occupiedMargin;
            set => SetProperty(ref _occupiedMargin, value);
        }

        private string _freeMargin;
        public string FreeMargin
        {
            get => _freeMargin;
            set => SetProperty(ref _freeMargin, value);
        }

        #endregion

        #region Symbol Trading Parameters

        private string _minValue = "0.00";
        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        private string _stepValue = "0.00";
        public string StepValue
        {
            get => _stepValue;
            set => SetProperty(ref _stepValue, value);
        }

        private string _oneValue = "0.00";
        public string OneValue
        {
            get => _oneValue;
            set => SetProperty(ref _oneValue, value);
        }

        private string _totalValue = "0.00";
        public string TotalValue
        {
            get => _totalValue;
            set => SetProperty(ref _totalValue, value);
        }

        private string _limitStopValue = "0.00";
        public string LimitStopValue
        {
            get => _limitStopValue;
            set => SetProperty(ref _limitStopValue, value);
        }

        #endregion

        #region UI Display Properties (Computed)

        private string _closeButtonText;

        /// <summary>Dynamically composed text for the close-position button.</summary>
        public string CloseButtonText
        {
            get => _closeButtonText;
            set => SetProperty(ref _closeButtonText, value);
        }

        /// <summary>True when the window is in Modify/Delete mode (opened from an existing order).</summary>
        public bool IsModifyDeleteMode
            => CurrentWindowModeEnum == EnumTradeWindowMode.FromOrder
            && OriginalOrderType.HasValue
            && OriginalOrderType.Value == CurrentOrderTypeEnum;

        public string LeftActionText
            => IsModifyDeleteMode ? "MODIFY"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "SELL"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "SELL LIMIT"
            : "SELL STOPLIMIT";

        public string RightActionText
            => IsModifyDeleteMode ? "DELETE"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "BUY"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "BUY LIMIT"
            : "BUY STOPLIMIT";

        public bool IsCloseButtonVisible
            => positionGridRow != null
            && positionGridRow.IsPosition
            && CurrentOrderTypeEnum == EnumTradeOrderType.Market;

        public bool IsSymbolEditable => CurrentWindowModeEnum == EnumTradeWindowMode.FromTradeButton;
        public bool IsLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.Limit;
        public bool IsMarketActive => CurrentOrderTypeEnum == EnumTradeOrderType.Market;
        public bool IsStopLimitActive => CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit;
        public bool IsExpiryVisible => CurrentOrderTypeEnum != EnumTradeOrderType.Market && CurrentSelectedSymbol != null && string.Equals(CurrentSelectedSymbol.SecurityGTC, "GoodTillCancelled", StringComparison.OrdinalIgnoreCase);

        public bool IsSpecificDateVisible => SelectedExpiry == "Specific Date";

        public string SellButtonText
            => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "SELL"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "SELL LIMIT"
            : "SELL STOPLIMIT";

        public string BuyButtonText
            => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "BUY"
            : CurrentOrderTypeEnum == EnumTradeOrderType.Limit ? "BUY LIMIT"
            : "BUY STOPLIMIT";

        public string RateLabelText
            => CurrentOrderTypeEnum == EnumTradeOrderType.Market ? "Rate :" : "Limit Rate :";

        #endregion

        #region Execution State

        private bool _isProcessingOrDone;

        /// <summary>
        /// True while an order is being submitted or after it completes.
        /// Drives the form-disabled / result-panel visibility toggle.
        /// </summary>
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

        /// <summary>Result message displayed in the OK panel after order submission.</summary>
        public string TradeResultMessage
        {
            get => _tradeResultMessage;
            set => SetProperty(ref _tradeResultMessage, value);
        }

        #endregion
    }
}