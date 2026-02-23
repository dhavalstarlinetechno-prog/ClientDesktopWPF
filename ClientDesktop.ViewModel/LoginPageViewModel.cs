using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class LoginPageViewModel : ViewModelBase, ICloseable
    {
        private readonly SessionService _sessionService;
        private readonly AuthService _authService;
        private const int Threshold = 3;

        public Action CloseAction { get; set; }

        public List<ServerList> AllServers { get; private set; } = new();
        public ObservableCollection<ServerList> FilteredServers { get; } = new();
        public ObservableCollection<string> LoginHistory { get; } = new();

        // --- Properties ---
        private ServerList _selectedServer;
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

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                if (!string.IsNullOrEmpty(value) && !value.All(char.IsDigit))
                    return;

                if (SetProperty(ref _username, value))
                {
                    CheckLoginHistory();
                }
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isRememberMe;
        public bool IsRememberMe { get => _isRememberMe; set => SetProperty(ref _isRememberMe, value); }

        public bool IsBusy { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public LoginPageViewModel(SessionService sessionService, AuthService authService)
        {
            _sessionService = sessionService;
            _authService = authService;

            LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync());
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());

            _ = LoadServerListAsync();
        }

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

        public void FilterServers(string input)
        {
            if (AllServers == null) return;

            FilteredServers.Clear();

            if (string.IsNullOrWhiteSpace(input) || input.Length < Threshold)
            {
                return;
            }

            var matches = AllServers
                .Where(s => (s.companyName ?? string.Empty)
                    .IndexOf(input.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(20)
                .ToList();

            foreach (var item in matches)
            {
                FilteredServers.Add(item);
            }
        }

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

                if (result.Success)
                {
                    var d = result.Data;
                    DateTime? exp = DateTime.TryParse(d.expiration, out var dt) ? dt : null;
                    _sessionService.SetSession(d.token, Username, d.name ?? Username, licenseId, exp, Password);

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

                    FileLogger.Log("Network", $"User '{Username}' Authorized Successfully.");
                }
                else
                {
                    LogDisconnect();
                }
            }
            catch (Exception ex)
            {
                LogDisconnect();
            }
            finally
            {
                CloseAction?.Invoke();
            }
        }

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
    }
}
