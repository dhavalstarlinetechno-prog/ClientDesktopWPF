using ClientDesktop.Core.Models;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.View.MarketWatch
{
    /// <summary>
    /// Interaction logic for MarketWatchView.xaml
    /// </summary>
    public partial class MarketWatchView : UserControl
    {
        private Point _startPoint;
        private object _draggedItem;
        private bool _isDragging = false;

        public MarketWatchView()
        {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _draggedItem = null;
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while (dep != null && !(dep is DataGridCell) && !(dep is DataGridColumnHeadersPresenter))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell cell)
            {
                if (cell.Column.DisplayIndex == 0)
                {
                    var item = cell.DataContext as MarketWatchSymbols;

                    if (item != null && string.IsNullOrWhiteSpace(item.SymbolName))
                        return;

                    _startPoint = e.GetPosition(null);
                    _draggedItem = cell.DataContext;
                    _isDragging = true;
                }
            }
        }

        private void DataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || e.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;
                _draggedItem = null;
                return;
            }
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (_draggedItem != null)
                {
                    DataGrid grid = sender as DataGrid;
                    DragDrop.DoDragDrop(grid, _draggedItem, DragDropEffects.Move);
                    _isDragging = false;
                }
            }
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (_draggedItem == null) return;
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while (dep != null && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridRow targetRow)
            {
                var targetItem = targetRow.DataContext as MarketWatchSymbols;
                var viewModel = this.DataContext as MarketWatchViewModel;

                if (viewModel != null && targetItem != null && _draggedItem is MarketWatchSymbols sourceItem)
                {
                    if (ReferenceEquals(sourceItem, targetItem)) return;

                    if (viewModel.MarketView is System.Windows.Data.ListCollectionView view)
                    {
                        if (view.SortDescriptions.Count > 0) view.SortDescriptions.Clear();
                        if (view.CustomSort != null) view.CustomSort = null;
                    }

                    if (string.IsNullOrWhiteSpace(sourceItem.SymbolName)) return;

                    int oldIndex = viewModel.MarketWatchSymbolsCollection.IndexOf(sourceItem);
                    int newIndex = viewModel.MarketWatchSymbolsCollection.IndexOf(targetItem);

                    if (string.IsNullOrWhiteSpace(targetItem.SymbolName))
                    {
                        newIndex = viewModel.MarketWatchSymbolsCollection.Count - 2;
                        if (newIndex < 0) newIndex = 0;
                    }

                    if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
                    {
                        viewModel.MarketWatchSymbolsCollection.Move(oldIndex, newIndex);
                    }
                }
            }
            _draggedItem = null;
            _isDragging = false;
        }

        #region Search Box Keyboard Handlers

        private void AddBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MarketWatchViewModel;
            if (vm != null && !string.IsNullOrWhiteSpace(vm.NewSymbolSearchText) && vm.SuggestedSymbols.Count > 0)
            {
                vm.IsSuggestionOpen = true;
            }
        }

        private void AddBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var txt = sender as TextBox;
            var vm = this.DataContext as MarketWatchViewModel;
            if (txt == null || vm == null) return;

            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                if (vm.IsSuggestionOpen && vm.SuggestedSymbols.Count > 0)
                {
                    var firstSymbol = vm.SuggestedSymbols[0];
                    if (vm.AddSymbolCommand.CanExecute(firstSymbol))
                    {
                        vm.AddSymbolCommand.Execute(firstSymbol);
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down && vm.IsSuggestionOpen && vm.SuggestedSymbols.Count > 0)
            {
                var grid = VisualTreeHelper.GetParent(txt) as Grid;
                var popup = grid?.Children.OfType<Popup>().FirstOrDefault();
                var border = popup?.Child as Border;
                var listBox = border?.Child as ListBox;

                if (listBox != null)
                {
                    listBox.Focus();
                    if (listBox.Items.Count > 0)
                    {
                        listBox.SelectedIndex = 0;
                        var item = listBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                        item?.Focus();
                    }
                    e.Handled = true;
                }
            }
        }

        private void SuggestionListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                var listBox = sender as ListBox;
                var vm = this.DataContext as MarketWatchViewModel;

                if (listBox != null && listBox.SelectedItem != null && vm != null)
                {
                    var symbol = listBox.SelectedItem as MarketWatchSymbols;
                    if (vm.AddSymbolCommand.CanExecute(symbol))
                    {
                        vm.AddSymbolCommand.Execute(symbol);
                    }
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Custom DataGrid Sorting (To keep empty row at bottom)

        private void MarketGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var viewModel = this.DataContext as MarketWatchViewModel;
            if (viewModel == null) return;

            e.Handled = true;

            var direction = (e.Column.SortDirection != ListSortDirection.Ascending)
                            ? ListSortDirection.Ascending
                            : ListSortDirection.Descending;

            e.Column.SortDirection = direction;

            if (viewModel.MarketView is System.Windows.Data.ListCollectionView view)
            {
                view.CustomSort = new EmptyRowStickyComparer(e.Column.SortMemberPath, direction);
            }
        }

        #endregion
    }

    public class EmptyRowStickyComparer : System.Collections.IComparer
    {
        private string _propertyName;
        private ListSortDirection _direction;

        public EmptyRowStickyComparer(string propertyName, ListSortDirection direction)
        {
            _propertyName = propertyName;
            _direction = direction;
        }

        public int Compare(object x, object y)
        {
            var itemX = x as MarketWatchSymbols;
            var itemY = y as MarketWatchSymbols;

            bool isXEmpty = string.IsNullOrWhiteSpace(itemX?.SymbolName);
            bool isYEmpty = string.IsNullOrWhiteSpace(itemY?.SymbolName);

            if (isXEmpty && isYEmpty) return 0;
            if (isXEmpty) return 1;  
            if (isYEmpty) return -1; 

            var propX = itemX?.GetType().GetProperty(_propertyName)?.GetValue(itemX, null);
            var propY = itemY?.GetType().GetProperty(_propertyName)?.GetValue(itemY, null);

            int result = 0;
            if (propX is IComparable compX && propY is IComparable compY)
            {
                result = compX.CompareTo(compY);
            }
            else if (propX == null && propY != null) result = -1;
            else if (propX != null && propY == null) result = 1;

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }

    // Proxy helper to allow DataGridColumns to bind to ViewModel properties
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
    }
}
