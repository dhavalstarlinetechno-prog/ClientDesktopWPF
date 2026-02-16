using ClientDesktop.Infrastructure.Services;
using System.Windows;

namespace ClientDesktop.View.Disclaimer
{
    /// <summary>
    /// Interaction logic for DisclaimerView.xaml
    /// </summary>
    public partial class DisclaimerView : Window
    {
        public DisclaimerView()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Acknowledge_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
