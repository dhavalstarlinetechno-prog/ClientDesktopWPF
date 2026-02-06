using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Base;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    //public class LoginPageViewModel : ViewModelBase
    //{
    //    private readonly AuthService _authService;

    //    // Action to close the window from ViewModel
    //    public Action? CloseAction { get; set; }
    //    public Action<bool>? SetDialogResult { get; set; }

    //    #region Properties

    //    private string _username;
    //    public string Username
    //    {
    //        get => _username;
    //        set => SetProperty(ref _username, value);
    //    }

    //    private string _password;
    //    public string Password
    //    {
    //        get => _password;
    //        set => SetProperty(ref _password, value);
    //    }

    //    private bool _isRememberMe;
    //    public bool IsRememberMe
    //    {
    //        get => _isRememberMe;
    //        set => SetProperty(ref _isRememberMe, value);
    //    }

    //    private ServerList _selectedServer;
    //    public ServerList SelectedServer
    //    {
    //        get => _selectedServer;
    //        set => SetProperty(ref _selectedServer, value);
    //    }

    //    private ObservableCollection<ServerList> _serverList;
    //    public ObservableCollection<ServerList> ServerList
    //    {
    //        get => _serverList;
    //        set => SetProperty(ref _serverList, value);
    //    }

    //    private bool _isBusy;
    //    public bool IsBusy
    //    {
    //        get => _isBusy;
    //        set => SetProperty(ref _isBusy, value);
    //    }

    //    #endregion

    //    #region Commands
    //    public ICommand LoginCommand { get; }
    //    public ICommand CancelCommand { get; }
    //    public ICommand TogglePasswordCommand { get; } // Logic inside View usually, but can be here
    //    #endregion

    //    public LoginPageViewModel(AuthService authService)
    //    {
    //        _authService = authService;
    //        ServerList = new ObservableCollection<ServerList>();

    //        LoginCommand = new RelayCommand(async _ => await LoginAsync());
    //        CancelCommand = new RelayCommand(_ => CancelLogin());

    //        // Initial Load
    //        LoadInitialData();
    //    }

    //    private async void LoadInitialData()
    //    {
    //        await LoadServerListAsync();

    //        // Auto-fill logic from LoginPage.cs
    //        if (!string.IsNullOrEmpty(SessionManager.UserId))
    //        {
    //            Username = SessionManager.UserId;
    //        }

    //        // Pre-select server logic similar to your WinForm
    //        if (!string.IsNullOrEmpty(SessionManager.LicenseId) && ServerList.Any())
    //        {
    //            SelectedServer = ServerList.FirstOrDefault(s => s.licenseId.ToString() == SessionManager.LicenseId);
    //        }
    //    }

    //    private async Task LoadServerListAsync()
    //    {
    //        IsBusy = true;
    //        try
    //        {
    //            var servers = await _authService.GetServerListAsync();
    //            ServerList.Clear();
    //            foreach (var server in servers)
    //            {
    //                ServerList.Add(server);
    //            }

    //            // Default selection logic
    //            if (ServerList.Count > 0 && SelectedServer == null)
    //            {
    //                SelectedServer = ServerList[0];
    //            }
    //        }
    //        finally
    //        {
    //            IsBusy = false;
    //        }
    //    }

    //    private async Task LoginAsync()
    //    {
    //        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
    //        {
    //            FileLogger.Log("Login", "Please enter valid credentials.");
    //            return;
    //        }

    //        IsBusy = true;
    //        string licenseId = SelectedServer?.licenseId.ToString() ?? "0";

    //        // Clear session first (Your logic)
    //        SessionManager.SetSession(string.Empty, Username, string.Empty, licenseId, null, Password);

    //        try
    //        {
    //            var result = await _authService.LoginAsync(Username, Password, licenseId, IsRememberMe);

    //            if (!result.Success)
    //            {
    //                FileLogger.Log("Network", $"Login failed: {result.Message}");
    //                FileLogger.Log(result.Message, "Login Failed");
    //                return;
    //            }

    //            // Success Logic
    //            var data = result.Data;
    //            DateTime? exp = null;
    //            if (DateTime.TryParse(data.expiration, out var dt)) exp = dt;

    //            SessionManager.SetSession(data.token, Username, data.name ?? Username, licenseId, exp, Password);
    //            FileLogger.Log("Network", $"User '{Username}' Authorized Successfully.");

    //            // Get User Profile
    //            var profileResult = await _authService.GetUserProfileAsync();
    //            if (profileResult != null && profileResult.isSuccess && profileResult.data != null)
    //            {
    //                // Update SessionManager socket info (Logic preserved)
    //                SessionManager.IsPasswordReadOnly = profileResult.data.isreadonlypassword;
    //                // ... (Other socket info mapping)
    //            }

    //            // Close Window with True result
    //            SetDialogResult?.Invoke(true);
    //            CloseAction?.Invoke();
    //        }
    //        catch (Exception ex)
    //        {
    //            FileLogger.Log("Network", $"Login Exception: {ex.Message}");
    //        }
    //        finally
    //        {
    //            IsBusy = false;
    //        }
    //    }

    //    private void CancelLogin()
    //    {
    //        CloseAction?.Invoke();
    //    }
    //}
}
