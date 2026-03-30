using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientDesktop.View.Symbol
{
    /// <summary>
    /// Interaction logic for SymbolView.xaml
    /// </summary>
    public partial class SymbolView : UserControl
    {

        private readonly SymbolViewModel _viewModel;
        private readonly SessionService _sessionService;
        public static string Mainresponse = string.Empty;
        public static string subJson = string.Empty;
        public static int routeId = 0;
        public static string symbolId = string.Empty;
        private string symbolExpiryclose = string.Empty;
        private string symbolExpiry = string.Empty;
        private TreeViewItem _lastSelectedTreeViewItem;
        private bool _isUpdatingTreeProgrammatically = false;

        public SymbolView()
        {
            InitializeComponent();
            ApplyGridBorders(Tablespecification);
            ApplyGridBorders(SecondGrid);
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();
                _viewModel = AppServiceLocator.GetService<SymbolViewModel>();

                this.DataContext = _viewModel;
            }
        }
        private void ApplyGridBorders(Grid grid)
        {
            int rowCount = grid.RowDefinitions.Count;
            int colCount = grid.ColumnDefinitions.Count;

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    double left = j == 1 ? 0 : 0.5;
                    double top = 0.5;
                    double right = j == 0 ? 0 : 0.5;
                    double bottom = 0.5;

                    Border b = new Border
                    {
                        BorderBrush = System.Windows.Media.Brushes.Gray,
                        BorderThickness = new Thickness(left, top, right, bottom),
                        SnapsToDevicePixels = true
                    };

                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, j);
                    grid.Children.Add(b);
                }
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable)
            {
                Window.GetWindow(this)?.Close();
                return;
            }
            _viewModel.IsBusy = true;
            var getTask = _viewModel.LoadSymbolsAsync();
            var getSubTreeTask = _viewModel.LoadSubSymbolsAsync();
            Mainresponse = await getTask;
            subJson = await getSubTreeTask;
            var root = Newtonsoft.Json.JsonConvert.DeserializeObject<Symbolmodel>(Mainresponse);
            var subRoot = Newtonsoft.Json.JsonConvert.DeserializeObject<SubSymbolRoot>(subJson);
            PopulateFolderTree(root?.Data?.Where(f => f.ParentId == 1).ToList(), null);
            PopulateSymbolTree(subRoot?.Data, root?.Data);
            _viewModel.IsBusy = false;
        }

        private void PopulateFolderTree(List<Folder> folders, TreeViewItem parentNode)
        {
            if (folders == null || folders.Count == 0) return;

            var uniqueFolders = folders.GroupBy(f => f.FolderName.Trim(), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderBy(f => f.FolderName, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var folder in uniqueFolders)
            {
                TreeViewItem folderNode = new TreeViewItem
                {
                    Header = $"{folder.FolderName} ({folder.SymbolCount})",
                    Tag = folder,
                    Style = (Style)FindResource("FolderTreeItemStyle")
                };
                if (parentNode == null) SymbolTreeview.Items.Add(folderNode);
                else parentNode.Items.Add(folderNode);
                var childFolders = GetChildFolders(folder.RouteId);
                if (childFolders != null && childFolders.Count > 0) PopulateFolderTree(childFolders, folderNode);
            }
        }

        private void PopulateSymbolTree(List<SubSymbolModel> symbols, List<Folder> folders)
        {
            if (symbols == null || symbols.Count == 0 || folders == null || folders.Count == 0) return;
            SymbolTreeview.Items.Clear();
            var folderNodes = new Dictionary<int, TreeViewItem>();
            foreach (var folder in folders)
            {
                TreeViewItem folderItem = new TreeViewItem
                {
                    Header = $"{folder.FolderName} ({folder.SymbolCount})",
                    Tag = folder,
                    Style = (Style)FindResource("FolderTreeItemStyle")
                };
                folderNodes[folder.RouteId] = folderItem;
            }

            foreach (var folder in folders)
            {
                if (folder.ParentId == 0 || !folderNodes.ContainsKey(folder.ParentId)) SymbolTreeview.Items.Add(folderNodes[folder.RouteId]);
                else folderNodes[folder.ParentId].Items.Add(folderNodes[folder.RouteId]);
            }

            foreach (var symbol in symbols)
            {
                if (string.IsNullOrWhiteSpace(symbol.SymbolRoutePath)) continue;
                string[] parts = symbol.SymbolRoutePath.Split('/');
                TreeViewItem targetFolderItem = null;
                ItemCollection currentCollection = SymbolTreeview.Items;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "*") break;
                    bool found = false;
                    foreach (TreeViewItem item in currentCollection)
                    {
                        if (item.Tag is Folder folder && item.Header.ToString().StartsWith(parts[i]))
                        {
                            targetFolderItem = item;
                            currentCollection = item.Items;
                            found = true;
                            break;
                        }
                    }
                    if (!found) { targetFolderItem = null; break; }
                }

                if (targetFolderItem != null)
                {
                    TreeViewItem symbolItem = new TreeViewItem
                    {
                        Header = $"{symbol.SymbolName} ({symbol.SymbolCode})",
                        Tag = new Folder { RouteId = symbol.SymbolId, FolderName = symbol.SymbolName, ParentId = symbol.SymbolRouteId, SymbolCount = 1 },
                        Style = (Style)FindResource("FolderTreeItemStyle")
                    };

                    targetFolderItem.Items.Add(symbolItem);
                }
            }

            SortTreeViewItems(SymbolTreeview.Items);
            CollapseAll(SymbolTreeview.Items);
        }

        private List<Folder> GetChildFolders(int parentId) => new List<Folder>();

        private void SortTreeViewItems(ItemCollection items)
        {
            var sorted = items.Cast<TreeViewItem>().OrderBy(i =>
            {
                string header = i.Header.ToString();
                int idx = header.IndexOf('(');
                if (idx > 0) header = header.Substring(0, idx).Trim();
                return header;
            }, StringComparer.OrdinalIgnoreCase).ToList();
            items.Clear();
            foreach (var item in sorted)
                items.Add(item);
            foreach (TreeViewItem item in items)
                SortTreeViewItems(item.Items);
        }

        private void CollapseAll(ItemCollection items)
        {
            foreach (TreeViewItem item in items)
            {
                item.IsExpanded = false;
                CollapseAll(item.Items);
            }
        }

        private void ApplyTreeViewHighlight(TreeViewItem item)
        {
            if (item == null) return;

            // Pela badhi items clear karo
            ClearAllHighlights(SymbolTreeview.Items);

            // Have fakt aa item ne custom color aapo
            item.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db"));
            item.Foreground = Brushes.White;

            // Focus set karvo pan default selection blue na aave e dhyan rakhvu
            _lastSelectedTreeViewItem = item;
        }

        private void ClearAllHighlights(ItemCollection items)
        {
            foreach (TreeViewItem item in items)
            {
                item.IsSelected = false; // IsSelected false karo jethi default blue na dikhay
                item.ClearValue(TreeViewItem.BackgroundProperty);
                item.ClearValue(TreeViewItem.ForegroundProperty);
                if (item.Items.Count > 0) ClearAllHighlights(item.Items);
            }
        }

        private async void SymbolTreeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Programmatic update vakhte data reload thava thi rokva mate ni condition
            if (_isUpdatingTreeProgrammatically) return;

            if (SymbolTreeview.SelectedItem is TreeViewItem tvi)
            {
                ApplyTreeViewHighlight(tvi);
                if (tvi.Tag is Folder folder)
                {
                    routeId = folder.RouteId;
                    await _viewModel.Loadsymbolsbyrouteforclient(routeId);
                    ShowDataForNode(_viewModel.Loadsymbolsbyroute);
                }
            }
        }

        private async void SymbolTreeview_Expanded(object sender, RoutedEventArgs e)
        {
            // Programmatic update vakhte data reload thava thi rokva mate ni condition
            if (_isUpdatingTreeProgrammatically) return;

            if (e.OriginalSource is TreeViewItem selectedNode)
            {
                var folder = selectedNode.Tag as Folder ?? selectedNode.DataContext as Folder;
                if (folder != null)
                {
                    routeId = folder.RouteId;
                    await _viewModel.Loadsymbolsbyrouteforclient(routeId);
                    ShowDataForNode(_viewModel.Loadsymbolsbyroute);
                }
            }
        }

        private void ShowDataForNode(ObservableCollection<SubSymbolModel> dataList)
        {
            try
            {
                if (dataList == null || dataList.Count == 0) { DgvSymbols.ItemsSource = null; return; }
                var displayData = dataList.Select(s => new SymbolDisplayModel
                {
                    Symbol = s.SymbolName,
                    Expiry = s.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(s.SymbolExpiry.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                    ExpiryClose = s.SymbolExpiryClose.HasValue ? CommonHelper.ConvertUtcToIst(s.SymbolExpiryClose.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                    ParentId = s.SymbolId.ToString(),
                    SymbolStatus = s.SymbolStatus.ToString(),
                    RouteType = s.RouteType
                }).ToList();
                DgvSymbols.AutoGenerateColumns = false;
                DgvSymbols.ItemsSource = displayData;
            }
            catch { }
        }

        private async void DgvSymbols_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null || grid.SelectedItem == null) return;
            var selectedRow = grid.SelectedItem as SymbolDisplayModel;
            if (selectedRow == null) return;

            try
            {
                // Set flag true kariye, jethi node ni properties change thay tyare side-events block thay jaay
                _isUpdatingTreeProgrammatically = true;

                string selectedSymbolName = selectedRow.Symbol.Trim();
                string symbolIdStr = selectedRow.ParentId.Trim();
                if (string.IsNullOrEmpty(selectedSymbolName) || string.IsNullOrEmpty(symbolIdStr)) return;
                await _viewModel.LoadDolorSignTree(symbolIdStr);
                if (_viewModel.Loaddolorsymbols != null)
                {
                    var symbolDataList = _viewModel.Loaddolorsymbols.Where(x => !string.IsNullOrEmpty(x.SymbolRoutePath) && x.SymbolRoutePath.Contains(selectedSymbolName) && x.SymbolRoutePath.Contains("*")).ToList();
                    if (symbolDataList.Any())
                    {
                        var validPaths = symbolDataList.Select(x => x.SymbolRoutePath).Distinct().ToList();
                        if (validPaths.Any()) GetSymbolsDetails(string.Join(",", validPaths), symbolIdStr);
                        TreeViewItem foundNode = FindTreeViewItemByRouteId(SymbolTreeview.Items, symbolIdStr);
                        if (foundNode != null)
                        {
                            ExpandParentNodes(foundNode);
                            foundNode.IsExpanded = true;
                            // Selection reset karvu jethi custom highlight lage
                            ApplyTreeViewHighlight(foundNode);
                            foundNode.BringIntoView();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Kam puru thaya pachi fari flag false kari do
                _isUpdatingTreeProgrammatically = false;
            }
        }

        private void GetSymbolsDetails(string selectedSymbolName, string symbolId)
        {
            if (_viewModel.Loaddolorsymbols == null || _viewModel.Loaddolorsymbols.Count == 0)
                return;

            var selectedRoutes = selectedSymbolName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            var filteredSymbols = _viewModel.Loaddolorsymbols.Where(s => !string.IsNullOrEmpty(s.SymbolRoutePath) && selectedRoutes.Any(route => s.SymbolRoutePath.Equals(route, StringComparison.OrdinalIgnoreCase))).Select(s => new SymbolDisplayModel
            {
                Symbol = s.SymbolName,
                Expiry = s.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(s.SymbolExpiry.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                ExpiryClose = s.SymbolExpiryClose.HasValue ? CommonHelper.ConvertUtcToIst(s.SymbolExpiryClose.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                ParentId = s.SymbolId.ToString(),
                SymbolStatus = s.SymbolStatus.ToString(),
                RouteType = s.RouteType
            }).ToList();

            if (filteredSymbols.Count == 0) return;
            DgvSymbols.AutoGenerateColumns = false;
            DgvSymbols.ItemsSource = filteredSymbols;
        }
        private TreeViewItem FindTreeViewItemByRouteId(ItemCollection items, string symbolId)
        {
            foreach (TreeViewItem item in items)
            {
                if (item.Tag is Folder folder && folder.RouteId.ToString() == symbolId) return item;
                if (item.Items.Count > 0)
                {
                    TreeViewItem child = FindTreeViewItemByRouteId(item.Items, symbolId);
                    if (child != null) return child;
                }
            }
            return null;
        }

        private void ExpandParentNodes(TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent != null)
            {
                if (parent is TreeViewItem parentItem) parentItem.IsExpanded = true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        private void Txtsearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Txtsearch.Text) || Txtsearch.Text == "Search Symbol")
            {
                if (_lastSelectedTreeViewItem != null)
                {
                    ApplyTreeViewHighlight(_lastSelectedTreeViewItem);
                    ShowDataForNode(_viewModel.Loadsymbolsbyroute);
                }
                else DgvSymbols.ItemsSource = null;
                return;
            }

            string searchText = Txtsearch.Text.Trim().ToLowerInvariant();
            var subRoot = Newtonsoft.Json.JsonConvert.DeserializeObject<SubSymbolRoot>(subJson);
            if (subRoot?.Data == null) return;

            var filteredList = subRoot.Data.Where(x => !string.IsNullOrEmpty(x.SymbolName) && x.SymbolName.ToLowerInvariant().Contains(searchText)).Select(x => new SymbolDisplayModel
            {
                Symbol = x.SymbolName,
                Expiry = x.SymbolExpiry.HasValue ? CommonHelper.ConvertUtcToIst(x.SymbolExpiry.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                ExpiryClose = x.SymbolExpiryClose.HasValue ? CommonHelper.ConvertUtcToIst(x.SymbolExpiryClose.Value).ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture) : null,
                ParentId = x.SymbolId.ToString(),
                SymbolStatus = x.SymbolStatus.ToString(),
                RouteType = x.RouteType.ToString()
            }).ToList();

            DgvSymbols.ItemsSource = filteredList;
        }

        private async void DgvSymbols_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null) return;

            var row = dep as DataGridRow;

            var dataItem = row.DataContext as SymbolDisplayModel;
            if (dataItem == null) return;

            string selectedSymbolName = dataItem.Symbol;
            if (string.IsNullOrWhiteSpace(selectedSymbolName)) return;

            int parentId = 0;
            if (!string.IsNullOrEmpty(dataItem.ParentId))
            {
                int.TryParse(dataItem.ParentId, out parentId);
            }

            if (_viewModel.Loaddolorsymbols != null)
            {
                var symbolDataList = _viewModel.Loaddolorsymbols
                    .Where(x => !string.IsNullOrEmpty(x.SymbolRoutePath) &&
                                x.SymbolRoutePath.Contains(selectedSymbolName) &&
                                x.SymbolRoutePath.Contains("*") &&
                                x.RouteType == "Folder")
                    .ToList();

                if (symbolDataList.Any())
                {
                    return;
                }
                else
                {
                    await _viewModel.LoadSymbolDetailsAsync(parentId);
                    if (_viewModel.SymbolData != null && _viewModel.SymbolData.Count > 0)
                    {
                        var latestData = _viewModel.SymbolData[0];

                        if (latestData != null)
                        {
                            string masterName = latestData.MasterSymbolName ?? "";
                            LableSymbol.Content = $"{latestData.SymbolName} , {masterName}";
                            Lbldigitvalue.Content = SafeToString(latestData.SymbolDigits);
                            Lblcontractsizevalue.Content = SafeToString(latestData.SymbolContractsize);
                            Lblstopsizevalue.Content = SafeToString(latestData.SymbolLimitstoplevel);
                            Lblticksizevalue.Content = SafeToString(latestData.SymbolTicksize);
                            LblTradeValue.Content = FormatValue(SafeToString(latestData.SymbolTrade));

                            Lbladvancevalue.Content = (latestData.SymbolAdvancelimit ? "Yes" : "No");
                            Lblgtcvalue.Content = FormatValue(SafeToString(latestData.SecurityGtc));
                            Lblordervalue.Content = SafeToString(latestData.SymbolOrder.ToString().Replace(",", ", "));

                            var expiryCloseToken = latestData.SymbolExpiryclose?.ToString();
                            if (!string.IsNullOrEmpty(expiryCloseToken) && DateTime.TryParse(expiryCloseToken, out DateTime utcTimeClose))
                            {
                                DateTime istTime = CommonHelper.ConvertUtcToIst(utcTimeClose);
                                symbolExpiryclose = istTime.ToString("dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                            }

                            var expiryToken = latestData.SymbolExpiry?.ToString();
                            if (!string.IsNullOrEmpty(expiryToken) && DateTime.TryParse(expiryToken, out DateTime utcTimeExp))
                            {
                                DateTime istTime = CommonHelper.ConvertUtcToIst(utcTimeExp);
                                symbolExpiry = istTime.ToString("dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                            }

                            Lblclosevalue.Content = symbolExpiryclose;
                            Lblpositionvalue.Content = symbolExpiry;
                            Lblminimumvalue.Content = latestData.SymbolMinimumvalue.ToString();
                            Lblstepvalue.Content = latestData.SymbolStepvalue.ToString();
                            Lbloneclickvalue.Content = latestData.SymbolOneclickvalue.ToString();
                            Lbltotalvalue.Content = latestData.SymbolTotalvalue.ToString();

                            var sessions = latestData.Sessions;
                            if (sessions != null)
                            {
                                ShowDayRows();
                                foreach (var session in sessions)
                                {
                                    string day = session.SessionDay;
                                    string quoteTimeStr = session.Quotetime;
                                    if (!string.IsNullOrWhiteSpace(quoteTimeStr))
                                    {
                                        var formatted = SetDateAndTime(quoteTimeStr);
                                        symbolExpiryclose = string.Join(", ", formatted);
                                    }

                                    string tradeTimeStr = session.Tradetime;
                                    if (!string.IsNullOrWhiteSpace(tradeTimeStr))
                                    {
                                        var formattedRanges = SetDateAndTime(tradeTimeStr);
                                        symbolExpiry = string.Join(", ", formattedRanges);
                                    }

                                    switch (day)
                                    {
                                        case "Monday":
                                            Lblmondayquotedate.Content = symbolExpiryclose;
                                            Lblmondaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Tuesday":
                                            Lbltuesdayquotedate.Content = symbolExpiryclose;
                                            Lbltuesdaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Wednesday":
                                            Lblwednesdayquotedate.Content = symbolExpiryclose;
                                            Lblwednesdaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Thursday":
                                            Lblthursdayquotedate.Content = symbolExpiryclose;
                                            Lblthursdaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Friday":
                                            Lblfridayquotedate.Content = symbolExpiryclose;
                                            Lblfridaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Saturday":
                                            Lblsaturdayquotedate.Content = symbolExpiryclose;
                                            Lblsaturdaytradedate.Content = symbolExpiry;
                                            break;
                                        case "Sunday":
                                            Lblsundayquotedate.Content = symbolExpiryclose;
                                            Lblsundaytradedate.Content = symbolExpiry;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<string> SetDateAndTime(string TimeStr)
        {
            string[] ranges = TimeStr.Split(',');
            List<string> formatted = new List<string>();

            foreach (var range in ranges)
            {
                string[] parts = range.Split('~');
                if (parts.Length == 2)
                {
                    if (DateTime.TryParseExact(parts[0], "HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTime) &&
                        DateTime.TryParseExact(parts[1], "HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endTime))
                    {
                        DateTime istStart = CommonHelper.ConvertUtcToIst(startTime);
                        DateTime istEnd = CommonHelper.ConvertUtcToIst(endTime);
                        formatted.Add($"{istStart:HH:mm} - {istEnd:HH:mm}");
                    }
                }
            }

            return formatted;
        }

        private void ShowDayRows()
        {
            for (int i = 16; i <= 22; i++)
            {
                if (Tablespecification.RowDefinitions.Count > i)
                {
                    Tablespecification.RowDefinitions[i].Height = new GridLength(30);

                    foreach (UIElement child in Tablespecification.Children)
                    {
                        if (Grid.GetRow(child) == i)
                        {
                            child.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        private string SafeToString(object o)
        {
            return o?.ToString() ?? "";
        }

        private string FormatValue(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        private async void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = DgvSymbols.SelectedItem as SymbolDisplayModel;

                if (selectedItem == null)
                {
                    return;
                }

                if (int.TryParse(selectedItem.ParentId, out int parentIdInt))
                {
                    var marketWatchVM = AppServiceLocator.GetService<MarketWatchViewModel>();
                    if (marketWatchVM != null)
                    {
                        await marketWatchVM.UpdateSymbolVisibility(true, parentIdInt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error showing symbol: " + ex.Message);
            }

        }

        private async void Btnhide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = DgvSymbols.SelectedItem as SymbolDisplayModel;

                if (selectedItem == null)
                {
                    return;
                }

                if (int.TryParse(selectedItem.ParentId, out int parentIdInt))
                {
                    var marketWatchVM = AppServiceLocator.GetService<MarketWatchViewModel>();
                    if (marketWatchVM != null)
                    {
                        await marketWatchVM.UpdateSymbolVisibility(false, parentIdInt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error showing symbol: " + ex.Message);
            }
        }
    }
}
