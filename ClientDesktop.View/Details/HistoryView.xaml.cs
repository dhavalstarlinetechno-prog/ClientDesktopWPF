using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ClientDesktop.Models;

namespace ClientDesktop.View.Details
{
    public partial class HistoryView : UserControl
    {
        public ObservableCollection<HistoryModel> HistoryItems { get; set; }

        private string _currentMode = "Deals";

        public HistoryView()
        {
            InitializeComponent();
            HistoryItems = new ObservableCollection<HistoryModel>();

            GridDealsOrders.ItemsSource = HistoryItems;
            GridPosition.ItemsSource = HistoryItems;

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        }

        // --- CONTEXT MENU HANDLER ---
        private void ChangeView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item == null) return; // Safety check

            string mode = item.Tag.ToString();
            _currentMode = mode;

            MenuDeals.IsChecked = false;
            MenuOrders.IsChecked = false;
            MenuPosition.IsChecked = false;

            item.IsChecked = true;

            HistoryItems.Clear();
            NoDataLabel.Visibility = Visibility.Visible;
            GridDealsOrders.Visibility = Visibility.Collapsed;
            GridPosition.Visibility = Visibility.Collapsed;

            HeaderDealsOrders.Visibility = Visibility.Collapsed;
            HeaderPosition.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case "Deals":
                    lblTitleDealsOrders.Text = "Deals";
                    HeaderDealsOrders.Visibility = Visibility.Visible;
                    GridDealsOrders.Visibility = Visibility.Visible;
                    break;

                case "Orders":
                    lblTitleDealsOrders.Text = "Orders";
                    HeaderDealsOrders.Visibility = Visibility.Visible;
                    GridDealsOrders.Visibility = Visibility.Visible;
                    break;

