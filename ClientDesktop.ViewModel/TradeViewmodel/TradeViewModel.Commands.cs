using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using System.Diagnostics;
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
                try
                {
                    if (param is string typeString &&
                        Enum.TryParse<EnumTradeOrderType>(typeString, ignoreCase: true, out var newType))
                    {
                        SetOrderType(newType);
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(ChangeOrderTypeCommand), ex);
                }
            });

        /// <summary>
        /// In normal mode: places a BUY order.
        /// In Modify/Delete mode (right button = DELETE): deletes the existing order.
        /// </summary>
        public ICommand BuyCommand
            => _buyCommand ??= new RelayCommand(async _ =>
            {
                try
                {
                    string action = IsModifyDeleteMode
                        ? TradeConstants.ActionDelete
                        : TradeConstants.ActionBuy;

                    await ExecuteTradeOperation(action);
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(BuyCommand), ex);
                }
            },
            _ => CanBuyEnabled());


        /// <summary>
        /// In normal mode: places a SELL order.
        /// In Modify/Delete mode (left button = MODIFY): modifies the existing order.
        /// </summary>
        public ICommand SellCommand
            => _sellCommand ??= new RelayCommand(async _ =>
            {
                try
                {
                    string action = IsModifyDeleteMode
                        ? TradeConstants.ActionModify
                        : TradeConstants.ActionSell;

                    await ExecuteTradeOperation(action);
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(SellCommand), ex);
                }
            },
                 _ => CanModifyButton());

        /// <summary>Closes an open position. Only enabled when quantity matches the position volume exactly.</summary>
        public ICommand ClosePositionCommand
            => _closePositionCommand ??= new RelayCommand(
                async _ =>
                {
                    try
                    {
                        await ExecuteTradeOperation(TradeConstants.ActionClose);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(ClosePositionCommand), ex);
                    }
                },
                _ => CanClosePosition());

        /// <summary>Closes the result panel. Dismisses the window on success, resets form on failure.</summary>
        public ICommand OkCommand
            => _okCommand ??= new RelayCommand(_ =>
            {
                try
                {
                    ResetTradeWindow();
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(OkCommand), ex);
                }
            });

        #endregion

        #region Trade Execution

        private async Task ExecuteTradeOperation(string actionType)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Delete logic (unchanged)
                if (actionType == TradeConstants.ActionDelete &&
                    positionGridRow != null && positionGridRow.IsOrder)
                {
                    FileLogger.Log("Trade", $"Delete requested | User: '{_sessionService?.UserId}' | OrderId: {positionGridRow.Id} | Symbol: {positionGridRow.SymbolName}");

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

                        FileLogger.Log("Trade", $"Delete result | User: '{_sessionService?.UserId}' | OrderId: {positionGridRow.Id} | Success: {_isLastTradeSuccessful} | Message: {deleteVm.deleteMessage}");
                    }
                    return;
                }

                IsProcessingOrDone = true;
                _isLastTradeSuccessful = false;
                TradeResultMessage = "Processing Order...";

                var clientData = _sessionService.CurrentClient;
                string positionId = positionGridRow?.Id ?? string.Empty;

                string baseAction = (actionType == TradeConstants.ActionBuy ||
                                     actionType == TradeConstants.ActionModify)
                                     ? "buy" : "sell";

                if (actionType != TradeConstants.ActionClose)
                {
                    var (isValid, errorMessage) = ValidateTradeOrder(baseAction, positionId, clientData);
                    if (!isValid)
                    {
                        TradeResultMessage = errorMessage;
                        return;
                    }
                }

                double.TryParse(Quantity, out double volume);
                double.TryParse(LimitRate, out double limitPrice);
                double.TryParse(LiveBid, out double bidPrice);
                double.TryParse(LiveAsk, out double askPrice);

                double currentPrice = actionType == TradeConstants.ActionBuy ? askPrice : bidPrice;

                FileLogger.Log("Trade", $"Order submitting | User: '{_sessionService?.UserId}' | Action: {actionType} | Symbol: {SelectedSymbol} | Volume: {volume} | OrderType: {CurrentOrderTypeEnum} | Price: {currentPrice} | LimitRate: {limitPrice} | PositionId: '{positionId}'");

                var payload = BuildTradePayload(actionType, volume, currentPrice, limitPrice);
                if (payload == null)
                {
                    FileLogger.Log("Trade", $"Order aborted | User: '{_sessionService?.UserId}' | Action: {actionType} | Symbol: {SelectedSymbol} | Reason: PayloadBuildFailed");
                    TradeResultMessage = "Failed to build order payload.";
                    return;
                }

                bool isModify = actionType == TradeConstants.ActionModify;

                var (success, error, _) = await _tradeService.PlaceOrModifyOrderAsync(payload, isModify);

                stopwatch.Stop();

                if (success)
                {
                    _isLastTradeSuccessful = true;

                    TradeResultMessage = actionType == TradeConstants.ActionClose
                        ? $"Position closed successfully!\nSymbol: {SelectedSymbol}\nPrice: {LimitRate}"
                        : $"{baseAction.ToUpper()} order placed successfully!\nSymbol: {SelectedSymbol}\nPrice: {LimitRate}";

                    // ✅ Call journal logger
                    LogTradeJournal(
                        actionType,
                        SelectedSymbol,
                        volume,
                        currentPrice,
                        stopwatch.ElapsedMilliseconds
                    );
                }
                else
                {
                    FileLogger.Log("Trade", $"Order rejected by server | User: '{_sessionService?.UserId}' | Action: {actionType} | Symbol: {SelectedSymbol} | Volume: {volume} | Price: {currentPrice} | Reason: {error} | ElapsedMs: {stopwatch.ElapsedMilliseconds}");
                    TradeResultMessage = $"Operation failed!\nReason: {error}";
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExecuteTradeOperation), ex);
                TradeResultMessage = $"System error: {ex.Message}";
            }
        }

        private bool CanClosePosition()
        {
            try
            {
                if (positionGridRow == null) return false;
                if (!double.TryParse(Quantity, out double qty)) return false;
                return qty == positionGridRow.Volume;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CanClosePosition), ex);
                return false;
            }
        }

        private bool CanBuyEnabled()
        {
            try
            {
                if (IsSymbolBanned) return false;
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CanBuyEnabled), ex);
                return false;
            }
        }

        private bool CanModifyButton()
        {
            try
            {
                if (IsSymbolBanned) return false;
                if (positionGridRow == null) return true;
                if (IsModifyDeleteMode && positionGridRow.IsOrder && double.TryParse(Quantity, out double qty))
                    return (qty != positionGridRow.Volume || LimitRate != positionGridRow.AveragePrice.ToString());
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CanClosePosition), ex);
                return false;
            }
        }

        private object BuildTradePayload(string actionType, double volume, double currentPrice, double limitPrice)
        {
            try
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

                int symbolId = 0;
                if (_symbolMap != null && !string.IsNullOrEmpty(SelectedSymbol) && _symbolMap.ContainsKey(SelectedSymbol))
                {
                    symbolId = _symbolMap[SelectedSymbol].Id;
                }

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

                if (actionType == TradeConstants.ActionModify && positionGridRow != null && positionGridRow.IsOrder)
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BuildTradePayload), ex);
                return null;
            }
        }

        private string GetSymbolExpiry()
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSymbolExpiry), ex);
                return null;
            }
        }

        private void ResetTradeWindow()
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ResetTradeWindow), ex);
            }
        }

        #endregion

        #region Trade Logs

        /// <summary>
        /// Writes a multi-line MT5-style journal entry for a completed trade operation.
        /// Covers: order request → accepted → deal executed → final order summary with execution time.
        /// </summary>
        private void LogTradeJournal(string actionType, string symbol, double volume, double price, long executionMs)
        {
            try
            {
                string accountId = _sessionService?.UserId ?? "UNKNOWN";

                string action = actionType == TradeConstants.ActionBuy ? "buy"
                             : actionType == TradeConstants.ActionSell ? "sell"
                             : actionType == TradeConstants.ActionClose ? "close"
                             : "modify";

                string timeStamp = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff");

                string volumeStr = volume.ToString("0.##");
                string priceStr = price.ToString("0.#####");

                // positionGridRow can be null for brand-new orders — safe-fall to "NEW"
                string orderId = positionGridRow?.Id ?? "NEW";
                string dealId = positionGridRow?.Id ?? "NEW";

                // 1. Order request
                FileLogger.Log("Trade",
                    $"'{accountId}': market {action} {volumeStr} {symbol}");

                // 2. Accepted
                FileLogger.Log("Trade",
                    $"'{accountId}': accepted market {action} {volumeStr} {symbol}");

                // 3. Deal executed
                ////FileLogger.Log("Trade",
                ////    $"'{accountId}': deal #{dealId} {action} {volumeStr} {symbol} at {priceStr} done (based on order #{orderId})");

                ////// 4. Final order with execution time
                ////FileLogger.Log("Trade",
                ////    $"'{accountId}': order #{orderId} {action} {volumeStr} / {volumeStr} {symbol} at {priceStr} done in {executionMs}.{new Random().Next(100, 999)} ms");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LogTradeJournal), ex);
            }
        }

        #endregion
    }
}