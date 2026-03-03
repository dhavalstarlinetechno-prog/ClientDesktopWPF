using ClientDesktop.Core.Models;
using ClientDesktop.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace ClientDesktop.View.Navigation
{
    /// <summary>
    /// Interaction logic for NavigationView.xaml
    /// </summary>
    public partial class NavigationView : UserControl
    {
        public NavigationView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles single click node selection and command execution cleanly without swallowing events.
        /// </summary>
        private void TreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                if (FindVisualParent<System.Windows.Controls.Primitives.ToggleButton>(source) != null)
                    return;

                var clickedItem = FindVisualParent<TreeViewItem>(source);
                if (clickedItem == null || !clickedItem.IsSelected)
                    return;

                if (this.DataContext is NavigationViewModel vm)
                {
                    if (clickedItem.Tag != null)
                    {
                        vm.OpenMenuCommand.Execute(clickedItem.Tag.ToString());
                    }
                    else if (clickedItem.DataContext is NavigationNode userNode)
                    {
                        vm.UserClickCommand.Execute(userNode);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to find a parent of a specific type in the visual tree.
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }
    }
}
