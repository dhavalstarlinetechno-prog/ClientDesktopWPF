using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Core.Models;
using System.Windows.Input;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ClientService _clientService;
        private readonly IDialogService _dialogService; // Tera DialogService yaha use hoga

        private string _title = "Home";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private string _userId;
        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }

        private bool _isLoggedIn;
        public bool IsLoggedIn { get => _isLoggedIn; set => SetProperty(ref _isLoggedIn, value); }

        // Disclaimer ke liye View se connect karne wala hook
        public Func<bool>? OpenDisclaimerAction { get; set; }

        public ICommand DisconnectCommand { get; }
        public ICommand ShowLoginCommand { get; }

        public MainWindowViewModel(
            AuthService authService,
            ClientService clientService,
            IDialogService dialogService) // Constructor Injection
        {
            _authService = authService;
            _clientService = clientService;
            _dialogService = dialogService;

            DisconnectCommand = new RelayCommand(_ => Disconnect());
            ShowLoginCommand = new RelayCommand(_ => ShowLoginWindow());
        }

        // --- MAIN STARTUP LOGIC (Same as Home.cs InitializeHome) ---
        public async Task InitializeHomeAsync()
        {
            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (existingUser != null)
            {
                SessionManager.SetServerList(existingUser.ServerListData);
                SessionManager.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);
            }

            await _authService.GetServerListAsync();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                FileLogger.Log("Network", "No Internet Connection detected at startup.");
                return;
            }

            if (loginInfoList == null || !loginInfoList.Any())
            {
                ShowLoginWindow();
            }
            else
            {
                if (existingUser != null && !string.IsNullOrEmpty(existingUser.Password))
                {
                    bool loginSuccessful = await AutoLoginAsync(existingUser);

                    if (loginSuccessful)
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
        }

        private async Task<bool> AutoLoginAsync(LoginInfo user)
        {
            try
            {
                var result = await _authService.LoginAsync(user.UserId, user.Password, user.LicenseId, true);
                if (result.Success)
                {
                    var data = result.Data;
                    DateTime? exp = DateTime.TryParse(data.expiration, out var dt) ? dt : null;
                    SessionManager.SetSession(data.token, user.UserId, data.name, user.LicenseId, exp, user.Password);

                    var profileResult = await _authService.GetUserProfileAsync();
                    if (profileResult != null && profileResult.isSuccess && profileResult.data != null)
                    {
                        SocketLoginInfo socketInfo = new SocketLoginInfo
                        {
                            UserSubId = profileResult.data.sub,
                            UserIss = profileResult.data.iss,
                            LicenseId = SessionManager.LicenseId,
                            Intime = profileResult.data.intime,
                            Role = profileResult.data.role,
                            IpAddress = profileResult.data.ip,
                            Device = "Windows"
                        };
                        SessionManager.socketLoginInfos = socketInfo;
                        SessionManager.IsPasswordReadOnly = profileResult.data.isreadonlypassword;
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        // --- DIALOG SERVICE USAGE ---
        public void ShowLoginWindow()
        {
            // Ye DialogService use karke Login Page dikhayega
            _dialogService.ShowDialog<LoginPageViewModel>("Login", (vm) =>
            {
                // Jab dialog band hoga, ye code chalega
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    _ = PerformPostLoginSetup();
                }
            });
        }

        private async Task PerformPostLoginSetup()
        {
            // 1. Show Disclaimer
            bool disclaimerAcknowledged = ShowDisclaimerAndCheck();

            if (disclaimerAcknowledged)
            {
                // 2. Load Client Data
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
            else
            {
                ShowLoginWindow();
            }
        }

        private bool ShowDisclaimerAndCheck()
        {
            // Disclaimer Custom Window hai, isliye Delegate se open karenge
            if (OpenDisclaimerAction != null)
            {
                return Application.Current.Dispatcher.Invoke(() => OpenDisclaimerAction.Invoke());
            }
            return false;
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
            FileLogger.Log("System", "User Disconnected.");
            ShowLoginWindow();
        }
    }
}