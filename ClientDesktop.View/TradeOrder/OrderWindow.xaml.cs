using ClientDesktop.Core.Enums;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.View.TradeOrder
{
    public partial class OrderWindow : UserControl
    {
        public OrderWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                if (this.DataContext is ViewModel.TradeViewModel vm)
                {
                    vm.CloseAction = () =>
                    {
                        Window.GetWindow(this)?.Close();
                    };
                }
            };
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ClientDesktop.ViewModel.TradeViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            Regex regex = new Regex("^[0-9.]+$");

            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true; 
                return;
            }

            e.Handled = !regex.IsMatch(e.Text); 
        }
    }
}