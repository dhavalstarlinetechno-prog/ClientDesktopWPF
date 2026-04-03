using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.ChangePassword
{
    /// <summary>
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : UserControl
    {
        private bool _isCurrentVisible = false;
        private bool _isNewVisible = false;
        private bool _isConfirmVisible = false;

        public ChangePasswordView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ChangePasswordView), ex);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtCurrent.Focus();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UserControl_Loaded), ex);
            }
        }

        #region Password Synchronization

        private void TxtCurrent_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ChangePasswordViewModel vm)
                {
                    vm.CurrentPassword = txtCurrent.Password;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TxtCurrent_PasswordChanged), ex);
            }
        }

        private void TxtNew_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ChangePasswordViewModel vm)
                {
                    vm.NewPassword = txtNew.Password;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TxtNew_PasswordChanged), ex);
            }
        }

        private void TxtConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ChangePasswordViewModel vm)
                {
                    vm.ConfirmPassword = txtConfirm.Password;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TxtConfirm_PasswordChanged), ex);
            }
        }

        #endregion

        #region Eye Button Toggles

        private void BtnEyeCurrent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isCurrentVisible = !_isCurrentVisible;
                if (_isCurrentVisible)
                {
                    txtCurrentVis.Text = txtCurrent.Password;
                    txtCurrentVis.Visibility = Visibility.Visible;
                    txtCurrent.Visibility = Visibility.Collapsed;
                    pathEyeClosedCurrent.Visibility = Visibility.Collapsed;
                    pathEyeOpenCurrent.Visibility = Visibility.Visible;
                }
                else
                {
                    txtCurrent.Password = txtCurrentVis.Text;
                    txtCurrent.Visibility = Visibility.Visible;
                    txtCurrentVis.Visibility = Visibility.Collapsed;
                    pathEyeClosedCurrent.Visibility = Visibility.Visible;
                    pathEyeOpenCurrent.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BtnEyeCurrent_Click), ex);
            }
        }

        private void BtnEyeNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isNewVisible = !_isNewVisible;
                if (_isNewVisible)
                {
                    txtNewVis.Text = txtNew.Password;
                    txtNewVis.Visibility = Visibility.Visible;
                    txtNew.Visibility = Visibility.Collapsed;
                    pathEyeClosedNew.Visibility = Visibility.Collapsed;
                    pathEyeOpenNew.Visibility = Visibility.Visible;
                }
                else
                {
                    txtNew.Password = txtNewVis.Text;
                    txtNew.Visibility = Visibility.Visible;
                    txtNewVis.Visibility = Visibility.Collapsed;
                    pathEyeClosedNew.Visibility = Visibility.Visible;
                    pathEyeOpenNew.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BtnEyeNew_Click), ex);
            }
        }

        private void BtnEyeConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isConfirmVisible = !_isConfirmVisible;
                if (_isConfirmVisible)
                {
                    txtConfirmVis.Text = txtConfirm.Password;
                    txtConfirmVis.Visibility = Visibility.Visible;
                    txtConfirm.Visibility = Visibility.Collapsed;
                    pathEyeClosedConfirm.Visibility = Visibility.Collapsed;
                    pathEyeOpenConfirm.Visibility = Visibility.Visible;
                }
                else
                {
                    txtConfirm.Password = txtConfirmVis.Text;
                    txtConfirm.Visibility = Visibility.Visible;
                    txtConfirmVis.Visibility = Visibility.Collapsed;
                    pathEyeClosedConfirm.Visibility = Visibility.Visible;
                    pathEyeOpenConfirm.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BtnEyeConfirm_Click), ex);
            }
        }

        #endregion
    }
}