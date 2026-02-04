using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ClientDesktop.Main.Login
{
    public partial class LoginPage : Window
    {
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();

            // Set focus to password if login is pre-filled
            if (cmbLogin != null && !string.IsNullOrEmpty(cmbLogin.Text))
            {
                txtPassword.Focus();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string username = cmbLogin.Text;
            string server = cmbServerName.Text;

            // Basic Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter valid credentials.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Connecting to {server} as {username}...", "Login Processing", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show Text, Hide Dots
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;

                // Toggle Path Visibility (Open Eye Visible)
                pathEyeClosed.Visibility = Visibility.Collapsed;
                pathEyeOpen.Visibility = Visibility.Visible;
            }
            else
            {
                // Show Dots, Hide Text
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;

                // Toggle Path Visibility (Closed Eye Visible)
                pathEyeClosed.Visibility = Visibility.Visible;
                pathEyeOpen.Visibility = Visibility.Collapsed;
            }
        }

        // New Disclaimer Click Event
        private void Disclaimer_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("This application is for authorized use only.\nUnauthorized access is prohibited.", "Disclaimer", MessageBoxButton.OK, MessageBoxImage.Information);
            // You can also open a web link here:
            // System.Diagnostics.Process.Start("https://www.your-company.com/disclaimer");
        }
    }
}