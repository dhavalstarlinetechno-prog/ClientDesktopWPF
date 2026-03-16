using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class PositionViewModel : INotifyPropertyChanged
    {
        private readonly SessionService _sessionService;
        private readonly PositionService _positionService;
        private readonly LiveTickService _liveTickService;
        private readonly ISocketService _socketService;
        private ObservableCollection<PositionGridRow> _gridRows;
        private readonly IDialogService _dialogService;
        private readonly HashSet<string> _subscribedSymbols = new HashSet<string>();
        private Guid _currentLoadId = Guid.Empty;


        private List<Position> _rawPositions = new List<Position>();
        private List<OrderModel> _rawOrders = new List<OrderModel>();

        public ICommand DoubleClickCommand { get; }
        public ObservableCollection<PositionGridRow> GridRows
        {
            get { return _gridRows; }
            set { _gridRows = value; OnPropertyChanged(); }
        }

        public PositionViewModel(SessionService sessionService, PositionService positionService, IDialogService dialogService, LiveTickService liveTickService , ISocketService socketService)
        {
            _sessionService = sessionService;
            _positionService = positionService;
            _dialogService = dialogService;
            _liveTickService = liveTickService;
            _socketService = socketService;

            GridRows = new ObservableCollection<PositionGridRow>();

            DoubleClickCommand = new RelayCommand(row => PositionGridDoubleClick((PositionGridRow?)row));

            RegisterMessenger();
            _liveTickService.OnTickReceived += HandleLiveTick;
            _liveTickService.OnReconnected += HandleLiveReconnected;
            _liveTickService.OnConnected += HandleLiveConnected;

            _socketService.OnPositionUpdated += HandlePositionUpdated;
            _socketService.OnOrderUpdated += HandleOrderUpdated;
            _socketService.OnUpdateUserBalance += HandleUserBalance;
            _socketService.OnSocketReconnected += HandleSocketReconnection;
        }

        /// <summary>
        /// Registers listeners for application-wide signals using the Event Aggregator.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    _currentLoadId = Guid.Empty;
                    Application.Current.Dispatcher.Invoke(() => GridRows?.Clear());
                    _subscribedSymbols.Clear();
                }
                else
                {
                    _subscribedSymbols.Clear();
                }
            });
        }

        public async void LoadDataAsync()
        {
            Guid thisLoadId = Guid.NewGuid();
            _currentLoadId = thisLoadId;

            try
            {
                var positionTask = _positionService.GetPositionsAsync();
                var orderTask = _positionService.GetOrdersAsync();

                await Task.WhenAll(positionTask, orderTask);

                if (_currentLoadId != thisLoadId || !_sessionService.IsLoggedIn)
                {
                    return;
                }

                var posResult = await positionTask;
                var ordResult = await orderTask;

                var tempRows = new ObservableCollection<PositionGridRow>();

                _rawPositions = posResult.Positions ?? new List<Position>();
                _rawOrders = ordResult.Orders ?? new List<OrderModel>();

                // --- A. ADD POSITIONS ---
                foreach (var pos in _rawPositions)
                {
                    string displaySide = pos.Side;
                    if (string.Equals(pos.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                    else if (string.Equals(pos.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

                    tempRows.Add(new PositionGridRow
                    {
                        Id = pos.Id,
                        SymbolId = pos.SymbolId,
                        SymbolDigit = pos.SymbolDigit,
                        SymbolName = pos.SymbolName,
                        Time = pos.LastInAt,
                        Side = displaySide,
                        OrderType = "Market",
                        Volume = pos.TotalVolume,
                        AveragePrice = pos.AveragePrice,
                        AveragePriceDisplay = pos.AveragePrice.ToString($"F{pos.SymbolDigit}"),
                        CurrentPrice = pos.CurrentPrice,
                        CurrentPriceDisplay = pos.CurrentPrice.ToString($"F{pos.SymbolDigit}"),
                        Pnl = pos.Pnl,
                        Type = RowType.Position
                    });
                }

                // --- B. ADD FOOTER (Summary) ---
                decimal totalPnl = _rawPositions.Sum(p => p.Pnl ?? 0);
                double balance = 50000.00;
                double credit = 1000.00;
                double equity = balance + credit + (double)totalPnl;
                double margin = 2000.00;
                double freeMargin = equity - margin;

                string footerText = $"Balance: {balance:N2}   Eq: {equity:N2}   Credit: {credit:N2}   Margin: {margin:N2}   Free: {freeMargin:N2}";

                tempRows.Add(new PositionGridRow
                {
                    SymbolName = footerText,
                    Type = RowType.Footer,
                    Pnl = totalPnl,
                    Volume = null,
                    AveragePrice = null,
                    CurrentPrice = null
                });

                // --- C. ADD ORDERS ---
                foreach (var ord in _rawOrders)
                {
                    string displaySide = ord.Side;
                    if (string.Equals(ord.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                    else if (string.Equals(ord.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

                    tempRows.Add(new PositionGridRow
                    {
                        Id = ord.OrderId,
                        SymbolId = ord.SymbolId,
                        SymbolDigit = ord.SymbolDigit,
                        SymbolName = ord.SymbolName,
                        Time = ord.UpdatedAt,
                        Side = displaySide,
                        OrderType = ord.OrderType,
                        Volume = ord.Volume,
                        AveragePrice = ord.Price,
                        AveragePriceDisplay = ord.Price.ToString($"F{ord.SymbolDigit}"),
                        CurrentPrice = ord.CurrentPrice,
                        CurrentPriceDisplay = ord.CurrentPrice.ToString($"F{ord.SymbolDigit}"),
                        Pnl = null,
                        Type = RowType.Order
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    GridRows = tempRows;
                });
                await SubscribeToSymbolAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data Load Error: " + ex.Message);
            }
        }

        private void PositionGridDoubleClick(PositionGridRow selectedRow)
        {
            if (selectedRow == null || selectedRow.Type == RowType.Footer)
                return;

            _dialogService.ShowDialog<TradeViewModel>(
                    "Trade Order",
                    configureViewModel: vm =>
                    {
                        _ = vm.LoadSymbolListAsync();

                        vm.positionGridRow = selectedRow;
                        vm.CurrentWindowModeEnum = selectedRow.Type == RowType.Position ? EnumTradeWindowMode.FromPosition : EnumTradeWindowMode.FromOrder;
                        vm.CurrentOrderTypeEnum = selectedRow.IsPosition ? EnumTradeOrderType.Market :
                              selectedRow.OrderType?.Contains("Stop", StringComparison.OrdinalIgnoreCase) == true ? EnumTradeOrderType.StopLimit :
                              selectedRow.OrderType?.Contains("Limit", StringComparison.OrdinalIgnoreCase) == true ? EnumTradeOrderType.Limit :
                              EnumTradeOrderType.Market;
                        vm.OriginalOrderType = !selectedRow.IsOrder ? null :
                              selectedRow.OrderType?.Contains("Stop", StringComparison.OrdinalIgnoreCase) == true ? EnumTradeOrderType.StopLimit :
                              selectedRow.OrderType?.Contains("Limit", StringComparison.OrdinalIgnoreCase) == true ? EnumTradeOrderType.Limit :
                              null;
                    }
                );
        }

        public void SortData(string sortBy, ListSortDirection direction)
        {
            var positions = GridRows.Where(r => r.Type == RowType.Position).ToList();
            var footer = GridRows.FirstOrDefault(r => r.Type == RowType.Footer);
            var orders = GridRows.Where(r => r.Type == RowType.Order).ToList();

            Func<PositionGridRow, object> keySelector = sortBy switch
            {
                "SymbolName" => r => r.SymbolName,
                "Time" => r => r.Time,
                "Side" => r => r.Side,
                "OrderType" => r => r.OrderType,
                "Volume" => r => r.Volume,
                "AveragePrice" => r => r.AveragePrice,
                "CurrentPrice" => r => r.CurrentPrice,
                "Pnl" => r => r.Pnl,
                _ => r => r.Id
            };

            if (direction == ListSortDirection.Ascending)
                positions = positions.OrderBy(keySelector).ToList();
            else
                positions = positions.OrderByDescending(keySelector).ToList();

            GridRows.Clear();
            foreach (var p in positions) GridRows.Add(p);
            if (footer != null) GridRows.Add(footer);
            foreach (var o in orders) GridRows.Add(o);
        }

        private async Task SubscribeToSymbolAsync()
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable) return;

            _subscribedSymbols.Clear();

            var symbolsToSubscribe = GridRows
                .Where(r => r.Type == RowType.Position || r.Type == RowType.Order)
                .Select(r => r.SymbolName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            foreach (var sym in symbolsToSubscribe)
            {
                await _liveTickService.SubscribeSymbolAsync(sym);
                _subscribedSymbols.Add(sym);
            }
        }

        private async Task UnsubscribeAllSymbolsAsync()
        {
            foreach (var symbol in _subscribedSymbols.ToList())
            {
                await _liveTickService.UnsubscribeSymbolAsync(symbol);
            }
            _subscribedSymbols.Clear();
        }

        private void HandleLiveTick(TickData tick)
        {
            if (string.IsNullOrEmpty(tick.SymbolName))
                return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Yahan Ab Position AUR Order dono ko filter karenge
                var rowsToUpdate = GridRows.Where(r =>
                    (r.Type == RowType.Position || r.Type == RowType.Order)
                    && r.SymbolName == tick.SymbolName).ToList();

                foreach (var row in rowsToUpdate)
                {
                    UpdateRowPrice(row, tick); // Naya method call kiya
                }

                UpdateFooterPnl();
            });
        }

        private async void HandleLiveConnected()
        {
            LoadDataAsync();
        }

        /// <summary>
        /// Handles the automatic re-subscription of symbols when SignalR reconnects after a network drop.
        /// </summary>
        private async void HandleLiveReconnected()
        {
            await SubscribeToSymbolAsync();
        }

        private void UpdateRowPrice(PositionGridRow row, TickData tick)
        {
            int digits = row.SymbolDigit;

            double newPrice;

            // LOGIC 1: Buy = Ask, Sell = Bid
            if (string.Equals(row.Side, "Buy", StringComparison.OrdinalIgnoreCase))
            {
                newPrice = tick.Ask;
            }
            else
            {
                newPrice = tick.Bid;
            }

            // COLOR LOGIC FOR PRICE (Purana price compare karo)
            double oldPrice = row.CurrentPrice ?? newPrice;
            if (newPrice > oldPrice)
            {
                row.PriceColor = "DodgerBlue"; 
            }
            else if (newPrice < oldPrice)
            {
                row.PriceColor = "Red";
            }

            // LOGIC 3: Decimal places strict formatting
            row.CurrentPrice = Math.Round(newPrice, digits);
            row.CurrentPriceDisplay = newPrice.ToString($"F{digits}"); // UI ko directly format de diya

            // LOGIC 2: Sirf Positions ka PNL calculate karo, Orders ka nahi
            if (row.Type == RowType.Position && row.AveragePrice.HasValue && row.Volume.HasValue)
            {
                double priceDifference;
                if (string.Equals(row.Side, "Buy", StringComparison.OrdinalIgnoreCase))
                {
                    priceDifference = newPrice - row.AveragePrice.Value;
                }
                else
                {
                    priceDifference = row.AveragePrice.Value - newPrice;
                }

                double calculatedPnl = CalculateFloatingPnl(tick.Bid, tick.Ask, row);
                row.Pnl = (decimal)Math.Round(calculatedPnl, 2);

                // COLOR LOGIC FOR PNL
                row.PnlColor = row.Pnl >= 0 ? "ForestGreen" : "Red";
            }
        }

        private void UpdateFooterPnl()
        {
            var footerRow = GridRows.FirstOrDefault(r => r.Type == RowType.Footer);
            if (footerRow == null)
                return;

            decimal totalPnl = GridRows
                .Where(r => r.Type == RowType.Position)
                .Sum(r => r.Pnl ?? 0);

            footerRow.Pnl = totalPnl;

            // Footer PNL ka color bhi update karo
            footerRow.PnlColor = totalPnl >= 0 ? "ForestGreen" : "Red";

            string summaryText = CalculatePositionFooterSummary((double)totalPnl);
            //string footerText = $"Balance: {balance:N2}   Eq: {equity:N2}   Credit: {credit:N2}   Margin: {margin:N2}   Free: {freeMargin:N2}";
            footerRow.SymbolName = summaryText;
        }

        public double CalculateFloatingPnl(double newBid, double newAsk, PositionGridRow row)
        {
            var position = _rawPositions.Find(q => q.Id == row.Id);

            if (position == null)
                return 0.0;

            // Safely extract values from position
            double spreadBalance = position.SpreadBalance ?? 0.0;
            double spreadValue = position.SpreadValue ?? 0.0;
            string spreadType = position.SpreadType ?? "Default";
            int digits = position.SymbolDigit;

            // Apply spread logic
            double bid = newBid;
            double ask = newAsk;

            switch (spreadType)
            {
                case "Default":
                    bid = AddSpreadBalance(bid, digits, spreadBalance);
                    ask = AddSpreadBalance(ask, digits, spreadBalance);
                    break;

                case "Fix":
                    bid = AddSpreadBalance(bid, digits, spreadBalance);
                    ask = AddSpreadBalance(bid, digits, spreadValue); // fixed distance from bid
                    break;

                case "Spread":
                    bid = AddSpreadBalance(bid, digits, spreadBalance);
                    ask = AddSpreadBalance(AddSpreadBalance(ask, digits, spreadBalance), digits, spreadValue);
                    break;
            }

            // Determine which price to use based on side
            double currentPrice = position.Side.Equals("ask", StringComparison.OrdinalIgnoreCase)
                ? (bid > 0 ? bid : position.CurrentPrice)
                : (ask > 0 ? ask : position.CurrentPrice);

            // Calculate PnL
            if (position.Side.Equals("ask", StringComparison.OrdinalIgnoreCase))
            {
                return (currentPrice - position.AveragePrice) *
                       position.TotalVolume *
                       position.SymbolContractSize;
            }
            else
            {
                return (position.AveragePrice - currentPrice) *
                       position.TotalVolume *
                       position.SymbolContractSize;
            }
        }

        public static double AddSpreadBalance(double odds, int symbolDigits, double spreadBalance)
        {
            double multiplier = Math.Pow(10.0, symbolDigits);
            return (odds * multiplier + spreadBalance) / multiplier;
        }

        private string CalculatePositionFooterSummary(double finalTotalPnl)
        {
            string tradeSummary = string.Empty;

            int maxWaitMs = 3000;
            int waited = 0;
            //while (!SessionManager.IsClientDataLoaded && waited < maxWaitMs)
            //{
            //    System.Threading.Thread.Sleep(200);
            //    waited += 200;
            //}

            var clientDetail = _sessionService.ClientListData.Where(q => q.ClientId == _sessionService.UserId).FirstOrDefault();
            if (clientDetail == null)
                return string.Empty;

            //if (skt_ClientDetail != null)
            //{
            //    clientDetail.CreditAmount = skt_ClientDetail.CreditAmount;
            //    clientDetail.UplineAmount = skt_ClientDetail.UplineAmount;
            //    clientDetail.Balance = skt_ClientDetail.Balance;
            //    clientDetail.OccupiedMarginAmount = skt_ClientDetail.OccupiedMarginAmount;
            //    clientDetail.UplineCommission = skt_ClientDetail.UplineCommission;
            //}

            double uplineAmount = clientDetail.UplineAmount;
            double uplineCommission = clientDetail.UplineCommission;
            double balance = clientDetail.Balance;

            //balanceForHistoryFooter = CalculateUplineBalance(clientDetail?.UplineAmount, clientDetail?.UplineCommission, clientDetail?.RealtimeCommission == true);

            double creditAmount = clientDetail.CreditAmount;
            var creditAmountForHistoryFooter = creditAmount;

            double occupiedMarginAmount = clientDetail.OccupiedMarginAmount;
            string freeMarginRule = clientDetail.FreeMargin?.ToLower() ?? "useopenpl";

            // 🔹 Compute balances
            double totalBalance = uplineAmount + uplineCommission + balance;
            double pnlContribution = 0;

            if (finalTotalPnl != 0)
            {
                if (freeMarginRule == "useopenpl")
                    pnlContribution = finalTotalPnl;
                else if (freeMarginRule == "useonlyopenprofit" && finalTotalPnl > 0)
                    pnlContribution = finalTotalPnl;
                else if (freeMarginRule == "useonlyopenloss" && finalTotalPnl < 0)
                    pnlContribution = finalTotalPnl;
            }

            double equity = uplineAmount + creditAmount + uplineCommission + finalTotalPnl;

            double freeMargin = uplineAmount + creditAmount + uplineCommission + pnlContribution - occupiedMarginAmount;
            tradeSummary =
                $"Balance: {CommonHelper.FormatAmount(totalBalance)}   " +
                $"Eq: {CommonHelper.FormatAmount(equity)}   " +
                $"Credit: {CommonHelper.FormatAmount(creditAmount)}   " +
                $"OM: {CommonHelper.FormatAmount(occupiedMarginAmount)}   " +
                $"FM: {CommonHelper.FormatAmount(freeMargin)}";

            return tradeSummary;
        }

        public static double CalculateUplineBalance(double? uplineAmount, double? uplineCommission, bool realtimeCommission)
        {
            double value = 0d;

            if (uplineAmount != null && uplineCommission != null)
                value = uplineAmount.Value +
                        (realtimeCommission ? uplineCommission.Value : 0d);
            else if (uplineAmount != null)
                value = uplineAmount.Value;
            else if (uplineCommission != null && realtimeCommission)
                value = uplineCommission.Value;

            return value;
        }

        #region Socket Specific Event Handlers

        private void HandleSocketReconnection()
        {
            // Jab socket reconnect ho, toh API se poora data wapas fetch aur refresh karlo
            Application.Current.Dispatcher.Invoke(() => LoadDataAsync());
        }

        private void HandlePositionUpdated(Position updatedPosition, bool isDeleted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isDeleted)
                {
                    // Memory se remove
                    _rawPositions.RemoveAll(p => p.Id == updatedPosition.Id);

                    // UI List se remove
                    var rowToRemove = GridRows.FirstOrDefault(r => r.Type == RowType.Position && r.Id == updatedPosition.Id);
                    if (rowToRemove != null) GridRows.Remove(rowToRemove);
                }
                else
                {
                    var existingRaw = _rawPositions.FirstOrDefault(p => p.Id == updatedPosition.Id);
                    if (existingRaw != null)
                    {
                        // Update Data in memory
                        int index = _rawPositions.IndexOf(existingRaw);
                        _rawPositions[index] = updatedPosition;

                        // Update Row in UI
                        var row = GridRows.FirstOrDefault(r => r.Type == RowType.Position && r.Id == updatedPosition.Id);
                        if (row != null)
                        {
                            row.AveragePrice = updatedPosition.AveragePrice;
                            row.AveragePriceDisplay = updatedPosition.AveragePrice.ToString($"F{updatedPosition.SymbolDigit}");
                            row.Volume = updatedPosition.TotalVolume;
                            // Note: CurrentPrice & Pnl will be handled naturally by the Next Live Tick
                        }
                    }
                    else
                    {
                        // Add New Position Row
                        _rawPositions.Add(updatedPosition);
                        string displaySide = string.Equals(updatedPosition.Side, "Bid", StringComparison.OrdinalIgnoreCase) ? "Sell" : "Buy";

                        var newRow = new PositionGridRow
                        {
                            Id = updatedPosition.Id,
                            SymbolId = updatedPosition.SymbolId,
                            SymbolDigit = updatedPosition.SymbolDigit,
                            SymbolName = updatedPosition.SymbolName,
                            Time = updatedPosition.CreatedAt ?? updatedPosition.LastInAt,
                            Side = displaySide,
                            OrderType = "Market",
                            Volume = updatedPosition.TotalVolume,
                            AveragePrice = updatedPosition.AveragePrice,
                            AveragePriceDisplay = updatedPosition.AveragePrice.ToString($"F{updatedPosition.SymbolDigit}"),
                            CurrentPrice = updatedPosition.CurrentPrice,
                            CurrentPriceDisplay = updatedPosition.CurrentPrice.ToString($"F{updatedPosition.SymbolDigit}"),
                            Pnl = updatedPosition.Pnl,
                            Type = RowType.Position
                        };

                        // Position ko Footer ke upar insert karna hai
                        var footerRow = GridRows.FirstOrDefault(r => r.Type == RowType.Footer);
                        int insertIndex = footerRow != null ? GridRows.IndexOf(footerRow) : GridRows.Count;
                        GridRows.Insert(insertIndex, newRow);

                        // SignalR ko bhi naye symbol ke liye subscribe kardo
                        _ = _liveTickService.SubscribeSymbolAsync(updatedPosition.SymbolName);
                    }
                }
                UpdateFooterPnl();
            });
        }

        private void HandleOrderUpdated(OrderModel updatedOrder, bool isDeleted, string orderId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isDeleted)
                {
                    _rawOrders.RemoveAll(o => o.OrderId == orderId);
                    var rowToRemove = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == orderId);
                    if (rowToRemove != null) GridRows.Remove(rowToRemove);
                }
                else
                {
                    var existingRaw = _rawOrders.FirstOrDefault(o => o.OrderId == updatedOrder.OrderId);
                    if (existingRaw != null)
                    {
                        // Update Data
                        int index = _rawOrders.IndexOf(existingRaw);
                        _rawOrders[index] = updatedOrder;

                        // Update Row
                        var row = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == updatedOrder.OrderId);
                        if (row != null)
                        {
                            row.AveragePrice = updatedOrder.Price;
                            row.AveragePriceDisplay = updatedOrder.Price.ToString($"F{updatedOrder.SymbolDigit}");
                            row.Volume = updatedOrder.Volume;
                            row.OrderType = updatedOrder.OrderType;
                            row.Time = DateTime.Now; // Update local time
                        }
                    }
                    else
                    {
                        // Add New Order
                        _rawOrders.Add(updatedOrder);
                        string displaySide = string.Equals(updatedOrder.Side, "Ask", StringComparison.OrdinalIgnoreCase) ? "Buy" : "Sell";

                        var newRow = new PositionGridRow
                        {
                            Id = updatedOrder.OrderId,
                            SymbolId = updatedOrder.SymbolId,
                            SymbolDigit = updatedOrder.SymbolDigit,
                            SymbolName = updatedOrder.SymbolName,
                            Time = DateTime.Now,
                            Side = displaySide,
                            OrderType = updatedOrder.OrderType,
                            Volume = updatedOrder.Volume,
                            AveragePrice = updatedOrder.Price,
                            AveragePriceDisplay = updatedOrder.Price.ToString($"F{updatedOrder.SymbolDigit}"),
                            CurrentPrice = updatedOrder.CurrentPrice,
                            CurrentPriceDisplay = updatedOrder.CurrentPrice.ToString($"F{updatedOrder.SymbolDigit}"),
                            Pnl = null,
                            Type = RowType.Order
                        };

                        // Orders grid ke bottom mein add honge
                        GridRows.Add(newRow);
                        _ = _liveTickService.SubscribeSymbolAsync(updatedOrder.SymbolName);
                    }
                }
            });
        }

        private void HandleUserBalance(ClientDetails details)
        {
            if (details == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var clientDetail = _sessionService.ClientListData.FirstOrDefault(q => q.ClientId == _sessionService.UserId);
                if (clientDetail != null)
                {
                    clientDetail.CreditAmount = details.CreditAmount;
                    clientDetail.UplineAmount = details.UplineAmount;
                    clientDetail.Balance = details.Balance;
                    clientDetail.OccupiedMarginAmount = details.OccupiedMarginAmount;
                    clientDetail.UplineCommission = details.UplineCommission;
                }
                UpdateFooterPnl(); // Session update hote hi footer refresh hoga
            });
        }

        #endregion

        public async void Cleanup()
        {
            await UnsubscribeAllSymbolsAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}