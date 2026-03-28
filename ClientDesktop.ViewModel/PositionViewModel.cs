using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services; 
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class PositionViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly PositionService _positionService;
        private readonly LiveTickService _liveTickService;
        private ObservableCollection<PositionGridRow> _gridRows;
        private readonly ISocketService _socketService; 

        private ListCollectionView _positionCollectionView;

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

        public ListCollectionView PositionCollectionView
        {
            get { return _positionCollectionView; }
            set { _positionCollectionView = value; OnPropertyChanged(); }
        }

        // 2. ISocketService Injected in Constructor
        public PositionViewModel(SessionService sessionService, PositionService positionService, IDialogService dialogService, LiveTickService liveTickService, ISocketService socketService)
        {
            _sessionService = sessionService;
            _positionService = positionService;
            _dialogService = dialogService;
            _liveTickService = liveTickService;
            _socketService = socketService;

            GridRows = new ObservableCollection<PositionGridRow>();
            PositionCollectionView = new ListCollectionView(GridRows);

            DoubleClickCommand = new RelayCommand(row => PositionGridDoubleClick((PositionGridRow?)row));

            RegisterMessenger();

            // Live Tick Events
            _liveTickService.OnTickReceived += HandleLiveTick;
            _liveTickService.OnReconnected += HandleLiveReconnected;
            _liveTickService.OnConnected += HandleLiveConnected;

            // 3. Socket Events Wired
            _socketService.OnPositionUpdated += HandlePositionUpdated;
            _socketService.OnOrderUpdated += HandleOrderUpdated;
            _socketService.OnUpdateUserBalance += HandleUserBalance;
            _socketService.OnSocketReconnected += HandleSocketReconnection;
        }

        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    LoadCachedDataAsync();
                    _currentLoadId = Guid.Empty;
                    _subscribedSymbols.Clear();
                }
                else
                {
                    _subscribedSymbols.Clear();
                }
            });
        }

        public async void LoadCachedDataAsync()
        {
            await Task.Run(() =>
            {
                var localPos = _positionService.GetCachedPositions();
                var localOrd = _positionService.GetCachedOrders();

                var list = BuildGridRows(localPos, localOrd);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _rawPositions = localPos;
                    _rawOrders = localOrd;
                    UpdateGridSilently(list);
                });
            });
        }

        public async void LoadDataAsync()
        {
            Guid thisLoadId = Guid.NewGuid();
            _currentLoadId = thisLoadId;

            if (_currentLoadId != thisLoadId || !_sessionService.IsLoggedIn) return;

            await Task.Run(async () =>
            {
                try
                {
                    var posResult = await _positionService.GetPositionsAsync().ConfigureAwait(false);
                    var ordResult = await _positionService.GetOrdersAsync().ConfigureAwait(false);

                    if (_currentLoadId != thisLoadId || !_sessionService.IsLoggedIn)
                    {
                        return;
                    }

                    var apiPos = posResult.Positions ?? new List<Position>();
                    var apiOrd = ordResult.Orders ?? new List<OrderModel>();

                    var list = BuildGridRows(apiPos, apiOrd);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _rawPositions = apiPos;
                        _rawOrders = apiOrd;
                        UpdateGridSilently(list);
                    }, System.Windows.Threading.DispatcherPriority.Background);

                    await SubscribeToSymbolAsync().ConfigureAwait(false);

                    // 4. Start Socket connection if not already connected
                    if (!_socketService.IsConnected)
                    {
                        _socketService.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Data Load Error: " + ex.Message);
                }
            });
        }

        private ObservableCollection<PositionGridRow> BuildGridRows(List<Position> positions, List<OrderModel> orders)
        {
            var list = new ObservableCollection<PositionGridRow>();

            // --- A. ADD POSITIONS ---
            foreach (var pos in positions)
            {
                string displaySide = pos.Side;
                if (string.Equals(pos.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                else if (string.Equals(pos.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";


                list.Add(new PositionGridRow
                {
                    Id = pos.Id,
                    SymbolId = pos.SymbolId,
                    SymbolDigit = pos.SymbolDigit,
                    SymbolName = pos.SymbolName,
                    Time = pos.LastInAt.HasValue? CommonHelper.ConvertUtcToIst(pos.LastInAt.Value): null,
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
            decimal totalPnl = positions.Sum(p => p.Pnl ?? 0);
            double balance = 0;
            double credit = 0;
            double equity = balance + credit + (double)totalPnl;
            double margin = 0;
            double freeMargin = equity - margin;

            string footerText = $"Balance: {balance:N2}   Eq: {equity:N2}   Credit: {credit:N2}   Margin: {margin:N2}   Free: {freeMargin:N2}";

            list.Add(new PositionGridRow
            {
                SymbolName = footerText,
                Type = RowType.Footer,
                Pnl = totalPnl,
                Volume = null,
                AveragePrice = null,
                CurrentPrice = null
            });

            // --- C. ADD ORDERS ---
            foreach (var ord in orders)
            {
                string displaySide = ord.Side;
                if (string.Equals(ord.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                else if (string.Equals(ord.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

                list.Add(new PositionGridRow
                {
                    Id = ord.OrderId,
                    SymbolId = ord.SymbolId,
                    SymbolDigit = ord.SymbolDigit,
                    SymbolName = ord.SymbolName,
                    Time = CommonHelper.ConvertUtcToIst(ord.UpdatedAt),
                    Side = displaySide,
                    OrderType = ord.OrderType,
                    Volume = ord.Volume,
                    AveragePrice = ord.Price,
                    AveragePriceDisplay = ord.Price.ToString($"F{ord.SymbolDigit}"),
                    CurrentPrice = ord.CurrentPrice,
                    CurrentPriceDisplay = ord.CurrentPrice.ToString($"F{ord.SymbolDigit}"),
                    Pnl = null,
                    SymbolExpiry = ord.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(ord.SymbolExpiry.Value) : null,
                    Type = RowType.Order
                });
            }

            return list;
        }

        private void UpdateGridSilently(ObservableCollection<PositionGridRow> newList)
        {
            var currentSort = PositionCollectionView?.CustomSort;

            GridRows = newList;
            PositionCollectionView = new ListCollectionView(GridRows);

            if (currentSort != null)
            {
                PositionCollectionView.CustomSort = currentSort;
            }
            OnPropertyChanged(nameof(PositionCollectionView));
        }

        #region Socket Handlers for Updating Local & UI

        private void HandleSocketReconnection()
        {
            Application.Current.Dispatcher.InvokeAsync(() => LoadDataAsync());
        }

        private void HandlePositionUpdated(Position updatedPosition, bool isDeleted)
        {   
            if (updatedPosition == null) return;

            Task.Run(() => _positionService.UpdateLocalPosition(updatedPosition, isDeleted));

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (isDeleted)
                {
                    _rawPositions.RemoveAll(p => p.Id == updatedPosition.Id);
                    var rowToRemove = GridRows.FirstOrDefault(r => r.Type == RowType.Position && r.Id == updatedPosition.Id);
                    if (rowToRemove != null) GridRows.Remove(rowToRemove);
                }
                else
                {
                    var existingPos = _rawPositions.FirstOrDefault(p => p.Id == updatedPosition.Id);
                    if (existingPos != null)
                    {
                        var index = _rawPositions.IndexOf(existingPos);
                        _rawPositions[index] = updatedPosition;

                        var row = GridRows.FirstOrDefault(r => r.Type == RowType.Position && r.Id == updatedPosition.Id);
                        if (row != null)
                        {
                            row.AveragePrice = updatedPosition.AveragePrice;
                            row.AveragePriceDisplay = updatedPosition.AveragePrice.ToString($"F{updatedPosition.SymbolDigit}");
                            row.Volume = updatedPosition.TotalVolume;
                            row.Time = updatedPosition.LastInAt.HasValue ? CommonHelper.ConvertUtcToIst(updatedPosition.LastInAt.Value) : updatedPosition.CreatedAt.HasValue ? CommonHelper.ConvertUtcToIst(updatedPosition.CreatedAt.Value) : null;
                            row.CurrentPrice = updatedPosition.CurrentPrice;
                            row.CurrentPriceDisplay = updatedPosition.CurrentPrice.ToString($"F{updatedPosition.SymbolDigit}");
                        }
                    }
                    else
                    {
                        _rawPositions.Add(updatedPosition);
                        string displaySide = updatedPosition.Side;
                        if (string.Equals(updatedPosition.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                        else if (string.Equals(updatedPosition.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

                        var newRow = new PositionGridRow
                        {
                            Id = updatedPosition.Id,
                            SymbolId = updatedPosition.SymbolId,
                            SymbolDigit = updatedPosition.SymbolDigit,
                            SymbolName = updatedPosition.SymbolName,
                            Time = updatedPosition.CreatedAt.HasValue? CommonHelper.ConvertUtcToIst(updatedPosition.CreatedAt.Value) : updatedPosition.LastInAt.HasValue ? CommonHelper.ConvertUtcToIst(updatedPosition.LastInAt.Value) : null,
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
                        var footerRow = GridRows.FirstOrDefault(r => r.Type == RowType.Footer);
                        int insertIndex = footerRow != null ? GridRows.IndexOf(footerRow) : GridRows.Count;
                        GridRows.Insert(insertIndex, newRow);
                    }
                }

                UpdateFooterPnl();
                _ = SubscribeToSymbolAsync();
            });
        }

        public void HandleOrderUpdated(OrderModel updatedOrder, bool isDeleted , string orderId)
        {
            if (updatedOrder == null) return;

            Task.Run(() => _positionService.UpdateLocalOrder(updatedOrder, isDeleted));

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (isDeleted)
                {
                    _rawOrders.RemoveAll(o => o.OrderId == orderId);
                    var rowToRemove = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == updatedOrder.OrderId);
                    if (rowToRemove != null) GridRows.Remove(rowToRemove);
                }
                else
                {
                    var existingOrder = _rawOrders.FirstOrDefault(o => o.OrderId == updatedOrder.OrderId);
                    if (existingOrder != null)
                    {
                        var index = _rawOrders.IndexOf(existingOrder);
                        _rawOrders[index] = updatedOrder;

                        var row = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == updatedOrder.OrderId);
                        if (row != null)
                        {
                            row.Time = updatedOrder.UpdatedAt;
                            row.OrderType = updatedOrder.OrderType;
                            row.Volume = updatedOrder.Volume;
                            row.AveragePrice = updatedOrder.Price;
                            row.AveragePriceDisplay = updatedOrder.Price.ToString($"F{updatedOrder.SymbolDigit}");
                            row.CurrentPrice = updatedOrder.CurrentPrice;
                            row.CurrentPriceDisplay = updatedOrder.CurrentPrice.ToString($"F{updatedOrder.SymbolDigit}");
                            row.SymbolExpiry = updatedOrder.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(updatedOrder.SymbolExpiry.Value) : null;
                        }
                    }
                    else
                    {
                        _rawOrders.Insert(0, updatedOrder);
                        string displaySide = updatedOrder.Side;
                        if (string.Equals(updatedOrder.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                        else if (string.Equals(updatedOrder.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

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
                            SymbolExpiry = updatedOrder.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(updatedOrder.SymbolExpiry.Value) : null, 
                            Type = RowType.Order
                        };
                        GridRows.Add(newRow);
                    }
                }

                _ = SubscribeToSymbolAsync();
            });
        }

        private void HandleUserBalance(ClientDetails details)
        {
            if (details == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_sessionService.CurrentClient != null)
                {
                    _sessionService.CurrentClient.CreditAmount = details.CreditAmount;
                    _sessionService.CurrentClient.UplineAmount = details.UplineAmount;
                    _sessionService.CurrentClient.Balance = details.Balance;
                    _sessionService.CurrentClient.OccupiedMarginAmount = details.OccupiedMarginAmount;
                    _sessionService.CurrentClient.UplineCommission = details.UplineCommission;
                }

                UpdateFooterPnl();
            });
        }

        #endregion

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
                        vm.LimitRate = selectedRow.AveragePrice.ToString();
                    }
               );
        }

        public void SortData(string sortBy, ListSortDirection direction)
        {
            if (PositionCollectionView != null)
            {
                PositionCollectionView.CustomSort = new PositionGridSorter(sortBy, direction);
            }
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
                var rowsToUpdate = GridRows.Where(r =>
                    (r.Type == RowType.Position || r.Type == RowType.Order)
                    && r.SymbolName == tick.SymbolName).ToList();

                foreach (var row in rowsToUpdate)
                {
                    UpdateRowPrice(row, tick);
                }

                UpdateFooterPnl();
            }, System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void HandleLiveConnected()
        {
            LoadDataAsync();
        }

        private async void HandleLiveReconnected()
        {
            await SubscribeToSymbolAsync();
        }

        private void UpdateRowPrice(PositionGridRow row, TickData tick)
        {
            int digits = row.SymbolDigit;
            double newPrice;

            if (string.Equals(row.Side, "Buy", StringComparison.OrdinalIgnoreCase))
            {
                newPrice = tick.Ask;
            }
            else
            {
                newPrice = tick.Bid;
            }

            double oldPrice = row.CurrentPrice ?? newPrice;
            if (newPrice > oldPrice)
            {
                row.PriceColor = "#009900";
            }
            else if (newPrice < oldPrice)
            {
                row.PriceColor = "#EF5350";
            }

            row.CurrentPrice = Math.Round(newPrice, digits);
            row.CurrentPriceDisplay = newPrice.ToString($"F{digits}");

            if (row.Type == RowType.Position && row.AveragePrice.HasValue && row.Volume.HasValue)
            {
                double calculatedPnl = CalculateFloatingPnl(tick.Bid, tick.Ask, row);
                row.Pnl = (decimal)Math.Round(calculatedPnl, 2);

                row.PnlColor = (!row.Pnl.HasValue || row.Pnl >= 0) ? "#009900" : "#EF5350";
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
            footerRow.PnlColor = totalPnl >= 0 ? "#009900" : "#EF5350";

            string summaryText = CalculatePositionFooterSummary((double)totalPnl);
            footerRow.SymbolName = summaryText;
        }

        public double CalculateFloatingPnl(double newBid, double newAsk, PositionGridRow row)
        {
            var position = _rawPositions.Find(q => q.Id == row.Id);

            if (position == null)
                return 0.0;

            double spreadBalance = position.SpreadBalance ?? 0.0;
            double spreadValue = position.SpreadValue ?? 0.0;
            string spreadType = position.SpreadType ?? "Default";
            int digits = position.SymbolDigit;

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
                    ask = AddSpreadBalance(bid, digits, spreadValue);
                    break;
                case "Spread":
                    bid = AddSpreadBalance(bid, digits, spreadBalance);
                    ask = AddSpreadBalance(AddSpreadBalance(ask, digits, spreadBalance), digits, spreadValue);
                    break;
            }

            double currentPrice = position.Side.Equals("ask", StringComparison.OrdinalIgnoreCase)
                ? (bid > 0 ? bid : position.CurrentPrice)
                : (ask > 0 ? ask : position.CurrentPrice);

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

            var clientDetail = _sessionService.CurrentClient;
            if (clientDetail == null)
                return string.Empty;

            double uplineAmount = clientDetail.UplineAmount;
            double uplineCommission = clientDetail.UplineCommission;
            double balance = clientDetail.Balance;
            double creditAmount = clientDetail.CreditAmount;
            double occupiedMarginAmount = clientDetail.OccupiedMarginAmount;
            string freeMarginRule = clientDetail.FreeMargin?.ToLower() ?? "useopenpl";

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

        public async void Cleanup()
        {
            await UnsubscribeAllSymbolsAsync();
        }

        private class PositionGridSorter : IComparer
        {
            private readonly string _sortBy;
            private readonly ListSortDirection _direction;

            public PositionGridSorter(string sortBy, ListSortDirection direction)
            {
                _sortBy = sortBy;
                _direction = direction;
            }

            public int Compare(object x, object y)
            {
                var row1 = x as PositionGridRow;
                var row2 = y as PositionGridRow;

                if (row1 == null || row2 == null) return 0;

                if (row1.IsOrder && !row2.IsOrder) return 1;
                if (!row1.IsOrder && row2.IsOrder) return -1;
                if (row1.IsOrder && row2.IsOrder) return 0;

                if (row1.IsFooter && !row2.IsFooter) return 1;
                if (!row1.IsFooter && row2.IsFooter) return -1;
                if (row1.IsFooter && row2.IsFooter) return 0;

                int result = 0;
                switch (_sortBy)
                {
                    case "SymbolName":
                        result = string.Compare(row1.SymbolName, row2.SymbolName, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "Time":
                        result = Nullable.Compare(row1.Time, row2.Time);
                        break;
                    case "Side":
                        result = string.Compare(row1.Side, row2.Side, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "OrderType":
                        result = string.Compare(row1.OrderType, row2.OrderType, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "Volume":
                        result = Nullable.Compare(row1.Volume, row2.Volume);
                        break;
                    case "AveragePrice":
                        result = Nullable.Compare(row1.AveragePrice, row2.AveragePrice);
                        break;
                    case "CurrentPrice":
                        result = Nullable.Compare(row1.CurrentPrice, row2.CurrentPrice);
                        break;
                    case "Pnl":
                        result = Nullable.Compare(row1.Pnl, row2.Pnl);
                        break;
                    default:
                        result = string.Compare(row1.Id, row2.Id, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                if (_direction == ListSortDirection.Descending)
                    result = -result;

                return result;
            }
        }
    }
}