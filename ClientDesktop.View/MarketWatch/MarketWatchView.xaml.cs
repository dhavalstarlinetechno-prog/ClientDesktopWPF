using ClientDesktop.Core.Models;
using ClientDesktop.ViewModel;
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
                var targetItem = targetRow.DataContext as MarketItem;
                var viewModel = this.DataContext as MarketWatchViewModel;
                if (viewModel != null && targetItem != null && _draggedItem is MarketItem sourceItem)
                {
                    if (ReferenceEquals(sourceItem, targetItem)) return;
                    if (viewModel.MarketView.SortDescriptions.Count > 0)
                    {
                        viewModel.MarketView.SortDescriptions.Clear();
                    }
                    int oldIndex = viewModel.MarketItems.IndexOf(sourceItem);
                    int newIndex = viewModel.MarketItems.IndexOf(targetItem);
                    if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
                    {
                        viewModel.MarketItems.Move(oldIndex, newIndex);
                    }
                }
            }
            _draggedItem = null;
            _isDragging = false;
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
