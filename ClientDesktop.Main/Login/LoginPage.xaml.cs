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

            // Set focus to password if login is pre-filled (optional UX improvement)
            if (!string.IsNullOrEmpty(cmbLogin.Text))
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

            // Simulate Login Success for now
            MessageBox.Show($"Connecting to {server} as {username}...", "Login Processing", MessageBoxButton.OK, MessageBoxImage.Information);

            // TODO: Add your actual authentication logic here
            // OnSuccess: this.DialogResult = true; this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EyePictureBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Switch to Text mode
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;

                // Attempt to load open eye image, handle error if image missing
                try
                {
                    eyePictureBox.Source = new BitmapImage(new Uri("/Assets/Images/eye_open.png"));
                }
                catch { /* Suppress image error so app doesn't crash */ }
            }
            else
            {
                // Switch back to Password mode
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;

                // Attempt to load closed eye image
                try
                {
                    eyePictureBox.Source = new BitmapImage(new Uri("/Assets/Images/eye_close.png"));
                }
                catch { /* Suppress image error */ }
            }
        }
    }
}