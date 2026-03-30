using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    /// <summary>
    /// Interaction logic for the History view user control.
    /// </summary>
    public partial class HistoryView : UserControl
    {
        #region Fields

        private readonly Dictionary<string, ListSortDirection> _dealsSortState = new Dictionary<string, ListSortDirection>();
        private readonly Dictionary<string, ListSortDirection> _positionSortState = new Dictionary<string, ListSortDirection>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HistoryView class.
        /// </summary>
        public HistoryView()
        {
            InitializeComponent();

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = AppServiceLocator.GetService<HistoryViewModel>();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the click event for copying an ID to the clipboard.
        /// </summary>
        private void CopyId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string fullId = btn.Tag.ToString();
                Clipboard.SetText(fullId);
            }
        }

        /// <summary>
        /// Handles custom sorting logic for the Deals and Orders data grid.
        /// </summary>
        private void GridDealsOrders_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var vm = DataContext as HistoryViewModel;
            if (vm == null) return;

            string propName = e.Column.SortMemberPath ?? (e.Column.Header as string) ?? string.Empty;

            _dealsSortState.TryGetValue(propName, out var currentDir);
            var newDir = currentDir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            _dealsSortState[propName] = newDir;

            foreach (var col in GridDealsOrders.Columns) col.SortDirection = null;
            e.Column.SortDirection = newDir;

            vm.SortDeals(propName, newDir);
        }

        /// <summary>
        /// Handles custom sorting logic for the Position data grid.
        /// </summary>
        private void GridPosition_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var vm = DataContext as HistoryViewModel;
            if (vm == null) return;

            string propName = e.Column.SortMemberPath ?? (e.Column.Header as string) ?? string.Empty;

            _positionSortState.TryGetValue(propName, out var currentDir);
            var newDir = currentDir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            _positionSortState[propName] = newDir;

            foreach (var col in GridPosition.Columns) col.SortDirection = null;
            e.Column.SortDirection = newDir;

            vm.SortPositions(propName, newDir);
        }

        #endregion
    }
}