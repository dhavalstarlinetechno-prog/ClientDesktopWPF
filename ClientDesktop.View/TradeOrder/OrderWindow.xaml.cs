using ClientDesktop.Infrastructure.Logger;
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
            try
            {
                InitializeComponent();

                Loaded += OnLoaded;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OrderWindow), ex);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ViewModel.TradeViewModel vm)
                {
                    vm.CloseAction = () =>
                    {
                        try
                        {
                            Window.GetWindow(this)?.Close();
                        }
                        catch (Exception ex)
                        {
                            FileLogger.ApplicationLog(nameof(OnLoaded) + "_CloseAction", ex);
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OnLoaded), ex);
            }
        }

        /// <summary>
        /// Unsubscribes live tick listeners and cleans up ViewModel resources
        /// when the control is removed from the visual tree.
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ViewModel.TradeViewModel vm)
                    vm.Cleanup();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UserControl_Unloaded), ex);
            }
        }

        /// <summary>
        /// Restricts text box input to numeric characters and a single decimal point.
        /// Attach to <c>PreviewTextInput</c> on quantity / price fields.
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(NumberValidationTextBox), ex);
            }
        }
    }
}