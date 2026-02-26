using ClientDesktop.Core.Enums;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientDesktop.View.TradeOrder
{
    public partial class OrderWindow : UserControl
    {
        public OrderWindow()
        {
            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ClientDesktop.ViewModel.TradeViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}