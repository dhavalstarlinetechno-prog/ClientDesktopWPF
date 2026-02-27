using ClientDesktop.Infrastructure.Helpers;
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

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = AppServiceLocator.GetService<PositionViewModel>();
            }

            this.Unloaded += PositionView_Unloaded;
        }

        private void PositionView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //if (this.DataContext is PositionViewModel vm)
            //{
            //    vm.Cleanup();
            //}
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