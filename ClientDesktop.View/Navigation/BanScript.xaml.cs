using ClientDesktop.Core.Models;
using ClientDesktop.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            DataContext = new BanScriptViewModel();           
        }        
    }
}
