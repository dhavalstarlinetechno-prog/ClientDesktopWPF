using ClientDesktop.Core.Config;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace ClientDesktop.View.Navigation
{
    /// <summary>
    /// Interaction logic for NavigationView.xaml
    /// </summary>
    public partial class NavigationView : UserControl
    {        
        public ObservableCollection<TreeItem> NavigationItems { get; set; }
        private List<LoginInfo> _loginInfos = new List<LoginInfo>();

        public NavigationView()
        {
            InitializeComponent();
            DataContext = this;
            //_sessionService.OnLoginSuccess += () => Dispatcher.Invoke(() => LoadTree(true));
            //_sessionService.OnLogout += () => Dispatcher.Invoke(() => LoadTree(false));
           
            
            LoadTree();
            string filePath = System.IO.Path.Combine(AppConfig.dataFolder,$"{AESHelper.ToBase64UrlSafe("LoginData")}.dat");
            //_loginInfos = CommonHelper.LoadLoginDataFromCache(filePath);
        }
        
        private void LoadTree()
        {
            NavigationItems = new ObservableCollection<TreeItem>();

            // SVG PATHS
            string personPath =
                "M16,13C15.71,13 15.38,13 15.03,13.05C16.19,13.89 17,15 17,16.5V19H22V16.5C22,14.34 18.33,13 16,13M8,13C5.67,13 2,14.34 2,16.5V19H14V16.5C14,14.34 10.33,13 8,13M8,11A3,3 0 0,0 11,8A3,3 0 0,0 8,5A3,3 0 0,0 5,8A3,3 0 0,0 8,11M16,11A3,3 0 0,0 19,8A3,3 0 0,0 16,5A3,3 0 0,0 13,8A3,3 0 0,0 16,11Z";

            string companyPath =
                "M4,4H20V10H4V4M4,13H20V19H4V13M7,7V8H9V7H7M7,16V17H9V16H7Z";

            string banScriptPath = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M7,14H9V17H11V14H13V13H7V14M15,13H14V17H15V15.5H16V17H17V13H15M11.5,13L10.5,17H11.5L12.5,13H11.5Z";

            string invoicePath =
                "M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3M7,7H17V5H19V19H5V5H7V7M10.5,15L15,10.5L13.5,9L10.5,12L9,10.5L7.5,12L10.5,15Z";

            string ledgerPath =
                "M7,2H17A2,2 0 0,1 19,4V20A2,2 0 0,1 17,22H7A2,2 0 0,1 5,20V4A2,2 0 0,1 7,2M7,4V8H17V4H7M7,10V12H9V10H7M11,10V12H13V10H11M15,10V12H17V10H15M7,14V16H9V14H7M11,14V16H13V14H11M15,14V16H17V14H15M7,18V20H9V18H7M11,18V20H13V18H11M15,18V20H17V18H15Z";

            string feedbackPath =
                "M21,6H3A2,2 0 0,0 1,8V18A2,2 0 0,0 3,20H7V24L12,20H21A2,2 0 0,0 23,18V8A2,2 0 0,0 21,6Z";

            string disclamepath =
                "M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6M11,10V14H13V10H11M11,16V18H13V16H11Z";
            // ROOT
            var accounts = new TreeItem
            {
                Title = "Accounts",
                IconData = personPath,
                IconColor = "#2171b5"
            };

            var company1 = new TreeItem
            {
                Title = "JIO GLOBEX - LIVE",
                IconData = companyPath,
                IconColor = "#00C853"
            };

            company1.Children.Add(new TreeItem
            {
                Title = "100271 [Demo]",
                IconData = personPath,
                IconColor = "#2171b5"
            });

            var company2 = new TreeItem
            {
                Title = "METAODDS TECHNOLOGY - LIVE",
                IconData = companyPath,
                IconColor = "#00C853"
            };

            company2.Children.Add(new TreeItem
            {
                Title = "100271 [Demo]",
                IconData = personPath,
                IconColor = "#2171b5"
            });

            accounts.Children.Add(company1);
            accounts.Children.Add(company2);

            NavigationItems.Add(accounts);

            // OTHER MENU
            NavigationItems.Add(new TreeItem { Title = "Ban Script", IconData = banScriptPath, IconColor = "#E53935" });
            NavigationItems.Add(new TreeItem { Title = "Invoice", IconData = invoicePath, IconColor = "#1D5288" });
            NavigationItems.Add(new TreeItem { Title = "Ledger", IconData = ledgerPath, IconColor = "#FFC107" });
            NavigationItems.Add(new TreeItem { Title = "Feedback", IconData = feedbackPath, IconColor = "#2196F3" });
            NavigationItems.Add(new TreeItem { Title = "Disclaimer", IconData = disclamepath, IconColor = "#1976D2" });
        }
     
        #region
        //private void LoadTree(bool isLoggedIn)
        //{
        //    if (isLoggedIn)
        //    {
        //        NavigationItems = new ObservableCollection<TreeItem>();


        //        string personPath = "M16,13C15.71,13 15.38,13 15.03,13.05C16.19,13.89 17,15 17,16.5V19H22V16.5C22,14.34 18.33,13 16,13M8,13C5.67,13 2,14.34 2,16.5V19H14V16.5C14,14.34 10.33,13 8,13M8,11A3,3 0 0,0 11,8A3,3 0 0,0 8,5A3,3 0 0,0 5,8A3,3 0 0,0 8,11M16,11A3,3 0 0,0 19,8A3,3 0 0,0 16,5A3,3 0 0,0 13,8A3,3 0 0,0 16,11Z";
        //        string companyPath = "M4,4H20V10H4V4M4,13H20V19H4V13M7,7V8H9V7H7M7,16V17H9V16H7Z";
        //        string banScriptPath = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M7,14H9V17H11V14H13V13H7V14M15,13H14V17H15V15.5H16V17H17V13H15M11.5,13L10.5,17H11.5L12.5,13H11.5Z";
        //        string invoicePath = "M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3M7,7H17V5H19V19H5V5H7V7M10.5,15L15,10.5L13.5,9L10.5,12L9,10.5L7.5,12L10.5,15Z";
        //        string ledgerPath = "M7,2H17A2,2 0 0,1 19,4V20A2,2 0 0,1 17,22H7A2,2 0 0,1 5,20V4A2,2 0 0,1 7,2M7,4V8H17V4H7M7,10V12H9V10H7M11,10V12H13V10H11M15,10V12H17V10H15M7,14V16H9V14H7M11,14V16H13V14H11M15,14V16H17V14H15M7,18V20H9V18H7M11,18V20H13V18H11M15,18V20H17V18H15Z";
        //        string feedbackPath = "M21,6H3A2,2 0 0,0 1,8V18A2,2 0 0,0 3,20H7V24L12,20H21A2,2 0 0,0 23,18V8A2,2 0 0,0 21,6Z";
        //        string disclamepath = "M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6M11,10V14H13V10H11M11,16V18H13V16H11Z";


        //        var accountsRoot = new TreeItem
        //        {
        //            Title = "Accounts",
        //            IconData = personPath,
        //            IconColor = "#2171b5"
        //        };


        //        AddAccountsUnderNode(accountsRoot, companyPath, personPath);

        //        NavigationItems.Add(accountsRoot);


        //        NavigationItems.Add(new TreeItem { Title = "Ban Script", IconData = banScriptPath, IconColor = "#E53935" });
        //        NavigationItems.Add(new TreeItem { Title = "Invoice", IconData = invoicePath, IconColor = "#1D5288" });
        //        NavigationItems.Add(new TreeItem { Title = "Ledger", IconData = ledgerPath, IconColor = "#FFC107" });
        //        NavigationItems.Add(new TreeItem { Title = "Feedback", IconData = feedbackPath, IconColor = "#2196F3" });
        //        NavigationItems.Add(new TreeItem { Title = "Disclaimer", IconData = disclamepath, IconColor = "#1976D2" });
        //    }         
        //}

        //private void AddAccountsUnderNode(TreeItem accountsNode, string companyIcon, string personIcon)
        //{

        //    if (_loginInfos == null || !_loginInfos.Any()) return;


        //    var grouped = _loginInfos.GroupBy(l => l.LicenseId).ToList();

        //    foreach (var group in grouped)
        //    {

        //        var server = _sessionService.ServerListData?
        //                    .FirstOrDefault(w => w.licenseId.ToString() == group.Key);

        //        string serverDisplayName = server?.serverDisplayName ?? "Unknown Server";


        //        TreeItem serverNode = new TreeItem
        //        {
        //            Title = serverDisplayName,
        //            IconData = companyIcon, 
        //            IconColor = "#00C853"
        //        };


        //        foreach (var login in group)
        //        {
        //            TreeItem userNode = new TreeItem
        //            {
        //                Title = $"{login.UserId} [{login.Username}]",
        //                IconData = personIcon,
        //                IconColor = "#2171b5"
        //            };


        //            if (login.UserId == _sessionService.UserId && login.LicenseId == _sessionService.LicenseId)
        //            {
        //                userNode.Title += " (Active)";

        //            }

        //            serverNode.Children.Add(userNode);
        //        }


        //        accountsNode.Children.Add(serverNode);
        //    }
        //}
        #endregion
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NavigationTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as TreeItem;

            if (selectedItem != null && selectedItem.Title == "Ban Script")
            {
                Window popupWindow = new Window
                {
                    Title = "Ban Script",
                    Content = new BanScript(),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White,
                    Height = 240,
                    Width = 320
                };

                // 2. Show the window
                popupWindow.Show();
            } 
            else if (selectedItem != null && selectedItem.Title == "Ledger")
            {
                Window popupWindow = new Window
                {
                    Title = "Ledger",
                    Content = new Ledger(),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White,
                    Height = 653,
                    Width = 1000
                };

                // 2. Show the window
                popupWindow.Show();
            }
        }

        private void NavigationTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = checked((DependencyObject)e.OriginalSource);
            while (item != null && !(item is TreeViewItem))
            {
                item = VisualTreeHelper.GetParent(item);
            }

            if (item is TreeViewItem treeViewItem)
            {
                var selectedData = treeViewItem.DataContext as TreeItem;

                if (selectedData != null && selectedData.Title == "Ban Script")
                {
                    Window popupWindow = new Window
                    {
                        Title = "Ban Script",
                        Content = new BanScript(),
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = Brushes.White,
                        Height = 240,
                        Width = 320
                    };

                    popupWindow.Show();                    
                    e.Handled = true;
                }
                else if (selectedData != null && selectedData.Title == "Ledger")
                {
                    Window popupWindow = new Window
                    {
                        Title = "Ledger",
                        Content = new Ledger(),
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = Brushes.White,
                        Height = 653,
                        Width = 1000
                    };

                    // 2. Show the window
                    popupWindow.Show();
                    e.Handled = true;
                }
            }
        }
    }
}
