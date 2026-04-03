using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the Change Password flow and validation logic.
    /// </summary>
    public class ChangePasswordViewModel : ViewModelBase, ICloseable
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly ChangePasswordService _changePasswordService;

        private string _userName;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        // Enabled States
        private bool _isNewPasswordEnabled;
        private bool _isConfirmPasswordEnabled;
        private bool _isUpdateEnabled;

        // Visibility States
        private bool _isCurrentValidationVisible;
        private bool _isValidationVisible;
        private bool _isConfirmValidationVisible;

        // Validation Rules
        private bool _isCurrentPasswordValid;
        private bool _hasDigit;
        private bool _hasAlphabet;
        private bool _hasMinLength;
        private bool _isNotOldPassword;
        private bool _passwordsMatch;

        #endregion

        #region Properties

        public Action CloseAction { get; set; }

        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }

        public string CurrentPassword
        {
            get => _currentPassword;
            set { if (SetProperty(ref _currentPassword, value)) ValidateAll(); }
        }

        public string NewPassword
        {
            get => _newPassword;
            set { if (SetProperty(ref _newPassword, value)) ValidateAll(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { if (SetProperty(ref _confirmPassword, value)) ValidateAll(); }
        }

        // Enable/Disable Properties
        public bool IsNewPasswordEnabled { get => _isNewPasswordEnabled; set => SetProperty(ref _isNewPasswordEnabled, value); }
        public bool IsConfirmPasswordEnabled { get => _isConfirmPasswordEnabled; set => SetProperty(ref _isConfirmPasswordEnabled, value); }
        public bool IsUpdateEnabled { get => _isUpdateEnabled; set => SetProperty(ref _isUpdateEnabled, value); }

        // Visibility Properties
        public bool IsCurrentValidationVisible { get => _isCurrentValidationVisible; set => SetProperty(ref _isCurrentValidationVisible, value); }
        public bool IsValidationVisible { get => _isValidationVisible; set => SetProperty(ref _isValidationVisible, value); }
        public bool IsConfirmValidationVisible { get => _isConfirmValidationVisible; set => SetProperty(ref _isConfirmValidationVisible, value); }

        // Rule Properties
        public bool IsCurrentPasswordValid { get => _isCurrentPasswordValid; set => SetProperty(ref _isCurrentPasswordValid, value); }
        public bool HasDigit { get => _hasDigit; set => SetProperty(ref _hasDigit, value); }
        public bool HasAlphabet { get => _hasAlphabet; set => SetProperty(ref _hasAlphabet, value); }
        public bool HasMinLength { get => _hasMinLength; set => SetProperty(ref _hasMinLength, value); }
        public bool IsNotOldPassword { get => _isNotOldPassword; set => SetProperty(ref _isNotOldPassword, value); }
        public bool PasswordsMatch { get => _passwordsMatch; set => SetProperty(ref _passwordsMatch, value); }

        #endregion

        #region Commands

        public ICommand UpdateCommand { get; }

        #endregion

        #region Constructor

        public ChangePasswordViewModel(SessionService sessionService, ChangePasswordService changePasswordService)
        {
            _sessionService = sessionService;
            _changePasswordService = changePasswordService;

            UserName = _sessionService.UserId;

            UpdateCommand = new AsyncRelayCommand(async _ => await ExecuteUpdateAsync());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Single place to evaluate all validations. Master logic hub.
        /// </summary>
        private void ValidateAll()
        {
            try
            {
                // 1. Current Password Validation
                IsCurrentValidationVisible = !string.IsNullOrEmpty(CurrentPassword);
                IsCurrentPasswordValid = string.Equals(CurrentPassword, _sessionService.Password);

                // New password box typing is enabled immediately as soon as current password has some chars
                IsNewPasswordEnabled = !string.IsNullOrEmpty(CurrentPassword);

                // 2. New Password Validation
                IsValidationVisible = !string.IsNullOrEmpty(NewPassword);
                HasDigit = !string.IsNullOrEmpty(NewPassword) && NewPassword.Any(char.IsDigit);
                HasAlphabet = !string.IsNullOrEmpty(NewPassword) && NewPassword.Any(char.IsLetter);
                HasMinLength = !string.IsNullOrEmpty(NewPassword) && NewPassword.Length >= 6;
                IsNotOldPassword = !string.IsNullOrEmpty(NewPassword) && NewPassword != CurrentPassword;

                // Confirm password block opens only if ALL new password rules are green
                IsConfirmPasswordEnabled = HasDigit && HasAlphabet && HasMinLength && IsNotOldPassword;

                if (!IsConfirmPasswordEnabled)
                {
                    ConfirmPassword = string.Empty; // Reset confirm field if new password becomes invalid
                }

                // 3. Confirm Password Validation
                IsConfirmValidationVisible = !string.IsNullOrEmpty(ConfirmPassword);
                PasswordsMatch = !string.IsNullOrEmpty(ConfirmPassword) && (NewPassword == ConfirmPassword);

                IsUpdateEnabled = IsCurrentPasswordValid && IsConfirmPasswordEnabled && PasswordsMatch;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ValidateAll), ex);
            }
        }

        /// <summary>
        /// Executes the API call and logs the response to FileLogger without showing any UI errors.
        /// </summary>
        private async Task ExecuteUpdateAsync()
        {
            try
            {
                if (!IsUpdateEnabled) return;

                var (success, errorMsg) = await _changePasswordService.VerifyUserPasswordAsync(
                    UserName, CurrentPassword, NewPassword, ConfirmPassword);

                if (success)
                {
                    FileLogger.Log("Auth", $"Password updated successfully for user '{UserName}'.");
                    CloseAction?.Invoke();
                }
                else
                {
                    FileLogger.Log("Auth", $"Password update failed: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExecuteUpdateAsync), ex);
            }
        }

        #endregion
    }
}