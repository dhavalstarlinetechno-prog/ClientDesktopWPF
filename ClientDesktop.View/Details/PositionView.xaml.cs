using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace ClientDesktop.View.Details
{
    public partial class PositionView : UserControl
    {
        public PositionView()
        {
            try
            {
                InitializeComponent();

                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    this.DataContext = AppServiceLocator.GetService<PositionViewModel>();
                }

                this.Unloaded += PositionView_Unloaded;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PositionView), ex);
            }
        }

        private void PositionView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (this.DataContext is PositionViewModel vm)
                {
                    vm.Cleanup();
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PositionView_Unloaded), ex);
            }
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DataGrid_Sorting), ex);
            }
        }
    }
}