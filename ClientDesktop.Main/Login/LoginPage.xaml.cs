using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.Main.Login
{
    public partial class LoginPage : UserControl
    {
        private bool _isPasswordVisible = false;
        private bool _isInternalChange = false;
        private bool _isUpdating = false;
        private const int Threshold = 3;

        public LoginPage()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[UI] UserControl_Loaded called");

            if (DataContext == null)
            {
                this.DataContext = new LoginPageViewModel();
            }
            else
            {
                Console.WriteLine($"[UI] DataContext is already set: {DataContext.GetType().Name}");
            }

            cmbServerName.Focus();
        }

        //private void LoginButton_Click(object sender, RoutedEventArgs e)
        //{
        //    string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
        //    string username = cmbLogin.Text;
        //    string server = cmbServerName.Text;

        //    // Basic Validation
        //    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        //    {
        //        MessageBox.Show("Please enter valid credentials.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    MessageBox.Show($"Connecting to {server} as {username}...", "Login Processing", MessageBoxButton.OK, MessageBoxImage.Information);
        //}

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void Disclaimer_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("This application is for authorized use only.\nUnauthorized access is prohibited.", "Disclaimer", MessageBoxButton.OK, MessageBoxImage.Information);
            // You can also open a web link here:
            // System.Diagnostics.Process.Start("https://www.your-company.com/disclaimer");
        }

        #region Server List Management
        private List<ServerList> Filter(string input)
        {
            var vm = this.DataContext as LoginPageViewModel;
            // Safety checks
            if (vm == null || vm.AllServers == null) return new List<ServerList>();

            // WinForms Logic Implementation
            if (string.IsNullOrWhiteSpace(input)) return new List<ServerList>();

            input = input.Trim(); // Trim input for search logic

            if (input.Length < Threshold) return new List<ServerList>();

            return vm.AllServers
                .Where(s => (s.companyName ?? string.Empty)
                    .IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(20)
                .ToList();
        }

        private void CmbServerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalChange) return;
            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                var vm = this.DataContext as LoginPageViewModel;
                if (vm == null) return;

                var textBox = cmbServerName.Template.FindName("PART_EditableTextBox", cmbServerName) as TextBox;

                if (textBox == null) return;

                string txt = textBox.Text;
                int caret = textBox.CaretIndex;

                if (!string.IsNullOrEmpty(txt))
                {
                    textBox.Background = Brushes.White;
                    cmbServerName.Background = Brushes.White;
                }
                else
                {
                    var errorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC7CE"));
                    textBox.Background = errorBrush;
                    cmbServerName.Background = errorBrush;
                }

                var results = Filter(txt);

                bool needsUpdate = true;
                if (vm.FilteredServers.Count == results.Count)
                {
                    bool match = true;
                    for (int i = 0; i < results.Count; i++)
                    {
                        if (vm.FilteredServers[i] != results[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) needsUpdate = false;
                }

                if (needsUpdate)
                {
                    vm.FilteredServers.Clear();

                    if (results.Count > 0)
                    {
                        foreach (var item in results) vm.FilteredServers.Add(item);
                        cmbServerName.IsDropDownOpen = true;

                        var firstMatch = results[0];
                        if (firstMatch.companyName.StartsWith(txt, StringComparison.OrdinalIgnoreCase))
                        {
                            _isInternalChange = true;

                            cmbServerName.SelectedItem = firstMatch;

                            textBox.Text = firstMatch.companyName;

                            int userTypedLength = txt.Length;
                            int fullLength = firstMatch.companyName.Length;

                            if (fullLength >= userTypedLength)
                                textBox.Select(userTypedLength, fullLength - userTypedLength);

                            cmbServerName.IsDropDownOpen = true;
                            _isInternalChange = false;
                        }
                    }
                    else
                    {
                        cmbServerName.IsDropDownOpen = false;
                    }
                }
                else
                {
                    if (results.Count > 0) cmbServerName.IsDropDownOpen = true;
                }

                if (results.Count == 0 && cmbServerName.SelectedItem != null)
                {
                    _isInternalChange = true;
                    cmbServerName.SelectedItem = null;

                    if (textBox.Text != txt) textBox.Text = txt;

                    textBox.Select(txt.Length, 0);
                    _isInternalChange = false;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void CmbServerName_DropDownOpened(object sender, EventArgs e)
        {
            var txt = cmbServerName.Text.Trim();
            if (txt.Length < Threshold)
            {
                cmbServerName.IsDropDownOpen = false;
            }
        }

        private void CmbServerName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || _isInternalChange) return;

            if (cmbServerName.SelectedItem is ServerList sel)
            {
                _isInternalChange = true;

                cmbServerName.Text = sel.companyName ?? string.Empty;
                var textBox = cmbServerName.Template.FindName("PART_EditableTextBox", cmbServerName) as TextBox;

                if (textBox != null)
                {
                    textBox.Select(cmbServerName.Text.Length, 0);

                    textBox.Background = Brushes.White;
                }
                cmbServerName.Background = Brushes.White;

                cmbServerName.IsDropDownOpen = false;

                _isInternalChange = false;
            }
        }

        private void CmbServerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var vm = this.DataContext as LoginPageViewModel;
                if (vm != null)
                {
                    vm.FilteredServers.Clear();
                    cmbServerName.Text = string.Empty;
                    cmbServerName.IsDropDownOpen = false;
                    cmbServerName.SelectedIndex = -1;
                }
                e.Handled = true;
            }
        }
        #endregion
    }
}