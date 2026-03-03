using ClientDesktop.Core.Base;
using System.Collections.ObjectModel;

namespace ClientDesktop.Core.Models
{
    public class NavigationNode : ViewModelBase
    {
        public LoginInfo LoginDetails { get; set; }
        public string DisplayName { get; set; }

        private bool _isActiveUser;
        public bool IsActiveUser
        {
            get => _isActiveUser;
            set => SetProperty(ref _isActiveUser, value);
        }
    }

    /// <summary>
    /// Represents a Server Node under Accounts.
    /// </summary>
    public class ServerNode
    {
        public string ServerName { get; set; }
        public string IconText { get; set; }
        public ObservableCollection<NavigationNode> Users { get; set; }
    }
}
