using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for handling user login, server selection, and authentication.
    /// </summary>
    public class LoginPageViewModel : ViewModelBase, ICloseable
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly AuthService _authService;
        private readonly IDialogService _dialogService;

        private ServerList _selectedServer;
        private string _username;
        private string _password;
        private bool _isRememberMe;

        #endregion

        #region Properties

        public Action CloseAction { get; set; }

        public List<ServerList> AllServers { get; private set; } = new();

        public ObservableCollection<ServerList> FilteredServers { get; } = new();

        public ObservableCollection<string> LoginHistory { get; } = new();

        public ServerList SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (SetProperty(ref _selectedServer, value))
                {
                    LoadLoginHistoryForServer();
                    CheckLoginHistory();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (!string.IsNullOrEmpty(value) && !value.All(char.IsDigit))
                {
                    return;
                }

                if (SetProperty(ref _username, value))
                {
                    CheckLoginHistory();
                }
            }
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsRememberMe
        {
            get => _isRememberMe;
            set => SetProperty(ref _isRememberMe, value);
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenDisclaimerCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LoginPageViewModel class.
        /// </summary>
        public LoginPageViewModel(SessionService sessionService, AuthService authService, IDialogService dialogService)
        {
            _sessionService = sessionService;
            _authService = authService;
            _dialogService = dialogService;

            LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync());
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
            OpenDisclaimerCommand = new RelayCommand(OpenDisclaimer);

            _ = LoadServerListAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously loads the list of available servers and applies the last successful login selection.
        /// </summary>
        public async Task LoadServerListAsync()
        {
            try
            {
                var serverList = await _authService.GetServerListAsync();
                AllServers = serverList ?? new List<ServerList>();
                _sessionService.SetServerList(AllServers);

                FilteredServers.Clear();

                string cLic = _sessionService.LastSelectedLogin.LicenseId;
                string cUser = _sessionService.LastSelectedLogin.UserId;

                if (string.IsNullOrEmpty(cLic)) cLic = _sessionService.LicenseId;
                if (string.IsNullOrEmpty(cUser)) cUser = _sessionService.UserId;

                if (!string.IsNullOrEmpty(cLic))
                {
                    var server = AllServers.FirstOrDefault(s => s.licenseId.ToString() == cLic);
                    if (server != null)
                    {
                        if (!FilteredServers.Contains(server)) FilteredServers.Add(server);
                        SelectedServer = server;
                    }
                }

                if (!string.IsNullOrEmpty(cUser))
                {
                    Username = cUser;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the login history specific to the currently selected server.
        /// </summary>
        private void LoadLoginHistoryForServer()
        {
            LoginHistory.Clear();
            Password = string.Empty;
            IsRememberMe = false;

            var history = _authService.GetLoginHistory();
            if (history != null && SelectedServer != null)
            {
                foreach (var item in history)
                {
                    if (item.LicenseId == SelectedServer.licenseId.ToString())
                    {
                        LoginHistory.Add(item.UserId);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the login history to auto-fill the password if the user has chosen to be remembered.
        /// </summary>
        private void CheckLoginHistory()
        {
            if (string.IsNullOrEmpty(Username) || SelectedServer == null)
            {
                Password = string.Empty;
                IsRememberMe = false;
                return;
            }

            var history = _authService.GetLoginHistory();
            var loginInfo = history?.FirstOrDefault(s =>
                s != null &&
                string.Equals(s.UserId, Username, StringComparison.OrdinalIgnoreCase) &&
                s.LicenseId == SelectedServer.licenseId.ToString()
            );

            if (loginInfo != null && !string.IsNullOrEmpty(loginInfo.Password))
            {
                Password = loginInfo.Password;
                IsRememberMe = true;
            }
            else
            {
                Password = string.Empty;
                IsRememberMe = false;
            }
        }

        /// <summary>
        /// Authenticates the user credentials against the selected server and establishes the session.
        /// </summary>
        private async Task LoginAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password) || SelectedServer == null)
                {
                    LogDisconnect();
                    return;
                }

                string licenseId = SelectedServer?.licenseId.ToString() ?? "";

                _sessionService.SetSession(string.Empty, Username, Username, licenseId, null, Password);

                var result = await _authService.LoginAsync(Username, Password, licenseId, IsRememberMe);

                if (!result.Success && !string.IsNullOrEmpty(result.Message))
                {
                    FileLogger.Log("Network", result.Message);
                }
                else if (!result.Success)
                {
                    LogDisconnect();
                }
            }
            catch (Exception)
            {
                LogDisconnect();
            }
            finally
            {
                CloseAction?.Invoke();
            }
        }

        /// <summary>
        /// Logs a disconnection event for the current user or a general disconnection.
        /// </summary>
        private void LogDisconnect()
        {
            string compName = SelectedServer?.companyName;

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(compName))
            {
                FileLogger.Log("Network", $"User '{Username}' Disconnected from {compName}");
            }
            else
            {
                FileLogger.Log("Network", "Disconnected");
            }
        }

        /// <summary>
        /// Opens the disclaimer dialog via the dialog service.
        /// </summary>
        private void OpenDisclaimer(object parameter)
        {
            _dialogService.ShowDialog<DisclaimerViewModel>(string.Empty);
        }

        #endregion
    }
}