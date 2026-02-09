using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Async tasks ke liye
using System.Windows;       // UI Dispatcher ke liye
using System.Windows.Data;

namespace ClientDesktop.ViewModel
{
    public class PositionViewModel : INotifyPropertyChanged
    {
        private readonly PositionService _positionService;
        public ObservableCollection<PositionGridRow> GridRows { get; set; }

        public PositionViewModel()
        {
            _positionService = new PositionService();
            GridRows = new ObservableCollection<PositionGridRow>();

            SessionManager.OnLoginSuccess += HandleLogin;
            //SessionManager.OnLogout += HandleLogout;


        }

        private void HandleLogin()
        {
            LoadDataAsync();
        }

        public async void LoadDataAsync()
        {
            try
            {
                var positionTask = _positionService.GetPositionsAsync();
                var orderTask = _positionService.GetOrdersAsync();

                await Task.WhenAll(positionTask, orderTask);

                var posResult = await positionTask;
                var ordResult = await orderTask;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    GridRows.Clear();

                    var positionsList = posResult.Positions ?? new List<Position>();
                    var ordersList = ordResult.Orders ?? new List<OrderModel>();

                    // --- A. ADD POSITIONS ---
                    foreach (var pos in positionsList)
                    {
                        // Mapping: Bid/Ask -> Sell/Buy
                        string displaySide = pos.Side;
                        if (string.Equals(pos.Side, "Bid", StringComparison.OrdinalIgnoreCase)) displaySide = "Sell";
                        else if (string.Equals(pos.Side, "Ask", StringComparison.OrdinalIgnoreCase)) displaySide = "Buy";

                        GridRows.Add(new PositionGridRow
                        {
                            Id = pos.Id,
                            SymbolName = pos.SymbolName,
                            Time = pos.LastInAt,
                            Side = displaySide,
                            OrderType = "Market",
                            Volume = pos.TotalVolume,
                            AveragePrice = pos.AveragePrice,
                            CurrentPrice = pos.CurrentPrice,
                            Pnl = pos.Pnl, // P/L Real Data
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

                    GridRows.Add(new PositionGridRow
                    {
                        SymbolName = footerText,
                        Type = RowType.Footer,
                        Pnl = totalPnl, // Total Floating P/L
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

                        GridRows.Add(new PositionGridRow
                        {
                            Id = ord.OrderId,
                            SymbolName = ord.SymbolName,
                            Time = ord.UpdatedAt,
                            Side = displaySide,
                            OrderType = ord.OrderType,
                            Volume = ord.Volume,
                            AveragePrice = ord.Price, // Order Price
                            CurrentPrice = ord.CurrentPrice,
                            Pnl = null, // Orders me P/L nahi hota
                            Type = RowType.Order
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data Load Error: " + ex.Message);
            }
        }

        // Custom Sorting (Positions Only)
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}