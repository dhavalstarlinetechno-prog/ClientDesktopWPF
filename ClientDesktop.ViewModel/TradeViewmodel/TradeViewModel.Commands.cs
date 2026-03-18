using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
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
        /// <summary>Callback invoked to close the host window on success.</summary>
        public Action CloseAction { get; set; }

        private bool _isLastTradeSuccessful;

        #region Commands (Lazy-Initialised)

        private ICommand _changeOrderTypeCommand;
        private ICommand _buyCommand;
        private ICommand _sellCommand;
        private ICommand _closePositionCommand;
        private ICommand _okCommand;

        /// <summary>Switches between Market / Limit / StopLimit. Expects the enum name as string parameter.</summary>
        public ICommand ChangeOrderTypeCommand
            => _changeOrderTypeCommand ??= new AsyncRelayCommand(async param =>
            {
                if (param is string typeString &&
                    Enum.TryParse<EnumTradeOrderType>(typeString, ignoreCase: true, out var newType))
                {
                    SetOrderType(newType);
                }
            });

        /// <summary>
        /// In normal mode: places a BUY order.
        /// In Modify/Delete mode (right button = DELETE): deletes the existing order.
        /// </summary>
        public ICommand BuyCommand
            => _buyCommand ??= new RelayCommand(async _ =>
            {
                string action = IsModifyDeleteMode
                    ? TradeConstants.ActionDelete
                    : TradeConstants.ActionBuy;

                await ExecuteTradeOperation(action);
            });

        /// <summary>
        /// In normal mode: places a SELL order.
        /// In Modify/Delete mode (left button = MODIFY): modifies the existing order.
        /// </summary>
        public ICommand SellCommand
            => _sellCommand ??= new RelayCommand(async _ =>
            {
                string action = IsModifyDeleteMode
                    ? TradeConstants.ActionModify
                    : TradeConstants.ActionSell;

                await ExecuteTradeOperation(action);
            });

        /// <summary>Closes an open position. Only enabled when quantity matches the position volume exactly.</summary>
        public ICommand ClosePositionCommand
            => _closePositionCommand ??= new RelayCommand(
                async _ => await ExecuteTradeOperation(TradeConstants.ActionClose),
                _ => CanClosePosition());

        /// <summary>Closes the result panel. Dismisses the window on success, resets form on failure.</summary>
        public ICommand OkCommand
            => _okCommand ??= new RelayCommand(_ => ResetTradeWindow());

        #endregion

        #region Trade Execution

        private async Task ExecuteTradeOperation(string actionType)
        {
            // Delete is handled through the dedicated DeleteTradeViewModel dialog
            if (actionType == TradeConstants.ActionDelete &&
                positionGridRow != null && positionGridRow.IsOrder)
            {
                DeleteTradeViewModel deleteVm = null;
                _dialogService.ShowDialog<DeleteTradeViewModel>(
                    "Delete Trade Order",
                    configureViewModel: vm =>
                    {
                        deleteVm = vm;
                        vm.OrderId = positionGridRow.Id;
                    });

                if (deleteVm != null)
                {
                    IsProcessingOrDone = deleteVm.isDeleted.HasValue;
                    _isLastTradeSuccessful = deleteVm.isDeleted ?? false;
                    TradeResultMessage = deleteVm.deleteMessage;
                }
                return;
            }

            IsProcessingOrDone = true;
            _isLastTradeSuccessful = false;
            TradeResultMessage = "Processing Order...";

            try
            {
                var clientData = _sessionService.ClientListData?.Find(c => c.ClientId == _sessionService.UserId);
                string positionId = positionGridRow?.Id ?? string.Empty;
                string baseAction = (actionType == TradeConstants.ActionBuy ||
                                      actionType == TradeConstants.ActionModify)
                                      ? "Buy" : "Sell";

                if (actionType != TradeConstants.ActionClose)
                {
                    var (isValid, errorMessage) = ValidateTradeOrder(baseAction, positionId, clientData);
                    if (!isValid)
                    {
                        TradeResultMessage = errorMessage; // ← now shows the specific message
                        return;
                    }
                }

                double.TryParse(Quantity, out double volume);
                double.TryParse(LimitRate, out double limitPrice);
                double.TryParse(LiveBid, out double bidPrice);
                double.TryParse(LiveAsk, out double askPrice);

                double currentPrice = actionType == TradeConstants.ActionBuy ? askPrice : bidPrice;

                var payload = BuildTradePayload(actionType, volume, currentPrice, limitPrice);
                if (payload == null)
                {
                    TradeResultMessage = "Failed to build order payload (invalid expiry date).";
                    return;
                }

                bool isModify = actionType == TradeConstants.ActionModify;
                var (success, error, _) = await _tradeService.PlaceOrModifyOrderAsync(payload, isModify);

                if (success)
                {
                    _isLastTradeSuccessful = true;
                    TradeResultMessage = actionType == TradeConstants.ActionClose
                        ? $"Position closed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}"
                        : $"{baseAction} order placed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}";
                }
                else
                {
                    TradeResultMessage = $"Operation failed!\nReason: {error}";
                }
            }
            catch (Exception ex)
            {
                TradeResultMessage = $"System error: {ex.Message}";
            }
        }

        private bool CanClosePosition()
        {
            if (positionGridRow == null) return false;
            if (!double.TryParse(Quantity, out double qty)) return false;
            return qty == positionGridRow.Volume;
        }

        private object BuildTradePayload(string actionType, double volume, double currentPrice, double limitPrice)
        {
            string baseAction = (actionType == TradeConstants.ActionBuy ||
                                (actionType == TradeConstants.ActionModify && OriginalOrderType != null))
                                ? "Buy" : "Sell";

            string apiOrderType = baseAction;
            if (CurrentOrderTypeEnum == EnumTradeOrderType.Limit) apiOrderType += "Limit";
            else if (CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit) apiOrderType += "Stop";
            else apiOrderType = CurrentOrderTypeEnum.ToString();

            string side = baseAction == "Buy" ? "ASK" : "BID";
            string symbolExpiry = GetSymbolExpiry();
            int symbolId = _symbolMap[SelectedSymbol].Id;

            if (actionType == TradeConstants.ActionClose && positionGridRow != null)
            {
                string reversedSide = positionGridRow.Side == "Buy" ? "BID" : "ASK";
                return new
                {
                    username = _sessionService.UserId,
                    symbolId,
                    OrderFulfillment = "PENDING",
                    comment = "",
                    positionId = positionGridRow.Id,
                    deviceDetail = new { clientIP = CommonHelper.GetLocalIPAddress(), device = "", reason = "Client" },
                    marketInfo = new { symbolExpiry },
                    placeInstruction = new
                    {
                        orderType = apiOrderType,
                        side = reversedSide,
                        limitMarketOrder = new { price = currentPrice, volume, currentPrice }
                    }
                };
            }

            if (actionType == TradeConstants.ActionModify && positionGridRow.IsOrder)
            {
                apiOrderType = positionGridRow.Side;
                if (CurrentOrderTypeEnum == EnumTradeOrderType.Limit) apiOrderType += "Limit";
                else if (CurrentOrderTypeEnum == EnumTradeOrderType.StopLimit) apiOrderType += "Stop";
                else apiOrderType = CurrentOrderTypeEnum.ToString();

                return new
                {
                    orderId = positionGridRow.Id,
                    username = _sessionService.UserId,
                    symbolId,
                    placeInstruction = new
                    {
                        orderType = apiOrderType,
                        side = positionGridRow.Side.ToUpper() == "SELL" ? "BID" : "ASK",
                        limitMarketOrder = new { price = limitPrice, volume, currentPrice }
                    },
                    marketInfo = new { symbolExpiry },
                    deviceDetail = new { clientIP = CommonHelper.GetLocalIPAddress(), device = "", reason = "Client" },
                    OrderFulfillment = "PENDING",
                    comment = ""
                };
            }

            return new
            {
                username = _sessionService.UserId,
                symbolId,
                OrderFulfillment = "PENDING",
                comment = "",
                deviceDetail = new { clientIP = CommonHelper.GetLocalIPAddress(), device = "", reason = "Client" },
                marketInfo = new { symbolExpiry },
                placeInstruction = new
                {
                    orderType = apiOrderType,
                    side,
                    limitMarketOrder = new
                    {
                        price = CurrentOrderTypeEnum == EnumTradeOrderType.Market ? 0 : limitPrice,
                        volume,
                        currentPrice
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
                return SelectedExpiryDate.HasValue
                    ? SelectedExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")
                    : "INVALID";
            }

            return null;
        }

        private void ResetTradeWindow()
        {
            if (_isLastTradeSuccessful)
            {
                CloseAction?.Invoke();
                // Window is closed — no further property changes needed
            }
            else
            {
                IsProcessingOrDone = false; // ← only reset form when trade failed
                _isLastTradeSuccessful = false;
            }
        }

        #endregion
    }
}