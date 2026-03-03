using ClientDesktop.Core.Models;
using ClientDesktop.View.Disclaimer;
using ClientDesktop.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientDesktop.Main.Login
{
    /// <summary>
    /// Interaction logic for the Login Page user control.
    /// </summary>
    public partial class LoginPage : UserControl
    {
        #region Fields

        private const int Threshold = 3;
        private bool _isPasswordVisible = false;
        private bool _isInternalChange = false;
        private bool _isUpdating = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LoginPage class.
        /// </summary>
        public LoginPage()
        {
            InitializeComponent();
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Synchronizes the password from the ViewModel and sets initial focus when the control loads.
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginPageViewModel vm)
            {
                if (!string.IsNullOrEmpty(vm.Password) && txtPassword.Password != vm.Password)
                {
                    txtPassword.Password = vm.Password;
                }

                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(LoginPageViewModel.Password))
                    {
                        if (txtPassword.Password != vm.Password)
                        {
                            txtPassword.Password = vm.Password;
                        }
                    }
                };
            }

            cmbServerName.Focus();
        }

        /// <summary>
        /// Updates the ViewModel when the password box text changes.
        /// </summary>
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginPageViewModel vm)
            {
                if (vm.Password != txtPassword.Password)
                {
                    vm.Password = txtPassword.Password;
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of the password characters between masked and unmasked states.
        /// </summary>
        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                pathEyeClosed.Visibility = Visibility.Collapsed;
                pathEyeOpen.Visibility = Visibility.Visible;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                pathEyeClosed.Visibility = Visibility.Visible;
                pathEyeOpen.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handles the text changed event to filter the server list and manage auto-selection based on user input.
        /// </summary>
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
                    _isInternalChange = true;

                    vm.FilteredServers.Clear();

                    if (results.Count > 0)
                    {
                        foreach (var item in results) vm.FilteredServers.Add(item);

                        var firstMatch = results[0];

                        if (firstMatch.companyName.StartsWith(txt, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbServerName.SelectedItem = firstMatch;
                            textBox.Text = firstMatch.companyName;

                            int userTypedLength = txt.Length;
                            int fullLength = firstMatch.companyName.Length;

                            if (fullLength >= userTypedLength)
                                textBox.Select(userTypedLength, fullLength - userTypedLength);

                            cmbServerName.IsDropDownOpen = true;
                        }
                        else
                        {
                            cmbServerName.SelectedItem = null;
                            cmbServerName.Text = txt;
                            textBox.Text = txt;
                            textBox.Select(txt.Length, 0);

                            cmbServerName.IsDropDownOpen = true;
                        }
                    }
                    else
                    {
                        cmbServerName.IsDropDownOpen = false;
                        cmbServerName.SelectedItem = null;

                        cmbServerName.Text = txt;
                        textBox.Text = txt;
                        textBox.Select(txt.Length, 0);
                    }

                    _isInternalChange = false;
                }
                else
                {
                    if (results.Count > 0) cmbServerName.IsDropDownOpen = true;
                }

                if (results.Count == 0 && cmbServerName.SelectedItem != null)
                {
                    _isInternalChange = true;
                    cmbServerName.SelectedItem = null;
                    cmbServerName.Text = txt;
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

        /// <summary>
        /// Prevents the server dropdown from opening if the input text length is below the required threshold.
        /// </summary>
        private void CmbServerName_DropDownOpened(object sender, EventArgs e)
        {
            var txt = cmbServerName.Text.Trim();
            if (txt.Length < Threshold)
            {
                cmbServerName.IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// Handles the selection change event to update the text box with the newly selected server name.
        /// </summary>
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
                }

                cmbServerName.IsDropDownOpen = false;

                _isInternalChange = false;
            }
        }

        /// <summary>
        /// Handles keyboard events within the server name combo box, specifically clearing the selection on Escape key.
        /// </summary>
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

        #region Private Methods

        /// <summary>
        /// Filters the server list based on the provided input string.
        /// </summary>
        private List<ServerList> Filter(string input)
        {
            var vm = this.DataContext as LoginPageViewModel;
            if (vm == null || vm.AllServers == null) return new List<ServerList>();

            if (string.IsNullOrWhiteSpace(input)) return new List<ServerList>();

            input = input.Trim();

            if (input.Length < Threshold) return new List<ServerList>();

            return vm.AllServers
                .Where(s => (s.companyName ?? string.Empty)
                    .IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(20)
                .ToList();
        }

        #endregion
    }
}