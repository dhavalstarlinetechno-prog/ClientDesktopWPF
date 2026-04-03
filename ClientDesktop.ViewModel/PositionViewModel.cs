using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the positions and orders view.
    /// </summary>
    public class PositionViewModel : ViewModelBase
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly PositionService _positionService;
        private readonly LiveTickService _liveTickService;
        private readonly ISocketService _socketService;
        private readonly IDialogService _dialogService;
        private readonly HashSet<string> _subscribedSymbols = new HashSet<string>();

        private ObservableCollection<PositionGridRow> _gridRows;
        private ListCollectionView _positionCollectionView;
        private Guid _currentLoadId = Guid.Empty;
        private List<Position> _rawPositions = new List<Position>();
        private List<OrderModel> _rawOrders = new List<OrderModel>();

        #endregion

        #region Properties

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

        #endregion

        #region Commands

        public ICommand DoubleClickCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PositionViewModel class.
        /// </summary>
        public PositionViewModel(SessionService sessionService, PositionService positionService, IDialogService dialogService, LiveTickService liveTickService, ISocketService socketService)
        {
            _sessionService = sessionService;
            _positionService = positionService;
            _dialogService = dialogService;
            _liveTickService = liveTickService;
            _socketService = socketService;

            GridRows = new ObservableCollection<PositionGridRow>();
            PositionCollectionView = new ListCollectionView(GridRows);
            PositionCollectionView.CustomSort = new PositionGridSorter("Time", ListSortDirection.Descending);

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads cached position and order data asynchronously.
        /// </summary>
        public async void LoadCachedDataAsync()
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadCachedDataAsync), ex);
            }
        }

        /// <summary>
        /// Loads live position and order data from the API asynchronously.
        /// </summary>
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

                    if (!_socketService.IsConnected)
                    {
                        _socketService.Start();
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(LoadDataAsync), ex);
                }
            });
        }

        /// <summary>
        /// Handles updates for order data received from the socket.
        /// </summary>
        public void HandleOrderUpdated(OrderModel updatedOrder, bool isDeleted, string orderId)
        {
            try
            {
                Task.Run(() => _positionService.UpdateLocalOrder(updatedOrder, isDeleted));

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (isDeleted)
                    {
                        _rawOrders.RemoveAll(o => o.OrderId == orderId);
                        var rowToRemove = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == orderId);
                        if (rowToRemove != null) GridRows.Remove(rowToRemove);
                    }
                    else
                    {
                        var existingOrder = _rawOrders.FirstOrDefault(o => o.OrderId == updatedOrder.OrderId);
                        if (existingOrder != null)
                        {
                            var index = _rawOrders.IndexOf(existingOrder);
                            _rawOrders[index] = updatedOrder;

                            var row = GridRows.FirstOrDefault(r => r.Type == RowType.Order && r.Id == orderId);
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleOrderUpdated), ex);
            }
        }

        /// <summary>
        /// Sets the sorting logic for the grid view.
        /// </summary>
        public void SortData(string sortBy, ListSortDirection direction)
        {
            try
            {
                if (PositionCollectionView != null)
                {
                    PositionCollectionView.CustomSort = new PositionGridSorter(sortBy, direction);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SortData), ex);
            }
        }

        /// <summary>
        /// Calculates the floating profit and loss for a specific position.
        /// </summary>
        public double CalculateFloatingPnl(double newBid, double newAsk, PositionGridRow row)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CalculateFloatingPnl), ex);
                return 0.0;
            }
        }

        /// <summary>
        /// Calculates the adjusted price by adding the spread balance.
        /// </summary>
        public static double AddSpreadBalance(double odds, int symbolDigits, double spreadBalance)
        {
            try
            {
                double multiplier = Math.Pow(10.0, symbolDigits);
                return (odds * multiplier + spreadBalance) / multiplier;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddSpreadBalance), ex);
                return odds;
            }
        }

        /// <summary>
        /// Calculates the upline balance based on client data.
        /// </summary>
        public static double CalculateUplineBalance(double? uplineAmount, double? uplineCommission, bool realtimeCommission)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CalculateUplineBalance), ex);
                return 0d;
            }
        }

        /// <summary>
        /// Cleans up resources by unsubscribing from all symbols.
        /// </summary>
        public async void Cleanup()
        {
            try
            {
                await UnsubscribeAllSymbolsAsync();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Cleanup), ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers listeners for application-wide signals.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, (recipient, message) =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(RegisterMessenger), ex);
                }
            });
        }

        /// <summary>
        /// Builds the grid rows from raw positions and orders data.
        /// </summary>
        private ObservableCollection<PositionGridRow> BuildGridRows(List<Position> positions, List<OrderModel> orders)
        {
            var list = new ObservableCollection<PositionGridRow>();

            try
            {
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
                        Time = pos.LastInAt.HasValue ? CommonHelper.ConvertUtcToIst(pos.LastInAt.Value) : null,
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
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BuildGridRows), ex);
            }

            return list;
        }

        /// <summary>
        /// Updates the collection view silently without breaking current sorting.
        /// </summary>
        private void UpdateGridSilently(ObservableCollection<PositionGridRow> newList)
        {
            try
            {
                var currentSort = PositionCollectionView?.CustomSort;

                GridRows = newList;
                PositionCollectionView = new ListCollectionView(GridRows);

                PositionCollectionView.CustomSort = currentSort ?? new PositionGridSorter("Time", ListSortDirection.Descending);
                OnPropertyChanged(nameof(PositionCollectionView));
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateGridSilently), ex);
            }
        }

        /// <summary>
        /// Initiates data loading process upon socket reconnection.
        /// </summary>
        private void HandleSocketReconnection()
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() => LoadDataAsync());
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleSocketReconnection), ex);
            }
        }

        /// <summary>
        /// Handles position updates received from the socket.
        /// </summary>
        private void HandlePositionUpdated(Position updatedPosition, bool isDeleted)
        {
            if (updatedPosition == null) return;

            try
            {
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
                                PositionCollectionView?.Refresh();
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
                                Time = updatedPosition.CreatedAt.HasValue ? CommonHelper.ConvertUtcToIst(updatedPosition.CreatedAt.Value) : updatedPosition.LastInAt.HasValue ? CommonHelper.ConvertUtcToIst(updatedPosition.LastInAt.Value) : null,
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
                            GridRows.Insert(0, newRow);
                        }
                    }

                    UpdateFooterPnl();
                    _ = SubscribeToSymbolAsync();
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandlePositionUpdated), ex);
            }
        }

        /// <summary>
        /// Processes updates for the user balance details.
        /// </summary>
        private void HandleUserBalance(ClientDetails details)
        {
            if (details == null) return;

            try
            {
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleUserBalance), ex);
            }
        }

        /// <summary>
        /// Handles the double click action on a grid row to open the trade window.
        /// </summary>
        private void PositionGridDoubleClick(PositionGridRow selectedRow)
        {
            if (selectedRow == null || selectedRow.Type == RowType.Footer)
                return;

            try
            {
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
                            vm.LimitRate = selectedRow.AveragePrice?.ToString($"F{selectedRow.SymbolDigit}");
                        }
                   );
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PositionGridDoubleClick), ex);
            }
        }

        /// <summary>
        /// Subscribes to live ticks for all currently displayed symbols.
        /// </summary>
        private async Task SubscribeToSymbolAsync()
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable) return;

            try
            {
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SubscribeToSymbolAsync), ex);
            }
        }

        /// <summary>
        /// Unsubscribes from all currently tracked symbols.
        /// </summary>
        private async Task UnsubscribeAllSymbolsAsync()
        {
            try
            {
                foreach (var symbol in _subscribedSymbols.ToList())
                {
                    await _liveTickService.UnsubscribeSymbolAsync(symbol);
                }
                _subscribedSymbols.Clear();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UnsubscribeAllSymbolsAsync), ex);
            }
        }

        /// <summary>
        /// Handles the reception of a new tick for a symbol.
        /// </summary>
        private void HandleLiveTick(TickData tick)
        {
            if (string.IsNullOrEmpty(tick.SymbolName))
                return;

            try
            {
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleLiveTick), ex);
            }
        }

        /// <summary>
        /// Handles initial data loading upon live socket connection.
        /// </summary>
        private void HandleLiveConnected()
        {
            try
            {
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleLiveConnected), ex);
            }
        }

        /// <summary>
        /// Restores symbol subscriptions after a live socket reconnection.
        /// </summary>
        private async void HandleLiveReconnected()
        {
            try
            {
                await SubscribeToSymbolAsync();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HandleLiveReconnected), ex);
            }
        }

        /// <summary>
        /// Updates the price attributes of a grid row based on recent tick data.
        /// </summary>
        private void UpdateRowPrice(PositionGridRow row, TickData tick)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateRowPrice), ex);
            }
        }

        /// <summary>
        /// Refreshes the summary footer containing total profit and loss information.
        /// </summary>
        private void UpdateFooterPnl()
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateFooterPnl), ex);
            }
        }

        /// <summary>
        /// Formats and calculates the overall summary text for the footer row.
        /// </summary>
        private string CalculatePositionFooterSummary(double finalTotalPnl)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CalculatePositionFooterSummary), ex);
                return string.Empty;
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Custom comparer class used to sort position grid items based on chosen criteria.
        /// </summary>
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

        #endregion
    }
}