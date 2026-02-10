using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ClientDesktop.Models; // Ensure ye namespace sahi ho

namespace ClientDesktop.View.Details
{
    public partial class HistoryView : UserControl
    {
        public ObservableCollection<HistoryModel> HistoryItems { get; set; }

        public HistoryView()
        {
            InitializeComponent();
            HistoryItems = new ObservableCollection<HistoryModel>();
            HistoryGrid.ItemsSource = HistoryItems;

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);

            PeriodCombo.SelectedIndex = 0;
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

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryItems.Clear();

            // Dummy Data
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

            if (hasData)
            {
                NoDataLabel.Visibility = Visibility.Collapsed;
                HistoryGrid.Visibility = Visibility.Visible;

                FilterSymbol.IsEnabled = true;
                FilterExecution.IsEnabled = true;
                FilterType.IsEnabled = true;
                FilterEntry.IsEnabled = true;
            }
            else
            {
                NoDataLabel.Visibility = Visibility.Visible;
                HistoryGrid.Visibility = Visibility.Collapsed;

                FilterSymbol.IsEnabled = false;
                FilterExecution.IsEnabled = false;
                FilterType.IsEnabled = false;
                FilterEntry.IsEnabled = false;
            }
        }

        private HistoryResponse GetDummyData()
        {
            return new HistoryResponse
            {
                IsSuccess = true,
                Data = new List<HistoryModel>
                {
                    new HistoryModel { CreatedOn = DateTime.Now, ClientDealId = 1001, SymbolName = "XAUUSD", OrderType = "Market", Side = "Buy", DealType = "In", Volume = 0.5m, Price = 2035.50m, Pnl = 150.00m, Comment = "Test" },
                    new HistoryModel { CreatedOn = DateTime.Now.AddHours(-2), ClientDealId = 1002, SymbolName = "EURUSD", OrderType = "Limit", Side = "Sell", DealType = "Out", Volume = 1.0m, Price = 1.0850m, Pnl = -20.00m, Comment = "SL Hit" }
                }
            };
        }
    }
}