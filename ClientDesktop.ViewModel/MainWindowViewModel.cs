using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using CommunityToolkit.Mvvm.Messaging;
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
        private bool _isPasswordReadOnly;

        #endregion

        #region Properties

        public string Title { get => _title; set => SetProperty(ref _title, value); }

        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }

        public bool IsLoggedIn { get => _isLoggedIn; set => SetProperty(ref _isLoggedIn, value); }
        public bool IsPasswordReadonly { get => _isPasswordReadOnly; set => SetProperty(ref _isPasswordReadOnly, value); }

        public MarketWatchViewModel MarketWatchVM { get; }
        public NavigationViewModel NavigationVM { get; }

        #endregion

        #region Commands

        public ICommand DisconnectCommand { get; }

        public ICommand ShowLoginCommand { get; }

        public ICommand OpenNewOrderCommand => new RelayCommand(param => OpenNewOrderWindow());
        public ICommand ChangePasswordCommand => new RelayCommand(param => OpenChangePasswordWindow());

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
            MarketWatchViewModel marketWatchVM,
            NavigationViewModel navigationVM)
        {
            _sessionService = sessionService;
            _authService = authService;
            _clientService = clientService;
            _dialogService = dialogService;

            MarketWatchVM = marketWatchVM;
            NavigationVM = navigationVM;

            DisconnectCommand = new RelayCommand(_ => Disconnect());
            ShowLoginCommand = new RelayCommand(_ => ShowLoginWindow());

            // Initialize the Messenger to listen for authentication signals
            RegisterMessenger();
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

            if (!_sessionService.IsInternetAvailable)
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
                    // Attempts to automatically log in using saved credentials.
                    var result = await _authService.LoginAsync(existingUser.UserId, existingUser.Password, existingUser.LicenseId, true);

                    if (!result.Success && !string.IsNullOrEmpty(result.Message))
                    {
                        FileLogger.Log("Network", result.Message);
                        ShowLoginWindow();
                    }
                    else
                    {
                        await PerformPostLoginSetup();
                    }
                }
                else
                {
                    ShowLoginWindow();
                }
            }
        }

        /// <summary>
        /// Opens the login dialog. Setup is handled via Event Aggregator upon successful login signal.
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
        /// Registers to Event Aggregator signals such as UserAuthSignal.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, (recipient, message) =>
            {
                if (!message.IsLoggedIn)
                {
                    HandleLogout();
                }
            });
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
                        var result = await _clientService.GetClientListAsync(specificData.Clients);
                        _sessionService.IsClientDataLoaded = true;
                        _sessionService.SetClientList(result.Clients);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Client Data Load Error: " + ex.Message);
                    }
                }

                InitializeAfterLogin();

                WeakReferenceMessenger.Default.Send(new UserAuthEvent(true, _sessionService.UserId));
            }
            else
            {
                // Trigger logout signal if disclaimer is rejected
                WeakReferenceMessenger.Default.Send(new UserAuthEvent(false, string.Empty));
            }
        }

        /// <summary>
        /// Displays the disclaimer dialog and returns whether the user acknowledged it.
        /// </summary>
        private bool ShowDisclaimerAndCheck()
        {
            bool isAcknowledged = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisclaimerViewModel disclaimerVM = null;

                _dialogService.ShowDialog<DisclaimerViewModel>(string.Empty, vm =>
                {
                    disclaimerVM = vm;
                });

                if (disclaimerVM != null)
                {
                    isAcknowledged = disclaimerVM.IsAcknowledged;
                }
            });

            return isAcknowledged;
        }

        /// <summary>
        /// Initializes the application state properties after a successful login.
        /// </summary>
        private void InitializeAfterLogin()
        {
            UserId = _sessionService.UserId;
            IsLoggedIn = true;
            IsPasswordReadonly = _sessionService.IsPasswordReadOnly;
            Title = _sessionService.ServerListData?.FirstOrDefault(q => q?.licenseId.ToString() == _sessionService.LicenseId)?.serverDisplayName ?? "";
        }

        /// <summary>
        /// Sets the application state to restricted mode, clearing user-specific data from the UI.
        /// </summary>
        private void SetRestrictedMode()
        {
            Title = string.Empty;
            IsLoggedIn = false;
            IsPasswordReadonly = true;
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
        /// Opens a dialog window that allows the user to change their password.
        /// </summary>
        /// <remarks>This method displays a modal dialog for password change, typically using a view model
        /// to manage the process. The dialog is intended to be used when a user needs to update their credentials
        /// during an active session.</remarks>
        private void OpenChangePasswordWindow()
        {
            _dialogService.ShowDialog<ChangePasswordViewModel>(
                "Change Password"
            );
        }

        /// <summary>
        /// Disconnects the current session by broadcasting a logout signal.
        /// </summary>
        private void Disconnect()
        {
            _sessionService.LastSelectedLogin = (string.Empty, string.Empty, string.Empty);
            WeakReferenceMessenger.Default.Send(new UserAuthEvent(false, string.Empty));
        }

        /// <summary>
        /// Handles the actual logout logic when a logout signal is received.
        /// </summary>
        private async void HandleLogout()
        {
            _sessionService.ClearSession();
            SetRestrictedMode();

            FileLogger.Log("Network", "Disconnected.");

            await Application.Current.Dispatcher.InvokeAsync(() => { });

            ShowLoginWindow();
        }

        #endregion
    }
}