using ClientDesktop.Core.Base;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class NavigationViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly AuthService _authService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ServerNode> Servers { get; } = new ObservableCollection<ServerNode>();

        public ICommand OpenMenuCommand { get; }
        public ICommand UserClickCommand { get; }

        public NavigationViewModel(SessionService sessionService, AuthService authService, IDialogService dialogService)
        {
            _sessionService = sessionService;
            _authService = authService;
            _dialogService = dialogService;

            OpenMenuCommand = new RelayCommand<string>(OpenMenu);
            UserClickCommand = new RelayCommand<NavigationNode>(OnUserClicked);

            // Initialize the Messenger to listen for authentication signals
            RegisterMessenger();
            LoadServers();
        }

        /// <summary>
        /// Registers to Event Aggregator signals such as UserAuthSignal.
        /// </summary>
        private void RegisterMessenger()
        {
            try
            {
                WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, (recipient, message) =>
                {
                    try
                    {
                        if (message.IsLoggedIn)
                        {
                            LoadServers();
                        }
                        else
                        {
                            ResetActiveUserStyles();
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(RegisterMessenger) + "_Callback", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(RegisterMessenger), ex);
            }
        }

        private void LoadServers()
        {
            try
            {
                Servers.Clear();
                var loginInfos = _authService.GetLoginHistory() ?? new System.Collections.Generic.List<LoginInfo>();

                var groupedLogins = loginInfos
                    .Where(l => l != null)
                    .GroupBy(l =>
                    {
                        var server = l.ServerListData?.FirstOrDefault(w => w.licenseId.ToString() == l.LicenseId);
                        return new { ServerName = server?.serverDisplayName ?? "Unknown Server", LicenseId = l.LicenseId };
                    })
                    .ToList();

                foreach (var group in groupedLogins)
                {
                    var serverNode = new ServerNode
                    {
                        ServerName = group.Key.ServerName,
                        IconText = !string.IsNullOrEmpty(group.Key.ServerName) ? group.Key.ServerName.Substring(0, 1).ToUpper() : "S",
                        Users = new ObservableCollection<NavigationNode>()
                    };

                    foreach (var login in group)
                    {
                        bool isCurrentActive = login.UserId == _sessionService.UserId && login.LicenseId == _sessionService.LicenseId;

                        serverNode.Users.Add(new NavigationNode
                        {
                            LoginDetails = login,
                            DisplayName = $"{login.UserId} [{login.Username}]",
                            IsActiveUser = isCurrentActive
                        });
                    }

                    Servers.Add(serverNode);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadServers), ex);
            }
        }

        private void ResetActiveUserStyles()
        {
            try
            {
                foreach (var server in Servers)
                {
                    foreach (var user in server.Users)
                    {
                        user.IsActiveUser = false;
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ResetActiveUserStyles), ex);
            }
        }

        private void OpenMenu(string menuName)
        {
            try
            {
                if (string.IsNullOrEmpty(menuName)) return;

                switch (menuName)
                {
                    case "BanScript":
                        _dialogService.ShowDialog<BanScriptViewModel>("Ban Script");
                        break;
                    case "Invoice":
                        _dialogService.ShowDialog<InvoiceViewModel>("Invoice");
                        break;
                    case "Ledger":
                        _dialogService.ShowDialog<LedgerViewModel>("Ledger");
                        break;
                    case "Feedback":
                        _dialogService.ShowDialog<FeedbackViewModel>("Feedback");
                        break;
                    case "Disclaimer":
                        _dialogService.ShowDialog<DisclaimerViewModel>(string.Empty);
                        break;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OpenMenu), ex);
            }
        }

        private void OnUserClicked(NavigationNode user)
        {
            try
            {
                if (user?.LoginDetails != null)
                {
                    _sessionService.LastSelectedLogin = (user.LoginDetails.UserId, user.LoginDetails.Password, user.LoginDetails.LicenseId);
                }

                WeakReferenceMessenger.Default.Send(new UserAuthEvent(false, string.Empty));
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OnUserClicked), ex);
            }
        }
    }
}