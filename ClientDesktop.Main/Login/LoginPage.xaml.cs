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
        }

        // Fixes error: 'LoginPage' does not contain a definition for 'LoginButton_Click'
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string username = cmbLogin.Text;
            string server = cmbServerName.Text;

            // Add your login logic here
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter valid credentials.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Attempting to login to {server}...");
        }

        // Fixes error: 'LoginPage' does not contain a definition for 'CancelButton_Click'
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Fixes error: 'LoginPage' does not contain a definition for 'EyePictureBox_MouseDown'
        private void EyePictureBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Switch to Text mode
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;

                // Change icon to 'open' (make sure eye_open.png is in your project)
                eyePictureBox.Source = new BitmapImage(new Uri("/Assets/Images/eye_open.png"));
            }
            else
            {
                // Switch back to Password mode
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;

                // Change icon back to 'close'
                eyePictureBox.Source = new BitmapImage(new Uri("/Assets/Images/eye_close.png"));
            }
        }
    }
}