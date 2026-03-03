using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using DocumentFormat.OpenXml.Vml.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientDesktop.ViewModel
{
    public partial class TradeViewModel : ViewModelBase
    {
        #region 1. Injected Services & Core Variables
        private readonly SessionService _sessionService;
        private readonly TradeService _tradeService;
        private readonly LiveTickService _liveTickService;

        private Dictionary<string, (int Id, int Digits)> _symbolMap = new Dictionary<string, (int Id, int Digits)>();
        private string _currentTickSymbol;
        private int _currentDigits = 2;
        #endregion

        #region 2. Constructor
        public TradeViewModel(SessionService sessionService, TradeService tradeService, LiveTickService liveTickService)
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
            _symbolMap = data.symbols.ToDictionary(
                s => (string)s.symbolName,
                s => ((int)s.symbolId, (int)s.symbolDigits)
            );
            AvailableSymbols = _symbolMap.Keys.ToList();
        }

        private void UpdateCloseButtonCaption()
        {
            if (!IsCloseButtonVisible || positionGridRow == null) return;

            string input = positionGridRow.Id;
            string first6 = !string.IsNullOrEmpty(input) && input.Length >= 6 ? input.Substring(0, 6) : input;
            string result = "#" + first6 + "...";

            string order = positionGridRow.Side?.ToLower() == "ask" ? "BUY" : "SELL";

            double.TryParse(LiveBid, out double currentBid);
            double.TryParse(LiveAsk, out double currentAsk);

            double value = order == "BUY" ? currentBid : currentAsk;
            string qtyStr = string.IsNullOrEmpty(Quantity) ? "0" : Quantity;
            double.TryParse(positionGridRow.AveragePrice?.ToString(), out double avgPrice);

            CloseButtonText = $"Close {result} {order} {qtyStr} {positionGridRow.SymbolName} {avgPrice.ToString("F" + _currentDigits)} at {value.ToString("F" + _currentDigits)}";
        }

        public async Task GetSymbolDataAsync(int symbolId)
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
            if (newType == EnumTradeOrderType.Market)
            {
                LimitRate = LiveAsk;
            }
            else
            {
                LimitRate = string.Empty;
            }
            OnPropertyChanged(nameof(IsCloseButtonVisible));
            UpdateCloseButtonCaption();
        }

        private async Task ManageLiveTicksAsync(string newSymbol)
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

                if (IsMarketActive)
                {
                    LimitRate = LiveAsk;
                }
                UpdateCloseButtonCaption();
            });
        }

        public async Task Cleanup()
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