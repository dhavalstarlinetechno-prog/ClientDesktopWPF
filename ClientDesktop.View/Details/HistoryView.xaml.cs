using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Models;
using ClientDesktop.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClientDesktop.View.Details
{
    public partial class HistoryView : UserControl
    {
        public ObservableCollection<HistoryModel> AllHistoryItems { get; set; }
        public ObservableCollection<HistoryModel> HistoryItems { get; set; }

        public ObservableCollection<PositionHistoryModel> AllPositionItems { get; set; }
        public ObservableCollection<PositionHistoryModel> PositionHistoryItems { get; set; }

        private HistoryViewModel _historyViewModel;

        HistoryType currentType = HistoryType.Deals;

        public HistoryView()
        {
            InitializeComponent();
            AllHistoryItems = new ObservableCollection<HistoryModel>();
            HistoryItems = new ObservableCollection<HistoryModel>();

            AllPositionItems = new ObservableCollection<PositionHistoryModel>();
            PositionHistoryItems = new ObservableCollection<PositionHistoryModel>();

            DataContext = this;

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);

            _historyViewModel = new HistoryViewModel();
            _historyViewModel.OnHistoryDataLoaded = () =>
            {
                FillDataInList();
            };
        }

        private void FillDataInList()
        {
            var history = _historyViewModel.GetHistoryData() ?? new List<HistoryModel>();
            var position = _historyViewModel.GetPositionHistoryData() ?? new List<PositionHistoryModel>();

            // 🔒 Fill master lists
            AllHistoryItems.Clear();
            foreach (var item in history)
                AllHistoryItems.Add(item);

            AllPositionItems.Clear();
            foreach (var item in position)
                AllPositionItems.Add(item);
        }

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryItems.Clear();
            PositionHistoryItems.Clear();
            FillDataGrid();
            FillComboFilters();
            UpdateUIState();
        }

        private void FillDataGrid()
        {
            var start = StartDatePicker.SelectedDate;
            var end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            HashSet<string> orderTypeFilter;
            HashSet<string> symbolNameFilter;

            if (currentType == HistoryType.Deals)
            {
                orderTypeFilter = new HashSet<string>
                {
                    "BuyLimit",
                    "SellLimit",
                    "BuyStop",
                    "SellStop"
                };

                symbolNameFilter = new HashSet<string>
                {
                    "Credit",
                    "Balance"
                };

                HistoryItems = new ObservableCollection<HistoryModel>(AllHistoryItems.Where(h =>
                {
                    if (orderTypeFilter.Contains(h.OrderType))
                        return false;

                    if (symbolNameFilter.Contains(h.SymbolName))
                        return false;

                    var istTime = GMTTime.ConvertUtcToIst(h.CreatedOn);
                    return istTime >= start && istTime <= end;
                }).OrderBy(s => s.CreatedOn));
                GridDealsOrders.ItemsSource = HistoryItems;
            }
            else if (currentType == HistoryType.Orders)
            {
                orderTypeFilter = new HashSet<string>();
                symbolNameFilter = new HashSet<string>();

                HistoryItems = new ObservableCollection<HistoryModel>(AllHistoryItems.Where(h =>
                {
                    if (orderTypeFilter.Contains(h.OrderType))
                        return false;

                    if (symbolNameFilter.Contains(h.SymbolName))
                        return false;

                    var istTime = GMTTime.ConvertUtcToIst(h.CreatedOn);
                    return istTime >= start && istTime <= end;
                }).OrderBy(s => s.CreatedOn));

                GridDealsOrders.ItemsSource = HistoryItems;
            }
            else
            {
                PositionHistoryItems = new ObservableCollection<PositionHistoryModel>(AllPositionItems.Where(h => h.LastOutAt == null || GMTTime.ConvertUtcToIst(h.UpdatedAt) >= start.Value.Date &&
                  GMTTime.ConvertUtcToIst(h.UpdatedAt) <= end.Date.AddDays(1).AddTicks(-1)).OrderBy(s => s.UpdatedAt));
                GridPosition.ItemsSource = PositionHistoryItems;
            }
        }

        private void FillComboFilters()
        {
            if (currentType == HistoryType.Position)
            {
                // Check if PositionHistoryItems is null or empty
                if (PositionHistoryItems == null || !PositionHistoryItems.Any())
                {
                    SetComboItems(FilterPosSymbol, new List<string> { "All" });
                    return;
                }

                var symbols = PositionHistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.SymbolName))  // Filter out null/empty symbols
                    .Select(x => x.SymbolName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                symbols.Insert(0, "All");
                SetComboItems(FilterPosSymbol, symbols);
            }
            else
            {
                // Check if HistoryItems is null or empty
                if (HistoryItems == null || !HistoryItems.Any())
                {
                    SetComboItems(FilterSymbol, new List<string> { "All" });
                    SetComboItems(FilterExecution, new List<string> { "All" });
                    SetComboItems(FilterType, new List<string> { "All" });
                    SetComboItems(FilterEntry, new List<string> { "All" });
                    return;
                }

                var symbols = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.SymbolName))
                    .Select(x => x.SymbolName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var executions = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.OrderType))
                    .Select(x => x.OrderType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var types = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.Side))
                    .Select(x => x.Side)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var entries = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.DealType))
                    .Select(x => x.DealType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                symbols.Insert(0, "All");
                executions.Insert(0, "All");
                types.Insert(0, "All");
                entries.Insert(0, "All");

                SetComboItems(FilterSymbol, symbols);
                SetComboItems(FilterExecution, executions);
                SetComboItems(FilterType, types);
                SetComboItems(FilterEntry, entries);
            }
        }

        private void SetComboItems(ComboBox combo, List<string> items)
        {
            combo.ItemsSource = null;
            combo.Items.Clear();

            combo.ItemsSource = items;
            combo.SelectedIndex = 0;
        }

        private void ChangeView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item == null) return;

            var mode = Enum.Parse<HistoryType>(item.Tag.ToString());
            currentType = mode;

            MenuDeals.IsChecked = false;
            MenuOrders.IsChecked = false;
            MenuPosition.IsChecked = false;

            item.IsChecked = true;

            HistoryItems.Clear();
            PositionHistoryItems.Clear();
            NoDataLabel.Visibility = Visibility.Visible;
            GridDealsOrders.Visibility = Visibility.Collapsed;
            GridPosition.Visibility = Visibility.Collapsed;

            HeaderDealsOrders.Visibility = Visibility.Collapsed;
            HeaderPosition.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case HistoryType.Deals:
                    lblTitleDealsOrders.Text = "Deals";
                    HeaderDealsOrders.Visibility = Visibility.Visible;
                    GridDealsOrders.Visibility = Visibility.Visible;
                    break;

                case HistoryType.Orders:
                    lblTitleDealsOrders.Text = "Orders";
                    HeaderDealsOrders.Visibility = Visibility.Visible;
                    GridDealsOrders.Visibility = Visibility.Visible;
                    break;

                case HistoryType.Position:
                    HeaderPosition.Visibility = Visibility.Visible;
                    GridPosition.Visibility = Visibility.Visible;
                    break;
            }

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool hasHistoryData = HistoryItems.Count > 0;
            bool hasPositionData = PositionHistoryItems.Count > 0;

            if (currentType == HistoryType.Position)
            {
                if (hasPositionData)
                {
                    NoDataLabel.Visibility = Visibility.Collapsed;
                    if (FilterPosSymbol != null) FilterPosSymbol.IsEnabled = true;
                }
                else
                {
                    NoDataLabel.Visibility = Visibility.Visible;
                    if (FilterPosSymbol != null) FilterPosSymbol.IsEnabled = false;
                }

                GridPosition.Visibility = Visibility.Visible;
                GridDealsOrders.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (hasHistoryData)
                {
                    NoDataLabel.Visibility = Visibility.Collapsed;
                    if (FilterSymbol != null) FilterSymbol.IsEnabled = true;
                    if (FilterExecution != null) FilterExecution.IsEnabled = true;
                    if (FilterType != null) FilterType.IsEnabled = true;
                    if (FilterEntry != null) FilterEntry.IsEnabled = true;
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

                GridDealsOrders.Visibility = Visibility.Visible;
                GridPosition.Visibility = Visibility.Collapsed;
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

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyGridFilters();
        }

        private void ApplyGridFilters()
        {
            string selectedSymbol = GetComboValue(FilterSymbol);
            string selectedExec = GetComboValue(FilterExecution);
            string selectedType = GetComboValue(FilterType); 
            string selectedEntry = GetComboValue(FilterEntry);
            string selectedPosSymbol = GetComboValue(FilterPosSymbol);

            if (currentType != HistoryType.Position)
            {
                if (HistoryItems == null || !HistoryItems.Any())
                    return;

                IEnumerable<HistoryModel> filterHistoryList = HistoryItems;

                if (selectedSymbol != "All")
                    filterHistoryList = filterHistoryList.Where(x => x.SymbolName == selectedSymbol);

                if (selectedExec != "All")
                    filterHistoryList = filterHistoryList.Where(x => x.OrderType == selectedExec);

                if (selectedType != "All")
                    filterHistoryList = filterHistoryList.Where(x => x.Side == selectedType);

                if (selectedEntry != "All")
                    filterHistoryList = filterHistoryList.Where(x => x.DealType == selectedEntry);

                // Update UI
                var finalCollection = new ObservableCollection<HistoryModel>(filterHistoryList.OrderBy(s => s.CreatedOn));
                GridDealsOrders.ItemsSource = finalCollection;
            }
            else
            {
                if (PositionHistoryItems == null || !PositionHistoryItems.Any())
                    return;

                IEnumerable<PositionHistoryModel> filterPositionList = PositionHistoryItems;
                if (selectedPosSymbol != "All")
                    filterPositionList = filterPositionList.Where(x => x.SymbolName == selectedPosSymbol);

                // Update UI
                var finalCollection = new ObservableCollection<PositionHistoryModel>(filterPositionList.OrderBy(s => s.UpdatedAt));
                GridPosition.ItemsSource = finalCollection;
            }
        }

        private string GetComboValue(ComboBox filterSymbol)
        {
            if (filterSymbol != null && filterSymbol.SelectedItem != null)
            {
                return filterSymbol.SelectedItem.ToString();
            }
            return "All";
        }
    }
}