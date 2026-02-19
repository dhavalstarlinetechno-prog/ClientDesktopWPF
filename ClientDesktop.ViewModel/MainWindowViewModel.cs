using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly AuthService _authService;
        private readonly ClientService _clientService;
        private readonly IDialogService _dialogService;

        private string _title = string.Empty;
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private string _userId;
        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }

        private bool _isLoggedIn;
        public bool IsLoggedIn { get => _isLoggedIn; set => SetProperty(ref _isLoggedIn, value); }

        public MarketWatchViewModel MarketWatchVM { get; }

        public Func<bool>? OpenDisclaimerAction { get; set; }

        public ICommand DisconnectCommand { get; }
        public ICommand ShowLoginCommand { get; }

        public MainWindowViewModel(
            SessionService sessionService,
            AuthService authService,
            ClientService clientService,
            IDialogService dialogService,
            MarketWatchViewModel marketWatchVM)
        {
            _sessionService = sessionService;
            _authService = authService;
            _clientService = clientService;
            _dialogService = dialogService;

            MarketWatchVM = marketWatchVM;

            DisconnectCommand = new RelayCommand(_ => Disconnect());
            ShowLoginCommand = new RelayCommand(_ => ShowLoginWindow());
        }

        public async Task InitializeHomeAsync()
        {
            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (existingUser != null)
            {
                _sessionService.SetServerList(existingUser.ServerListData);
                _sessionService.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);
            }

            SetRestrictedMode();

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
                    _sessionService.SetSession(data.token, user.UserId, data.name, user.LicenseId, exp, user.Password);

                    var profileResult = await _authService.GetUserProfileAsync();
                    if (profileResult != null && profileResult.isSuccess && profileResult.data != null)
                    {
                        SocketLoginInfo socketInfo = new SocketLoginInfo
                        {
                            UserSubId = profileResult.data.sub,
                            UserIss = profileResult.data.iss,
                            LicenseId = _sessionService.LicenseId,
                            Intime = profileResult.data.intime,
                            Role = profileResult.data.role,
                            IpAddress = profileResult.data.ip,
                            Device = "Windows"
                        };
                        _sessionService.socketLoginInfos = socketInfo;
                        _sessionService.IsPasswordReadOnly = profileResult.data.isreadonlypassword;
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        public void ShowLoginWindow()
        {
            _dialogService.ShowDialog<LoginPageViewModel>("Login", (vm) =>
            {
                if (_sessionService.IsLoggedIn)
                {
                    _ = PerformPostLoginSetup();
                }
            });
        }

        private async Task PerformPostLoginSetup()
        {
            bool disclaimerAcknowledged = ShowDisclaimerAndCheck();

            if (disclaimerAcknowledged)
            {
                if (_sessionService.IsLoggedIn)
                {
                    try
                    {
                        var specificData = await _clientService.GetSpecificClientListAsync();
                        var result1 = await _clientService.GetClientListAsync(specificData.Clients);
                        _sessionService.IsClientDataLoaded = true;
                        _sessionService.SetClientList(result1.Clients);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Client Data Load Error: " + ex.Message);
                    }
                }

                InitializeAfterLogin();
            }
            else
            {
                _sessionService.ClearSession();
                ShowLoginWindow();
            }
        }

        private bool ShowDisclaimerAndCheck()
        {
            if (OpenDisclaimerAction != null)
            {
                return Application.Current.Dispatcher.Invoke(() => OpenDisclaimerAction.Invoke());
            }
            return false;
        }

        private void InitializeAfterLogin()
        {
            UserId = _sessionService.UserId;
            IsLoggedIn = true;
            Title = _sessionService.ServerListData?.FirstOrDefault(q => q?.licenseId.ToString() == _sessionService.LicenseId)?.serverDisplayName ?? "Home";
            FileLogger.Log("System", "Login Successful.");
        }

        private void SetRestrictedMode()
        {
            Title = string.Empty;
            IsLoggedIn = false;
            UserId = string.Empty;
        }

        private void Disconnect()
        {
            _sessionService.ClearSession();
            SetRestrictedMode();
            FileLogger.Log("System", "User Disconnected.");
            ShowLoginWindow();
        }
    }
}