                case "Position":
                    HeaderPosition.Visibility = Visibility.Visible;
                    GridPosition.Visibility = Visibility.Visible;
                    break;
            }

            UpdateUIState();
        }

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryItems.Clear();

            var dummyResponse = GetDummyData();
            if (dummyResponse.IsSuccess)
            {
                foreach (var item in dummyResponse.Data)
                {
                    HistoryItems.Add(item);
                }
            }
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool hasData = HistoryItems.Count > 0;

            if (_currentMode == "Position")
            {
                GridPosition.Visibility = Visibility.Visible;
                GridDealsOrders.Visibility = Visibility.Collapsed;
            }
            else // Deals or Orders
            {
                GridDealsOrders.Visibility = Visibility.Visible;
                GridPosition.Visibility = Visibility.Collapsed;
            }

            if (hasData)
            {
                NoDataLabel.Visibility = Visibility.Collapsed;

                if (_currentMode == "Position")
                {
                    if (FilterPosSymbol != null) FilterPosSymbol.IsEnabled = true;
                }
                else
                {
                    if (FilterSymbol != null) FilterSymbol.IsEnabled = true;
                    if (FilterExecution != null) FilterExecution.IsEnabled = true;
                    if (FilterType != null) FilterType.IsEnabled = true;
                    if (FilterEntry != null) FilterEntry.IsEnabled = true;
                }
            }
            else
            {
                NoDataLabel.Visibility = Visibility.Visible;

                if (FilterSymbol != null) FilterSymbol.IsEnabled = false;
                if (FilterExecution != null) FilterExecution.IsEnabled = false;
                if (FilterType != null) FilterType.IsEnabled = false;
                if (FilterEntry != null) FilterEntry.IsEnabled = false;
                if (FilterPosSymbol != null) FilterPosSymbol.IsEnabled = false;
            }
        }

        private void PeriodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker == null || EndDatePicker == null) return;

            if (PeriodCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                string period = selectedItem.Content.ToString();

                EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);

                switch (period)
                {
                    case "Today":
                        StartDatePicker.SelectedDate = DateTime.Today;
                        break;
                    case "Last 3 Days":
                        StartDatePicker.SelectedDate = DateTime.Today.AddDays(-3);
                        break;
                    case "Last Week":
                        StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
                        break;
                    case "Last Month":
                        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
                        break;
                    case "Last 3 Months":
                        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-3);
                        break;
                    case "Last 6 Months":
                        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-6);
                        break;
                    case "All History":
                        StartDatePicker.SelectedDate = new DateTime(2000, 1, 1);
                        break;
                }
            }
        }

        private HistoryResponse GetDummyData()
        {
            var list = new List<HistoryModel>();

            switch (_currentMode)
            {
                case "Deals":
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddMinutes(-15),
                        ClientDealId = 8801,
                        PositionId = "5001",
                        SymbolName = "XAUUSD",
                        OrderType = "Market",
                        Side = "Buy",
                        DealType = "In",
                        Volume = 0.50m,
                        Price = 2035.50m,
                        Fee = -5.00m,
                        Pnl = 0.00m, 
                        Comment = "Gold Scalp Entry"
                    });
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddMinutes(-5),
                        ClientDealId = 8802,
                        PositionId = "5001",
                        SymbolName = "XAUUSD",
                        OrderType = "Market",
                        Side = "Sell",
                        DealType = "Out",
                        Volume = 0.50m,
                        Price = 2038.50m,
                        Fee = 0.00m,
                        Pnl = 150.00m, 
                        Comment = "TP Hit"
                    });
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddHours(-2),
                        ClientDealId = 8799,
                        PositionId = "4990",
                        SymbolName = "EURUSD",
                        OrderType = "Limit",
                        Side = "Sell",
                        DealType = "Out",
                        Volume = 1.0m,
                        Price = 1.0850m,
                        Fee = -2.50m,
                        Pnl = -25.00m, // Loss
                        Comment = "SL Hit"
                    });
                    break;

                case "Orders":
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now,
                        ClientDealId = 9001,
                        PositionId = "-", 
                        SymbolName = "GBPUSD",
                        OrderType = "Buy Limit",
                        Side = "Buy",
                        DealType = "Order",
                        Volume = 0.10m,
                        Price = 1.2600m,
                        Fee = 0.00m,
                        Pnl = 0.00m,
                        Comment = "Waiting for dip"
                    });
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddMinutes(-30),
                        ClientDealId = 9002,
                        PositionId = "-",
                        SymbolName = "US30",
                        OrderType = "Sell Stop",
                        Side = "Sell",
                        DealType = "Order",
                        Volume = 0.05m,
                        Price = 38500.00m,
                        Fee = 0.00m,
                        Pnl = 0.00m,
                        Comment = "Breakout Strategy"
                    });
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddHours(-5),
                        ClientDealId = 9003,
                        PositionId = "-",
                        SymbolName = "BTCUSD",
                        OrderType = "Buy Market",
                        Side = "Buy",
                        DealType = "Cancelled",
                        Volume = 1.00m,
                        Price = 45000.00m,
                        Fee = 0.00m,
                        Pnl = 0.00m,
                        Comment = "Cancelled by user"
                    });
                    break;

                case "Position":
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddDays(-1),
                        ClientDealId = 5501,
                        PositionId = "5501",
                        SymbolName = "DJ30",
                        OrderType = "Market",
                        Side = "Buy",
                        DealType = "In",
                        Volume = 2.0m,
                        Price = 34500.00m,
                        Fee = -10.00m,
                        Pnl = 350.50m, // Running Profit
                        Comment = "Swing Trade"
                    });
                    list.Add(new HistoryModel
                    {
                        CreatedOn = DateTime.Now.AddHours(-4),
                        ClientDealId = 5502,
                        PositionId = "5502",
                        SymbolName = "USDJPY",
                        OrderType = "Limit",
                        Side = "Sell",
                        DealType = "In",
                        Volume = 0.20m,
                        Price = 150.50m,
                        Fee = -1.50m,
                        Pnl = -12.40m, // Running Loss
                        Comment = "Scalping"
                    });
                    break;
            }

            return new HistoryResponse
            {
                IsSuccess = true,
                Data = list
            };
        }
    }
}