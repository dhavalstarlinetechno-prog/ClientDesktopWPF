using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ClientDesktop.Helpers
{
    /// <summary>
    /// Provides attached behaviors for the DataGrid control.
    /// </summary>
    public static class DataGridBehaviors
    {
        #region Dependency Properties

        /// <summary>
        /// Dependency property to automatically select the first row when data loads.
        /// </summary>
        public static readonly DependencyProperty AutoSelectFirstRowProperty =
            DependencyProperty.RegisterAttached(
                "AutoSelectFirstRow",
                typeof(bool),
                typeof(DataGridBehaviors),
                new UIPropertyMetadata(false, OnAutoSelectFirstRowChanged));

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the value of the AutoSelectFirstRow attached property.
        /// </summary>
        public static bool GetAutoSelectFirstRow(DependencyObject obj) => (bool)obj.GetValue(AutoSelectFirstRowProperty);

        /// <summary>
        /// Sets the value of the AutoSelectFirstRow attached property.
        /// </summary>
        public static void SetAutoSelectFirstRow(DependencyObject obj, bool value) => obj.SetValue(AutoSelectFirstRowProperty, value);

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the property changed event to attach or detach the LoadingRow event handler.
        /// </summary>
        private static void OnAutoSelectFirstRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid grid)
            {
                if ((bool)e.NewValue)
                {
                    grid.LoadingRow += Grid_LoadingRow;
                }
                else
                {
                    grid.LoadingRow -= Grid_LoadingRow;
                }
            }
        }

        /// <summary>
        /// Automatically selects the first row when it is loaded into the DataGrid.
        /// </summary>
        private static void Grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            if (e.Row.GetIndex() == 0 && grid.SelectedIndex == -1)
            {
                grid.Dispatcher.InvokeAsync(
                    () =>
                    {
                        if (grid.Items.Count > 0 && grid.SelectedIndex == -1)
                        {
                            grid.SelectedIndex = 0;
                        }
                    },
                DispatcherPriority.Loaded);
            }
        }

        #endregion
    }
}