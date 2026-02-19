using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using System.Globalization;
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
        }        
    }
}
