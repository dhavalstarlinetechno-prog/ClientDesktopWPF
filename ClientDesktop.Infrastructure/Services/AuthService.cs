using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TraderApp.Interfaces;
using TraderApp.Utils.Network;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.Utils.Storage;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace TraderApps.Services
{
    public class AuthService
    {
        private readonly IRepository<List<LoginInfo>> _loginRepo;
        private readonly IRepository<List<ServerList>> _serverRepo;
        private readonly IApiService _apiService;

        public AuthService()
        {
            _loginRepo = new FileRepository<List<LoginInfo>>();
            _serverRepo = new FileRepository<List<ServerList>>();
            _apiService = new ApiService();
        }

        #region Server Management

        public async Task<List<ServerList>> GetServerListAsync()
        {
            string folderName = AESHelper.ToBase64UrlSafe("Servers");
            string fileName = AESHelper.ToBase64UrlSafe("ServerList");

            string relativePath = Path.Combine(folderName, fileName);

            var cachedList = _serverRepo.Load(relativePath);
            if (cachedList != null && cachedList.Count > 0)
            {
                return cachedList;
            }

            try
            {
                var response = await _apiService.GetAsync<ServerListResponse>(AppConfig.ServerListURL);
                if (response?.data?.licenseDetail != null)
                {
                    _serverRepo.Save(relativePath, response.data.licenseDetail);
                    return response.data.licenseDetail;
                }
            }
            catch { }

            return new List<ServerList>();
        }
        #endregion

        #region Login & Auth
        public async Task<(bool Success, string Message, AuthResponseData Data)> LoginAsync(string user, string pass, string licenseId, bool isRemember)
        {
            var formData = new Dictionary<string, string>
            {
                { "username", user },
                { "password", pass },
                { "licenseId", licenseId }
            };

            string url = CommonHelper.ToReplaceUrl(AppConfig.AuthURL);

            var result = await _apiService.PostFormAsync<AuthResponse>(url, formData);

            if (result != null && result.isSuccess && result.data != null)
            {
                SaveLoginHistory(user, pass, licenseId, isRemember);
                return (true, "Success", result.data);
            }

            return (false, result?.successMessage ?? ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)result.exception).Last).Value.ToString() ?? "Login Failed", null);
        }

        public async Task<AuthResponseObj> GetUserProfileAsync()
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.AuthURL);
                return await _apiService.GetAsync<AuthResponseObj>(url);
            }
            catch
            {
                return null;
            }
        }

        public void SaveLoginHistory(string user, string pass, string licenseId, bool isRemember)
        {
            string fileName = AESHelper.ToBase64UrlSafe("LoginData");

            var list = _loginRepo.Load(fileName) ?? new List<LoginInfo>();

            var existingUser = list.FirstOrDefault(u => u.UserId == user && u.LicenseId == licenseId);

            if (existingUser != null)
            {
                existingUser.Username = SessionManager.Username;
                existingUser.Expiration = SessionManager.Expiration;
                existingUser.ServerListData = SessionManager.ServerListData;
                existingUser.Password = isRemember ? pass : string.Empty;
                existingUser.LastLogin = true;
            }
            else
            {
                list.Add(new LoginInfo
                {
                    UserId = user,
                    Username = SessionManager.Username,
                    LicenseId = licenseId,
                    Expiration = SessionManager.Expiration,
                    ServerListData = SessionManager.ServerListData,
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

        public List<LoginInfo> GetLoginHistory()
        {
            string fileName = AESHelper.ToBase64UrlSafe("LoginData");
            return _loginRepo.Load(fileName) ?? new List<LoginInfo>();
        }
        #endregion
    }
}