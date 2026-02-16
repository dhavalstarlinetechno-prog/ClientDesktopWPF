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
        private readonly AuthService _authService;

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
                if (SetProperty(ref _username, value))
                {
                    CheckLoginHistory();
                }
            }
        }

        private string _password;
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        private bool _isRememberMe;
        public bool IsRememberMe { get => _isRememberMe; set => SetProperty(ref _isRememberMe, value); }

        public bool IsBusy { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public LoginPageViewModel()
        {
            _authService = new AuthService();
            LoginCommand = new RelayCommand(async _ => await LoginAsync());
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());

            _ = LoadServerListAsync();
        }

        public async Task LoadServerListAsync()
        {
            try
            {
                var serverList = await _authService.GetServerListAsync();
                AllServers = serverList ?? new List<ServerList>();
                SessionManager.SetServerList(AllServers);

                FilteredServers.Clear();

                string cLic = SessionManager.LastSelectedLogin.LicenseId;
                string cUser = SessionManager.LastSelectedLogin.UserId;

                if (string.IsNullOrEmpty(cLic)) cLic = SessionManager.LicenseId;
                if (string.IsNullOrEmpty(cUser)) cUser = SessionManager.UserId;

                if (!string.IsNullOrEmpty(cLic))
                {
                    var server = AllServers.FirstOrDefault(s => s.licenseId.ToString() == cLic);
                    if (server != null)
                    {
                        // Add to filtered list so it shows up
                        if (!FilteredServers.Contains(server)) FilteredServers.Add(server);

                        SelectedServer = server; // This triggers history check
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

        // --- HELPER: Dropdown Items ---
        private void LoadLoginHistoryForServer()
        {
            LoginHistory.Clear();
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

        // --- HELPER: Auto-Fill Password ---
        private void CheckLoginHistory()
        {
            if (string.IsNullOrEmpty(Username) || SelectedServer == null) return;

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
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) return;

            string licenseId = SelectedServer?.licenseId.ToString() ?? "";

            if (string.IsNullOrEmpty(licenseId)) return;

            SessionManager.SetSession(string.Empty, Username, Username, licenseId, null, Password);

            try
            {
                var result = await _authService.LoginAsync(Username, Password, licenseId, IsRememberMe);

                if (result.Success)
                {
                    var d = result.Data;
                    DateTime? exp = DateTime.TryParse(d.expiration, out var dt) ? dt : null;
                    SessionManager.SetSession(d.token, Username, d.name ?? Username, licenseId, exp, Password);

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
                }

                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                FileLogger.Log("Network", ex.Message);
                CloseAction?.Invoke();
            }
        }
    }
}
