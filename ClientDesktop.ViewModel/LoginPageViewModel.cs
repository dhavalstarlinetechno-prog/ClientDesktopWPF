using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class LoginPageViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        public Action? CloseAction { get; set; }
        public Action<bool>? SetDialogResult { get; set; }
        public List<ServerList> AllServers { get; private set; } = new();
        public ObservableCollection<ServerList> FilteredServers { get; } = new();
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsRememberMe { get; set; }
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

        #region Server List Management
        public async Task LoadServerListAsync()
        {
            try
            {
                var serverList = await _authService.GetServerListAsync();

                AllServers = serverList ?? new List<ServerList>();
                SessionManager.SetServerList(AllServers);

                FilteredServers.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion

        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username) ||
                string.IsNullOrEmpty(Password) )
                // ||SelectedServer == null)
                return;

            string licenseId = "";/*SelectedServer.licenseId.ToString()*/;

            SessionManager.SetSession(string.Empty, Username, Username, licenseId, null, Password);

            try
            {
                var result = await _authService.LoginAsync(Username, Password, licenseId, IsRememberMe);

                if (result.Success)
                {
                    var d = result.Data;
                    DateTime? exp = DateTime.TryParse(d.expiration, out var dt) ? dt : null;

                    SessionManager.SetSession(d.token, Username, d.name ?? Username, licenseId, exp, Password);
                }

                // 🔥 IMPORTANT: WinForms jesa
                SetDialogResult?.Invoke(result.Success);
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                FileLogger.Log("Network", ex.Message);
                CloseAction?.Invoke(); // WinForms jesa
            }
        }
    }

}
