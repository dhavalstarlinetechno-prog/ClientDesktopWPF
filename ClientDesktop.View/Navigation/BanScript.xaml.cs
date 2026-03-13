using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Navigation
{
    /// <summary>
    /// Interaction logic for BanScript.xaml
    /// </summary>
    public partial class BanScript : UserControl
    {
        public BanScript()
        {
            InitializeComponent();

            DateTime currentDate = DateTime.Today;
            string bandate = $"({currentDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)})";
            LblBanscripiptdate.Text = bandate;

            DgvBanScript.ColumnHeaderHeight = 25;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = AppServiceLocator.GetService<BanScriptViewModel>();
            }
            Loaded += BanScript_Loaded;
        }
        private async void BanScript_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BanScriptViewModel vm)
            {
                await vm.LoadBanScriptData();

                if (vm.GridRows == null || vm.GridRows.Count == 0)
                {
                    LblNoData.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
