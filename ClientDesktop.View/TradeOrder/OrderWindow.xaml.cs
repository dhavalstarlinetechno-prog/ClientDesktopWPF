using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientDesktop.View.TradeOrder
{
    /// <summary>
    /// UserControl for placing, modifying, and closing trade orders.
    /// Hosts <see cref="TradeViewModel"/> as its DataContext.
    /// </summary>
    public partial class OrderWindow : UserControl
    {
        /// <summary>Matches valid numeric input: digits and a single decimal point.</summary>
        private static readonly Regex NumericInputRegex = new Regex("^[0-9.]+$", RegexOptions.Compiled);

        public OrderWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel.TradeViewModel vm)
            {
                vm.CloseAction = () => Window.GetWindow(this)?.Close();
            }
        }

        /// <summary>
        /// Unsubscribes live tick listeners and cleans up ViewModel resources
        /// when the control is removed from the visual tree.
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel.TradeViewModel vm)
                vm.Cleanup();
        }

        /// <summary>
        /// Restricts text box input to numeric characters and a single decimal point.
        /// Attach to <c>PreviewTextInput</c> on quantity / price fields.
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // Block a second decimal point
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
                return;
            }

            e.Handled = !NumericInputRegex.IsMatch(e.Text);
        }
    }
}