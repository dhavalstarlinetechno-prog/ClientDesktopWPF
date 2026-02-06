using ClientDesktop.Infrastructure.Base;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ClientService _clientService;
        private readonly MarketWatchService _marketWatchService; // Assuming you have this

        // Dependencies for showing windows (DI se aayenge)
        //private readonly Func<LoginPageViewModel> _loginVmFactory;

        // Properties bound to UI
        private string _title = "Home";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _userId;
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        public ICommand DisconnectCommand { get; }

        //public MainWindowViewModel(AuthService authService, ClientService clientService, Func<LoginPageViewModel> loginVmFactory)
        //{
        //    _authService = authService;
        //    _clientService = clientService;
        //    _loginVmFactory = loginVmFactory;

        //    DisconnectCommand = new RelayCommand(_ => Disconnect());
        //}

        // Ye method MainWindow.xaml.cs ke Loaded event se call hoga
        public async Task InitializeHomeAsync()
        {
            // Logic from Home.cs InitializeHome()

            // 1. Get Server List
            await _authService.GetServerListAsync();

            // 2. Check Login History
            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (existingUser != null)
            {
                SessionManager.SetServerList(existingUser.ServerListData);
                SessionManager.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);
            }

            // 3. Show Pre-Login Layout (MarketWatch loads local data)
            // Note: In WPF, MainWindow is already open. We just ensure MarketWatchView is loaded with local data.
            // This is handled by MarketWatchViewModel automatically if we initialize it.

            // 4. Check Internet
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                FileLogger.Log("Network", "No Internet Connection detected at startup.");
                ShowLoginWindow();
                return;
            }

            // 5. Auth Logic
            if (loginInfoList == null || !loginInfoList.Any())
            {
                ShowLoginWindow();
            }
            else
            {
                if (existingUser != null && !string.IsNullOrEmpty(existingUser.Password))
                {
                    // Auto Login Logic
                    // We need a temporary LoginViewModel just for logic
                    //var tempLoginVm = _loginVmFactory();

                    // We need to expose a public method in LoginViewModel or AuthService logic here
                    // Since LoginViewModel handles UI, we can try direct AuthService login here to keep it clean
                    // But your Home.cs used LoginPage for auto login. 

                    // Let's implement Auto-Login Logic directly here using AuthService
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
            //// WPF way to show modal dialog
            //// We use the Factory to get a fresh ViewModel instance
            //var loginVm = _loginVmFactory();

            //// Create the View (Ideally via a WindowManager, but doing manually for simplicity)
            //var loginWindow = new ClientDesktop.Main.Login.LoginPage();
            //loginWindow.DataContext = loginVm;

            //// Wire up actions
            //loginVm.CloseAction = () => loginWindow.Close();
            //loginVm.SetDialogResult = (result) => loginWindow.DialogResult = result;

            //bool? result = loginWindow.ShowDialog();

            //if (result == true)
            //{
            //    // Login Success
            //    _ = PerformPostLoginSetup();
            //}
        }

        private async Task PerformPostLoginSetup()
        {
            // Logic from Home.cs
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                try
                {
                    var specificData = await _clientService.GetSpecificClientListAsync();
                    // ... client details logic
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

            FileLogger.Log("System", "Login Successful. Full Access Enabled.");

            // Notify MarketWatchViewModel to reload via Messenger/Event or Property
            // For now, assume View handles this or we have a shared service
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
