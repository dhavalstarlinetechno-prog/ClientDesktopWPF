using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Core.Models;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ClientService _clientService;
        private readonly IDialogService _dialogService;

        private string _title = "Home";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private string _userId;
        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }

        private bool _isLoggedIn;
        public bool IsLoggedIn { get => _isLoggedIn; set => SetProperty(ref _isLoggedIn, value); }

        public ICommand DisconnectCommand { get; }

        public MainWindowViewModel(
            AuthService authService,
            ClientService clientService,
            IDialogService dialogService)
        {
            _authService = authService;
            _clientService = clientService;
            _dialogService = dialogService;

            DisconnectCommand = new RelayCommand(_ => Disconnect());
        }

        public async Task InitializeHomeAsync()
        {
            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                FileLogger.Log("Network", "No Internet.");
                ShowLoginWindow();
                return;
            }

            if (existingUser != null && !string.IsNullOrEmpty(existingUser.Password))
            {
                bool success = await AutoLoginAsync(existingUser);
                if (success)
                {
                    await PerformPostLoginSetup();
                }
                else
                {
                    ShowLoginWindow();
                }
            }
            else
            {
                ShowLoginWindow();
            }
        }

        private async Task<bool> AutoLoginAsync(LoginInfo user)
        {
            var result = await _authService.LoginAsync(user.UserId, user.Password, user.LicenseId, true);
            if (result.Success)
            {
                var data = result.Data;
                DateTime? exp = null;
                if (DateTime.TryParse(data.expiration, out var dt)) exp = dt;
                SessionManager.SetSession(data.token, user.UserId, data.name, user.LicenseId, exp, user.Password);
                return true;
            }
            return false;
        }

        private void ShowLoginWindow()
        {
            _dialogService.ShowDialog<LoginPageViewModel>("Login", (vm) =>
            {
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    _ = PerformPostLoginSetup();
                }
                else
                {
                   
                }
            });
        }

        private async Task PerformPostLoginSetup()
        {
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                try
                {
                    var specificData = await _clientService.GetSpecificClientListAsync();
                    var result1 = await _clientService.GetClientListAsync(specificData.Clients);
                    SessionManager.IsClientDataLoaded = true;
                    SessionManager.SetClientList(result1.Clients);
                }
                catch (Exception ex)
                {
                    FileLogger.Log("Home", "Client Data Load Error: " + ex.Message);
                }
            }

            InitializeAfterLogin();
        }

        private void InitializeAfterLogin()
        {
            UserId = SessionManager.UserId;
            IsLoggedIn = true;
            Title = SessionManager.ServerListData?.FirstOrDefault(q => q?.licenseId.ToString() == SessionManager.LicenseId)?.serverDisplayName ?? "Home";
            FileLogger.Log("System", "Login Successful.");
        }

        private void Disconnect()
        {
            SessionManager.ClearSession();
            IsLoggedIn = false;
            UserId = "";
            ShowLoginWindow();
        }
    }
}