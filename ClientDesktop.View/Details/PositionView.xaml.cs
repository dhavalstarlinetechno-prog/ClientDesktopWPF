using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class PositionView : UserControl
    {
        public PositionView()
        {
            InitializeComponent();
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            if (this.DataContext is PositionViewModel vm)
            {
                ListSortDirection direction = (e.Column.SortDirection != ListSortDirection.Ascending)
                                            ? ListSortDirection.Ascending
                                            : ListSortDirection.Descending;

                vm.SortData(e.Column.SortMemberPath, direction);

                e.Column.SortDirection = direction;
            }
        }
    }
}