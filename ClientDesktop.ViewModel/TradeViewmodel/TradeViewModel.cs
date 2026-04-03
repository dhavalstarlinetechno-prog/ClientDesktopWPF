using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for the trade order window.
    /// Handles order placement, modification, closure, and live price updates.
    /// </summary>
    public partial class TradeViewModel : ViewModelBase
    {
        #region Injected Services & Private State

        private readonly SessionService _sessionService;
        private readonly ITradeService _tradeService;
        private readonly LiveTickService _liveTickService;
        private readonly IDialogService _dialogService;

        /// <summary>Maps symbol display name → (Id, Digits, SymbolBook) for fast lookup.</summary>
        private Dictionary<string, (int Id, int Digits, SymbolBook SymbolBook)> _symbolMap
            = new Dictionary<string, (int Id, int Digits, SymbolBook SymbolBook)>();

        private string _currentTickSymbol;
        private int _currentDigits = 2;

        #endregion

        #region Constructor

        /// <param name="sessionService">Provides logged-in user session and client data.</param>
        /// <param name="tradeService">Trade API operations abstracted behind <see cref="ITradeService"/>.</param>
        /// <param name="liveTickService">Manages real-time price tick subscriptions.</param>
        /// <param name="dialogService">Shows modal dialogs (e.g. delete confirmation).</param>
        public TradeViewModel(
            SessionService sessionService,
            ITradeService tradeService,
            LiveTickService liveTickService,
            IDialogService dialogService)
        {
            try
            {
                _sessionService = sessionService;
                _tradeService = tradeService;
                _liveTickService = liveTickService;
                _dialogService = dialogService;

                SetUserAccountInfo();
                _liveTickService.OnTickReceived += HandleLiveTick;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TradeViewModel), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads all tradeable symbols from cached market watch data and populates
        /// <see cref="AvailableSymbols"/> and the internal symbol map.
        /// </summary>
        public async Task LoadSymbolListAsync()
        {
            try
            {
                var data = await _tradeService.GetMarketWatchDataAsync();
                _symbolMap = data.symbols.ToDictionary(
                    s => (string)s.symbolName,
                    s => ((int)s.symbolId, (int)s.symbolDigits, s.symbolBook));

                AvailableSymbols = _symbolMap.Keys.ToList();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadSymbolListAsync), ex);
            }
        }

        /// <summary>
        /// Fetches per-symbol trading parameters (min/step/total values) and updates bound properties.
        /// </summary>
        public async Task GetSymbolDataAsync(int symbolId)
        {
            try
            {
                var result = await _tradeService.GetSymbolDataAsync(_sessionService.UserId, symbolId);
                if (!result.Success || result.SymbolData == null) return;

                CurrentSelectedSymbol = result.SymbolData;
                MinValue = result.SymbolData.SymbolMinimumValue.ToString("0.##");
                StepValue = result.SymbolData.SymbolStepValue.ToString("0.##");
                OneValue = result.SymbolData.SymbolOneClickValue.ToString("0.##");
                TotalValue = result.SymbolData.SymbolTotalValue.ToString("0.##");
                LimitStopValue = result.SymbolData.SymbolLimitstoplevel.ToString("0.##");

                Quantity = positionGridRow != null
                    ? positionGridRow.Volume?.ToString()
                    : MinValue;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSymbolDataAsync), ex);
            }
        }

        /// <summary>
        /// Unsubscribes from live tick feed and detaches event handler.
        /// Must be called when the view is unloaded to prevent memory leaks.
        /// </summary>
        public async Task Cleanup()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentTickSymbol))
                    await _liveTickService.UnsubscribeSymbolAsync(_currentTickSymbol);

                _liveTickService.OnTickReceived -= HandleLiveTick;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Cleanup), ex);
            }
        }

        #endregion

        #region Private Methods

        private void SetUserAccountInfo()
        {
            try
            {
                if (_sessionService == null) return;

                var client = _sessionService.CurrentClient;
                if (client == null) return;

                UserName = client.ClientName;
                Balance = FormatAmount(client.UplineAmount);
                Credit = FormatAmount(client.CreditAmount);
                OccupiedMargin = FormatAmount(client.OccupiedMarginAmount);
                FreeMargin = FormatAmount(client.FreeMarginAmount);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SetUserAccountInfo), ex);
            }
        }

        private void SetOrderType(EnumTradeOrderType newType)
        {
            try
            {
                CurrentOrderTypeEnum = newType;

                if (newType == EnumTradeOrderType.Market)
                    LimitRate = LiveAsk;

                OnPropertyChanged(nameof(IsCloseButtonVisible));
                UpdateCloseButtonCaption();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SetOrderType), ex);
            }
        }

        private void UpdateCloseButtonCaption()
        {
            try
            {
                if (!IsCloseButtonVisible || positionGridRow == null) return;

                string orderIdPreview = !string.IsNullOrEmpty(positionGridRow.Id) && positionGridRow.Id.Length >= 6
                    ? "#" + positionGridRow.Id.Substring(0, 6) + "..."
                    : "#" + positionGridRow.Id;

                string orderDirection = positionGridRow.Side?.ToLower() == "ask" ? "BUY" : "SELL";

                double.TryParse(LiveBid, out double currentBid);
                double.TryParse(LiveAsk, out double currentAsk);
                double.TryParse(positionGridRow.AveragePrice?.ToString(), out double avgPrice);

                double closePrice = orderDirection == "BUY" ? currentBid : currentAsk;
                string qtyDisplay = string.IsNullOrEmpty(Quantity) ? "0" : Quantity;

                CloseButtonText = $"Close {orderIdPreview} {orderDirection} {qtyDisplay} " +
                                  $"{positionGridRow.SymbolName} {avgPrice.ToString("F" + _currentDigits)} " +
                                  $"at {closePrice.ToString("F" + _currentDigits)}";
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateCloseButtonCaption), ex);
            }
        }

        private async Task ManageLiveTicksAsync(string newSymbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newSymbol) || _currentTickSymbol == newSymbol) return;

                if (!string.IsNullOrEmpty(_currentTickSymbol))
                    await _liveTickService.UnsubscribeSymbolAsync(_currentTickSymbol);

                _currentTickSymbol = newSymbol;
                await _liveTickService.SubscribeSymbolAsync(_currentTickSymbol);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ManageLiveTicksAsync), ex);
            }
        }

        private void HandleLiveTick(TickData tick)
        {
            try
            {
                if (tick.SymbolName != _currentTickSymbol) return;

                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        LiveBid = tick.Bid.ToString($"F{_currentDigits}");
                        LiveAsk = tick.Ask.ToString($"F{_currentDigits}");

                        if (IsMarketActive)
                            LimitRate = LiveAsk;

                        UpdateCloseButtonCaption();
                    }
                    catch (Exception innerEx)
                    {
                        FileLogger.ApplicationLog(nameof(HandleLiveTick) + "_Dispatcher", innerEx);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleLiveTick), ex);
            }
        }

        /// <summary>
        /// Validates the trade order inputs against symbol rules and client account constraints.
        /// </summary>
        /// <returns>
        /// <c>IsValid = true</c> if all checks pass;
        /// otherwise <c>IsValid = false</c> with a user-facing <c>ErrorMessage</c>.
        /// </returns>
        private (bool IsValid, string ErrorMessage) ValidateTradeOrder(
            string orderType, string positionId, ClientDetails clientData)
        {
            try
            {
                if (!double.TryParse(Quantity, out double tradeQty) || tradeQty <= 0)
                    return (false, CommonMessages.EnterVolume);

                if ((CurrentOrderTypeEnum == EnumTradeOrderType.Limit ||
                     CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit) &&
                    (string.IsNullOrEmpty(LimitRate) ||
                     !double.TryParse(LimitRate, out double price) || price <= 0))
                    return (false, CommonMessages.EnterPrice);

                if (CurrentSelectedSymbol == null)
                    return (false, CommonMessages.SelectSymbol);

                if (!double.TryParse(LiveAsk, out _) || !double.TryParse(LiveBid, out _))
                    return (false, CommonMessages.PriceDataNotAvailable);

                if (tradeQty > CurrentSelectedSymbol.SymbolTotalValue)
                    return (false, CommonMessages.MaxLimitExceed + " " +
                                   CurrentSelectedSymbol.SymbolTotalValue.ToString("F2"));

                if (clientData == null)
                    return (false, CommonMessages.ClientDataNotFound);

                if (!clientData.ClientStatus)
                    return (false, CommonMessages.AccountBlocked);

                if (!clientData.EnableTrading)
                    return (false, CommonMessages.TradeDisabled);

                bool isNewTrade = string.IsNullOrEmpty(positionId);
                if (clientData.CloseOnlyTradeLock && isNewTrade)
                    return (false, CommonMessages.CloseOnly);

                if (CurrentSelectedSymbol.SymbolTrade == "Disabled")
                    return (false, CommonMessages.TradeDisabled);

                if ((CurrentOrderTypeEnum == EnumTradeOrderType.Limit ||
                     CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit) &&
                     CurrentSelectedSymbol.SymbolLimitstoplevel > 0)
                {
                    if (!double.TryParse(LimitRate, out double enteredPrice))
                        return (false, CommonMessages.InvalidPrice);

                    double currentMarketPrice = orderType == "Buy"
                        ? double.Parse(LiveAsk)
                        : double.Parse(LiveBid);

                    double limitStopLevel = CurrentSelectedSymbol.SymbolLimitstoplevel /
                                            Math.Pow(10, CurrentSelectedSymbol.SymbolDigits);

                    double minAllowed = currentMarketPrice - limitStopLevel;
                    double maxAllowed = currentMarketPrice + limitStopLevel;

                    if (enteredPrice >= minAllowed && enteredPrice <= maxAllowed)
                        return (false, CommonMessages.InvalidPrice);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ValidateTradeOrder), ex);
                return (false, "Validation Error: " + ex.Message);
            }
        }

        #endregion
    }
}