using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the main application window, including authentication and global session state.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly AuthService _authService;
        private readonly ClientService _clientService;
        private readonly IDialogService _dialogService;

        private string _title = string.Empty;
        private string _userId;
        private bool _isLoggedIn;

        #endregion

        #region Properties

        public string Title { get => _title; set => SetProperty(ref _title, value); }

        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }

        public bool IsLoggedIn { get => _isLoggedIn; set => SetProperty(ref _isLoggedIn, value); }

        public MarketWatchViewModel MarketWatchVM { get; }

        public Func<bool>? OpenDisclaimerAction { get; set; }

        #endregion

        #region Commands

        public ICommand DisconnectCommand { get; }

        public ICommand ShowLoginCommand { get; }

        public ICommand OpenNewOrderCommand => new RelayCommand(param => OpenNewOrderWindow());

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// </summary>
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the home window, validates the network, and attempts auto-login.
        /// </summary>
        public async Task InitializeHomeAsync()
        {
            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (existingUser != null)
            {
                _sessionService.SetServerList(existingUser.ServerListData);
                _sessionService.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);
                MarketWatchVM.LoadLocalData();
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

        /// <summary>
        /// Opens the login dialog and triggers post-login setup upon successful authentication.
        /// </summary>
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Attempts to automatically log in using saved credentials.
        /// </summary>
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

                    FileLogger.Log("Network", $"User '{user.UserId}' Authorized Successfully.");
                    return true;
                }
                else
                {
                    FileLogger.Log("Network", $"User '{user.UserId}' Disconnected");
                }
            }
            catch
            {
                // Silently catch auto-login failures
            }
            return false;
        }

        /// <summary>
        /// Performs necessary setup operations like displaying disclaimers and loading client lists after login.
        /// </summary>
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

        /// <summary>
        /// Displays the disclaimer dialog and returns whether the user acknowledged it.
        /// </summary>
        private bool ShowDisclaimerAndCheck()
        {
            if (OpenDisclaimerAction != null)
            {
                return Application.Current.Dispatcher.Invoke(() => OpenDisclaimerAction.Invoke());
            }
            return false;
        }

        /// <summary>
        /// Initializes the application state properties after a successful login.
        /// </summary>
        private void InitializeAfterLogin()
        {
            UserId = _sessionService.UserId;
            IsLoggedIn = true;
            Title = _sessionService.ServerListData?.FirstOrDefault(q => q?.licenseId.ToString() == _sessionService.LicenseId)?.serverDisplayName ?? "Home";
        }

        /// <summary>
        /// Sets the application state to restricted mode, clearing user-specific data from the UI.
        /// </summary>
        private void SetRestrictedMode()
        {
            Title = string.Empty;
            IsLoggedIn = false;
            UserId = string.Empty;
        }

        /// <summary>
        /// Opens a new trade order dialog initialized with market order parameters.
        /// </summary>
        private void OpenNewOrderWindow()
        {
            _dialogService.ShowDialog<TradeViewModel>(
                "New Trade Order",
                configureViewModel: vm =>
                {
                    vm.CurrentOrderTypeEnum = EnumTradeOrderType.Market;
                    vm.CurrentWindowModeEnum = EnumTradeWindowMode.FromTradeButton;
                    vm.positionGridRow = null;

                    _ = vm.LoadSymbolListAsync();
                }
            );
        }

        /// <summary>
        /// Disconnects the current session and returns to the login window.
        /// </summary>
        private void Disconnect()
        {
            _sessionService.ClearSession();
            SetRestrictedMode();
            FileLogger.Log("Network", "Disconnected.");
            ShowLoginWindow();
        }

        #endregion
    }
}