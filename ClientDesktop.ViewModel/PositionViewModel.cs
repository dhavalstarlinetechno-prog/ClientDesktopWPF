using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
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
        private ObservableCollection<PositionGridRow> _gridRows;
        private readonly IDialogService _dialogService;
        private readonly HashSet<string> _subscribedSymbols = new HashSet<string>();
        private Guid _currentLoadId = Guid.Empty;

        public ICommand DoubleClickCommand { get; }
        public ObservableCollection<PositionGridRow> GridRows
        {
            get { return _gridRows; }
            set { _gridRows = value; OnPropertyChanged(); }
        }

        public PositionViewModel(SessionService sessionService, PositionService positionService, IDialogService dialogService, LiveTickService liveTickService)
        {
            _sessionService = sessionService;
            _positionService = positionService;
            _dialogService = dialogService;
            _liveTickService = liveTickService;
            GridRows = new ObservableCollection<PositionGridRow>();

            DoubleClickCommand = new RelayCommand(row => PositionGridDoubleClick((PositionGridRow?)row));

            RegisterMessenger();
            _liveTickService.OnTickReceived += HandleLiveTick;
            _liveTickService.OnReconnected += HandleLiveReconnected;
            _liveTickService.OnConnected += HandleLiveConnected;
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

                var positionsList = posResult.Positions ?? new List<Position>();
                var ordersList = ordResult.Orders ?? new List<OrderModel>();

                // --- A. ADD POSITIONS ---
                foreach (var pos in positionsList)
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
                decimal totalPnl = positionsList.Sum(p => p.Pnl ?? 0);
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
                foreach (var ord in ordersList)
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

        // Ye tere teeno core logics sambhalega
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
                row.PriceColor = "DodgerBlue"; // Ya "Blue" jo tujhe theek lage
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

                decimal calculatedPnl = (decimal)(priceDifference * row.Volume.Value);
                row.Pnl = Math.Round(calculatedPnl, 2);

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

            double balance = 50000.00;
            double credit = 1000.00;
            double equity = balance + credit + (double)totalPnl;
            double margin = 2000.00;
            double freeMargin = equity - margin;

            string footerText = $"Balance: {balance:N2}   Eq: {equity:N2}   Credit: {credit:N2}   Margin: {margin:N2}   Free: {freeMargin:N2}";
            footerRow.SymbolName = footerText;
        }

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