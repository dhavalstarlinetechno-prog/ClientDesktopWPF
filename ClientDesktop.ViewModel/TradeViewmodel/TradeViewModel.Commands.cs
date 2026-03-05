using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public partial class TradeViewModel : ViewModelBase
    {
        public Action CloseAction { get; set; }

        private bool _isLastTradeSuccessful = false;

        #region 4. Commands
        public ICommand ChangeOrderTypeCommand => new AsyncRelayCommand(async param =>
        {
            if (param is string typeString && Enum.TryParse<EnumTradeOrderType>(typeString, true, out var newType))
            {
                SetOrderType(newType);
            }
        });

        public ICommand BuyCommand => new RelayCommand(async _ =>
        {
            if (IsModifyDeleteMode) await ExecuteTradeOperation(TradeConstants.ActionDelete);
            else await ExecuteTradeOperation(TradeConstants.ActionBuy);
        });

        public ICommand SellCommand => new RelayCommand(async _ =>
        {
            if (IsModifyDeleteMode) await ExecuteTradeOperation(TradeConstants.ActionModify);
            else await ExecuteTradeOperation(TradeConstants.ActionSell);
        });

        public ICommand ClosePositionCommand => new RelayCommand(async _ => await ExecuteTradeOperation(TradeConstants.ActionClose));

        public ICommand OkCommand => new RelayCommand(_ => ResetTradeWindow());
        #endregion

        #region 7. Trade Execution Logic
        private async Task ExecuteTradeOperation(string actionType)
        {
            IsProcessingOrDone = true;
            _isLastTradeSuccessful = false; 
            TradeResultMessage = "Processing Order...";

            try
            {
                var clientData = _sessionService.ClientListData?.Find(c => c.ClientId == _sessionService.UserId);
                string positionId = positionGridRow?.Id ?? "";

                string baseAction = (actionType == "BUY" || actionType == "MODIFY") ? "Buy" : "Sell";

                if (actionType != "CLOSE")
                {
                    bool isValid = ValidateTradeOrder(baseAction, positionId, clientData);
                    if (!isValid)
                    {
                        TradeResultMessage = "Validation Failed! Please check your inputs.";
                        return; 
                    }
                }

                // 3. Price and Volume nikal
                double.TryParse(Quantity, out double volume);
                double.TryParse(LimitRate, out double limitPrice);
                double.TryParse(LiveBid, out double bidPrice);
                double.TryParse(LiveAsk, out double askPrice);

                double currentPrice = actionType == "BUY" ? askPrice : bidPrice;

                var payload = BuildTradePayload(actionType, volume, currentPrice, limitPrice);

                if (payload == null)
                {
                    TradeResultMessage = "Failed to build order payload (Invalid Expiry Date).";
                    return;
                }

                bool isModify = actionType == "MODIFY";
                var result = await _tradeService.PlaceOrModifyOrderAsync(payload, isModify);

                // 6. Result Handle
                if (result.Success)
                {
                    _isLastTradeSuccessful = true;

                    if (actionType == "CLOSE")
                        TradeResultMessage = $"Position Closed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}";
                    else
                    {
                        string displayAction = actionType == "BUY" ? "BUY" : "Sell";
                        TradeResultMessage = $"{displayAction} Order placed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}";
                    }
                }
                else
                {
                    _isLastTradeSuccessful = false;

                    TradeResultMessage = $"Operation Failed!\nReason: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                TradeResultMessage = $"System Error: {ex.Message}";
            }
        }

        private object BuildTradePayload(string actionType, double volume, double currentPrice, double limitPrice)
        {
            string baseAction = (actionType == TradeConstants.ActionBuy ||
                               (actionType == TradeConstants.ActionModify && OriginalOrderType != null)) 
                               ? "Buy" : "Sell";

            string apiOrderType = actionType;
            if (CurrentOrderTypeEnum == EnumTradeOrderType.Limit) apiOrderType += "Limit";
            else if (CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit) apiOrderType += "Stop";
            else apiOrderType = CurrentOrderTypeEnum.ToString();

            string side = baseAction == "Buy" ? "ASK" : "BID";
            string symbolExpiry = GetSymbolExpiry();
            int symbolId = _symbolMap[SelectedSymbol].Id;

            if (actionType == TradeConstants.ActionClose && positionGridRow != null)
            {
                string reversedSide = side == "ASK" ? "BID" : "ASK";
                return new
                {
                    username = _sessionService.UserId,
                    symbolId = symbolId,
                    OrderFulfillment = "PENDING",
                    comment = "",
                    positionId = positionGridRow.Id,
                    deviceDetail = new { clientIP = CommonHelper.GetLocalIPAddress(), device = "", reason = "Client" }, 
                    marketInfo = new { symbolExpiry = symbolExpiry },
                    placeInstruction = new
                    {
                        orderType = apiOrderType,
                        side = reversedSide,
                        limitMarketOrder = new { price = currentPrice, volume = volume, currentPrice = currentPrice }
                    }
                };
            }

            return new
            {
                username = _sessionService.UserId,
                symbolId = symbolId,
                OrderFulfillment = "PENDING",
                comment = "",
                deviceDetail = new { clientIP = CommonHelper.GetLocalIPAddress(), device = "", reason = "Client" },
                marketInfo = new { symbolExpiry = symbolExpiry },
                placeInstruction = new
                {
                    orderType = apiOrderType,
                    side = side,
                    limitMarketOrder = new
                    {
                        price = CurrentOrderTypeEnum == EnumTradeOrderType.Market ? 0 : limitPrice,
                        volume = volume,
                        currentPrice = currentPrice
                    }
                }
            };
        }

        private string GetSymbolExpiry()
        {
            if (string.IsNullOrEmpty(SelectedExpiry) || SelectedExpiry == TradeConstants.ExpiryGtc)
                return null;

            if (SelectedExpiry == TradeConstants.ExpiryToday)
                return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            if (SelectedExpiry == TradeConstants.ExpirySpecificDate)
            {
                if (SelectedExpiryDate.HasValue)
                {
                    return SelectedExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                }

                return "INVALID"; 
            }

            return null;
        }

        private void ResetTradeWindow()
        {
            if (_isLastTradeSuccessful)
            {
                CloseAction?.Invoke();
            }
            else
            {
                IsProcessingOrDone = false;
                _isLastTradeSuccessful = false;
            }

            IsProcessingOrDone = false;
        }
        #endregion
    }
}