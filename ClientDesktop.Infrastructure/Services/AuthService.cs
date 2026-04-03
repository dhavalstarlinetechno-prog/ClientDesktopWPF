using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for handling user authentication, session management, and server list retrieval.
    /// </summary>
    public class AuthService
    {
        #region Fields

        private readonly IRepository<List<LoginInfo>> _loginRepo;
        private readonly IRepository<List<ServerList>> _serverRepo;
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AuthService class.
        /// </summary>
        public AuthService(IApiService apiService, SessionService sessionService)
        {
            try
            {
                _loginRepo = new FileRepository<List<LoginInfo>>();
                _serverRepo = new FileRepository<List<ServerList>>();
                _apiService = apiService;
                _sessionService = sessionService;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AuthService), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the list of available servers from the local cache or API.
        /// </summary>
        public async Task<List<ServerList>> GetServerListAsync()
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe("Servers");
                string fileName = AESHelper.ToBase64UrlSafe("ServerList");

                string relativePath = Path.Combine(folderName, fileName);

                var cachedList = _serverRepo.Load(relativePath);
                if (cachedList != null && cachedList.Count > 0)
                {
                    return cachedList;
                }

                var response = await _apiService.GetAsync<ServerListResponse>(AppConfig.ServerListURL);
                if (response?.data?.licenseDetail != null)
                {
                    _serverRepo.Save(relativePath, response.data.licenseDetail);
                    return response.data.licenseDetail;
                }
            }
            catch (Exception ex)
            {
                // Safely log API or IO exceptions instead of ignoring them
                FileLogger.ApplicationLog(nameof(GetServerListAsync), ex);
            }

            return new List<ServerList>();
        }

        /// <summary>
        /// Authenticates the user with the given credentials and license ID, and broadcasts the login signal on success.
        /// </summary>
        public async Task<(bool Success, string Message)> LoginAsync(string user, string pass, string licenseId, bool isRemember)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "username", user },
                    { "password", pass },
                    { "licenseId", licenseId }
                };

                string url = CommonHelper.ToReplaceUrl(AppConfig.AuthURL, _sessionService.PrimaryDomain);

                var result = await _apiService.PostFormAsync<AuthResponse>(url, formData);

                if (result != null && result.isSuccess && result.data != null)
                {
                    var data = result.data;

                    DateTime? exp = DateTime.TryParse(data.expiration, out var dt) ? dt : null;
                    _sessionService.SetSession(data.token, user, data.name, licenseId, exp, pass);
                    this.SaveLoginHistory(user, pass, licenseId, isRemember);

                    var profileResult = await this.GetUserProfileAsync();
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

                    return (true, "Success");
                }

                string errorMessage = result?.successMessage ?? ((result?.exception as Newtonsoft.Json.Linq.JContainer)?.Last as Newtonsoft.Json.Linq.JProperty)?.Value?.ToString() ?? "Login Failed";

                FileLogger.ApplicationLog(nameof(LoginAsync), $"Login failed for user '{user}': {errorMessage}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoginAsync), ex);
                return (false, string.Empty);
            }
        }

        /// <summary>
        /// Retrieves the profile information for the currently authenticated user.
        /// </summary>
        public async Task<AuthResponseObj> GetUserProfileAsync()
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.AuthURL, _sessionService.PrimaryDomain);
                return await _apiService.GetAsync<AuthResponseObj>(url);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetUserProfileAsync), ex);
                return null;
            }
        }

        /// <summary>
        /// Saves the user's login history, including session state and preferences, to local storage.
        /// </summary>
        public void SaveLoginHistory(string user, string pass, string licenseId, bool isRemember)
        {
            try
            {
                string fileName = AESHelper.ToBase64UrlSafe("LoginData");

                var list = _loginRepo.Load(fileName) ?? new List<LoginInfo>();

                var existingUser = list.FirstOrDefault(u => u.UserId == user && u.LicenseId == licenseId);

                if (existingUser != null)
                {
                    existingUser.Username = _sessionService.Username;
                    existingUser.Expiration = _sessionService.Expiration;
                    existingUser.ServerListData = _sessionService.ServerListData;
                    existingUser.Password = isRemember ? pass : string.Empty;
                    existingUser.LastLogin = true;
                }
                else
                {
                    list.Add(new LoginInfo
                    {
                        UserId = user,
                        Username = _sessionService.Username,
                        LicenseId = licenseId,
                        Expiration = _sessionService.Expiration,
                        ServerListData = _sessionService.ServerListData,
                        Password = isRemember ? pass : string.Empty,
                        LastLogin = true
                    });
                }

                foreach (var u in list)
                {
                    if (u.UserId != user || u.LicenseId != licenseId)
                    {
                        u.LastLogin = false;
                    }
                }

                _loginRepo.Save(fileName, list);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SaveLoginHistory), ex);
            }
        }

        /// <summary>
        /// Loads and retrieves the login history data from local storage.
        /// </summary>
        public List<LoginInfo> GetLoginHistory()
        {
            try
            {
                string fileName = AESHelper.ToBase64UrlSafe("LoginData");
                return _loginRepo.Load(fileName) ?? new List<LoginInfo>();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetLoginHistory), ex);
                return new List<LoginInfo>();
            }
        }

        #endregion
    }
}