using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Models;
using ClientDesktop.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class HistoryView : UserControl
    {
        public ObservableCollection<HistoryModel> AllHistoryItems { get; set; }
        public ObservableCollection<HistoryModel> HistoryItems { get; set; }

        public ObservableCollection<PositionHistoryModel> AllPositionItems { get; set; }
        public ObservableCollection<PositionHistoryModel> PositionHistoryItems { get; set; }

        private HistoryViewModel _historyViewModel;
        private PDFBuilder _pdfBuilder = new PDFBuilder();

        HistoryType currentType = HistoryType.Deals;

        public HistoryView()
        {
            InitializeComponent();

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _historyViewModel = AppServiceLocator.GetService<HistoryViewModel>();

                _historyViewModel.OnHistoryDataLoaded = () =>
                {
                    Dispatcher.Invoke(() => FillDataInList());
                };
            }

            this.DataContext = _historyViewModel;

            AllHistoryItems = new ObservableCollection<HistoryModel>();
            HistoryItems = new ObservableCollection<HistoryModel>();

            AllPositionItems = new ObservableCollection<PositionHistoryModel>();
            PositionHistoryItems = new ObservableCollection<PositionHistoryModel>();

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void FillDataInList()
        {
            var history = _historyViewModel.HistoryItems;
            var position = _historyViewModel.PositionHistoryItems;

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
            ApplyGridFilters();
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
                    "BuyLimit", "SellLimit", "BuyStop", "SellStop"
                };
                symbolNameFilter = new HashSet<string>
                {
                    "Credit", "Balance"
                };

                HistoryItems = new ObservableCollection<HistoryModel>(AllHistoryItems.Where(h =>
                {
                    if (orderTypeFilter.Contains(h.OrderType))
                        return false;

                    if (symbolNameFilter.Contains(h.SymbolName))
                        return false;

                    var istTime = CommonHelper.ConvertUtcToIst(h.CreatedOn);
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

                    var istTime = CommonHelper.ConvertUtcToIst(h.CreatedOn);
                    return istTime >= start && istTime <= end;
                }).OrderBy(s => s.CreatedOn));

                GridDealsOrders.ItemsSource = HistoryItems;
            }
            else
            {
                PositionHistoryItems = new ObservableCollection<PositionHistoryModel>(AllPositionItems.Where(h => h.LastOutAt == null || CommonHelper.ConvertUtcToIst(h.UpdatedAt) >= start.Value.Date &&
                  CommonHelper.ConvertUtcToIst(h.UpdatedAt) <= end.Date.AddDays(1).AddTicks(-1)).OrderBy(s => s.UpdatedAt));
                GridPosition.ItemsSource = PositionHistoryItems;
            }
        }

        private void FillComboFilters()
        {
            if (currentType == HistoryType.Position)
            {
                if (PositionHistoryItems == null || !PositionHistoryItems.Any())
                {
                    SetComboItems(FilterPosSymbol, new List<string> { "All" });
                    return;
                }

                var symbols = PositionHistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.SymbolName))
                    .Select(x => x.SymbolName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                symbols.Insert(0, "All");
                SetComboItems(FilterPosSymbol, symbols);
            }
            else
            {
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
                    .Select(x => x.SymbolName).Distinct().OrderBy(x => x).ToList();

                var executions = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.OrderType))
                    .Select(x => x.OrderType).Distinct().OrderBy(x => x).ToList();

                var types = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.Side))
                    .Select(x => x.Side).Distinct().OrderBy(x => x).ToList();

                var entries = HistoryItems
                    .Where(x => !string.IsNullOrEmpty(x.DealType))
                    .Select(x => x.DealType).Distinct().OrderBy(x => x).ToList();

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

            GridDealsOrders.ItemsSource = null;
            GridPosition.ItemsSource = null;
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

            ApplyGridFilters();
            FillComboFilters();
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
                    case "Today": StartDatePicker.SelectedDate = DateTime.Today; break;
                    case "Last 3 Days": StartDatePicker.SelectedDate = DateTime.Today.AddDays(-3); break;
                    case "Last Week": StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7); break;
                    case "Last Month": StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1); break;
                    case "Last 3 Months": StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-3); break;
                    case "Last 6 Months": StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-6); break;
                    case "All History": StartDatePicker.SelectedDate = new DateTime(2000, 1, 1); break;
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
                if (HistoryItems == null || !HistoryItems.Any()) return;

                IEnumerable<HistoryModel> filterHistoryList = HistoryItems;

                if (selectedSymbol != "All") filterHistoryList = filterHistoryList.Where(x => x.SymbolName == selectedSymbol);
                if (selectedExec != "All") filterHistoryList = filterHistoryList.Where(x => x.OrderType == selectedExec);
                if (selectedType != "All") filterHistoryList = filterHistoryList.Where(x => x.Side == selectedType);
                if (selectedEntry != "All") filterHistoryList = filterHistoryList.Where(x => x.DealType == selectedEntry);

                var finalData = filterHistoryList.OrderBy(s => s.CreatedOn).ToList();

                if (finalData.Any())
                {
                    decimal totalProfit = finalData.Where(x => x.OrderType != "Bill").Sum(x => x.Pnl);
                    decimal totalComm = finalData.Where(x => x.OrderType != "Bill").Sum(x => x.UplineCommission);

                    var start = StartDatePicker.SelectedDate.Value;
                    var end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

                    var billEntry = AllHistoryItems
                        .Where(x => x.OrderType == "Bill" &&
                                    CommonHelper.ConvertUtcToIst(x.CreatedOn) >= start &&
                                    CommonHelper.ConvertUtcToIst(x.CreatedOn) <= end)
                        .FirstOrDefault();

                    decimal credit = billEntry?.UplineCommission ?? 0;
                    decimal balance = billEntry?.Pnl ?? 0;

                    var footerRow = new HistoryModel
                    {
                        RefId = "FOOTER",
                        CreatedOn = DateTime.MaxValue,
                        SymbolName = "",
                        OrderType = "",
                        Side = "",
                        Volume = 0,
                        Price = 0,
                        UplineCommission = totalComm,
                        Pnl = totalProfit,
                        Comment = $"Profit: {totalProfit:N2}  Credit: {credit:N2}  Balance: {balance:F3}INR"
                    };

                    finalData.Add(footerRow);
                }

                GridDealsOrders.ItemsSource = finalData;
                NoDataLabel.Visibility = finalData.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                if (PositionHistoryItems == null || !PositionHistoryItems.Any()) return;

                IEnumerable<PositionHistoryModel> filterPositionList = PositionHistoryItems;
                if (selectedPosSymbol != "All")
                    filterPositionList = filterPositionList.Where(x => x.SymbolName == selectedPosSymbol);

                var finalData = filterPositionList.OrderBy(s => s.UpdatedAt).ToList();

                if (finalData.Any())
                {
                    double totalProfit = finalData.Sum(x => x.Pnl);
                    double totalComm = finalData.Sum(x => (double)x.AverageOutPrice);

                    var footerRow = new PositionHistoryModel
                    {
                        RefId = "FOOTER",
                        UpdatedAt = DateTime.MaxValue,
                        SymbolName = "",
                        Side = "",
                        TotalVolume = 0,
                        AveragePrice = 0,
                        CurrentPrice = 0,
                        AverageOutPrice = (double)totalComm,
                        Pnl = totalProfit,
                        Comment = $"Profit: {totalProfit:N2}"
                    };

                    finalData.Add(footerRow);
                }

                GridPosition.ItemsSource = finalData;
                NoDataLabel.Visibility = PositionHistoryItems.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // Track sort direction manually because reassigning ItemsSource resets column.SortDirection
        private readonly Dictionary<string, ListSortDirection> _dealsSortState = new Dictionary<string, ListSortDirection>();
        private readonly Dictionary<string, ListSortDirection> _positionSortState = new Dictionary<string, ListSortDirection>();

        // ─────────────────────────────────────────────────────────────────────
        // SORTING — intercept DataGrid sort, do it manually so the footer row
        // is always excluded from sorting and stays pinned at the bottom.
        // ─────────────────────────────────────────────────────────────────────

        private void GridDealsOrders_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var source = GridDealsOrders.ItemsSource as List<HistoryModel>;
            if (source == null || source.Count == 0) return;

            var footer = source.FirstOrDefault(x => x.RefId == "FOOTER");
            var dataRows = source.Where(x => x.RefId != "FOOTER").ToList();

            string propName = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(propName)) propName = (e.Column.Header as string) ?? string.Empty;

            // Read previous direction from our own dictionary (not from column, which gets reset on ItemsSource change)
            if (_dealsSortState == null) return;
            _dealsSortState.TryGetValue(propName, out var currentDir);
            var newDir = (currentDir == ListSortDirection.Ascending)
                         ? ListSortDirection.Descending
                         : ListSortDirection.Ascending;

            // Save new direction
            _dealsSortState[propName] = newDir;

            // Update sort arrows: clear all, set current column
            foreach (var col in GridDealsOrders.Columns)
                col.SortDirection = null;
            e.Column.SortDirection = newDir;

            var prop = typeof(HistoryModel).GetProperty(propName);
            IEnumerable<HistoryModel> sorted = prop != null
                ? (newDir == ListSortDirection.Ascending
                    ? dataRows.OrderBy(r => prop.GetValue(r))
                    : dataRows.OrderByDescending(r => prop.GetValue(r)))
                : dataRows;

            var result = sorted.ToList();
            if (footer != null) result.Add(footer);

            GridDealsOrders.ItemsSource = result;
        }

        private void GridPosition_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var source = GridPosition.ItemsSource as List<PositionHistoryModel>;
            if (source == null || source.Count == 0) return;

            var footer = source.FirstOrDefault(x => x.RefId == "FOOTER");
            var dataRows = source.Where(x => x.RefId != "FOOTER").ToList();

            string propName = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(propName)) propName = (e.Column.Header as string) ?? string.Empty;

            if (_positionSortState == null) return;
            _positionSortState.TryGetValue(propName, out var currentDir);
            var newDir = (currentDir == ListSortDirection.Ascending)
                         ? ListSortDirection.Descending
                         : ListSortDirection.Ascending;

            _positionSortState[propName] = newDir;

            foreach (var col in GridPosition.Columns)
                col.SortDirection = null;
            e.Column.SortDirection = newDir;

            var prop = typeof(PositionHistoryModel).GetProperty(propName);
            IEnumerable<PositionHistoryModel> sorted = prop != null
                ? (newDir == ListSortDirection.Ascending
                    ? dataRows.OrderBy(r => prop.GetValue(r))
                    : dataRows.OrderByDescending(r => prop.GetValue(r)))
                : dataRows;

            var result = sorted.ToList();
            if (footer != null) result.Add(footer);

            GridPosition.ItemsSource = result;
        }

        // ─────────────────────────────────────────────────────────────────────

        private string GetComboValue(ComboBox filterSymbol)
        {
            if (filterSymbol != null && filterSymbol.SelectedItem != null)
                return filterSymbol.SelectedItem.ToString();
            return "All";
        }

        private void CopyId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string fullId = btn.Tag.ToString();
                Clipboard.SetText(fullId);
            }
        }

        private void ExcelExport_Click(object sender, RoutedEventArgs e)
        {
            if (currentType == HistoryType.Position)
            {
                var data = GridPosition.ItemsSource as List<PositionHistoryModel>;
                _historyViewModel.ExportPositionToExcel(data);
            }
            else
            {
                var data = GridDealsOrders.ItemsSource as List<HistoryModel>;
                _historyViewModel.ExportToExcel(data, currentType);
            }
        }

        private void PdfExport_Click(object sender, RoutedEventArgs e)
        {
            if (currentType == HistoryType.Position)
            {
                var data = GridPosition.ItemsSource as List<PositionHistoryModel>;
                _historyViewModel.ExportPositionToPdf(data);
            }
            else
            {
                var data = GridDealsOrders.ItemsSource as List<HistoryModel>;
                _historyViewModel.ExportToPdf(data, currentType);
            }
        }
    }
}
