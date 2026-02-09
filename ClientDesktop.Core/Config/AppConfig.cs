namespace ClientDesktop.Core.Config
{
    public static class AppConfig
    {
        // 🔹 API URLs
        #region URLs

        public static readonly string MarketWatchSignalRUrl = "https://sglr:6020/offers";
        public static readonly string ServerListURL = "https://api.tradeprint.in:6001/sso/server/listByOperator/SRK";
        public static readonly string AuthURL = "https://api:6001/sso/auth";
        public static string symbolstreeviewwithclientrights = "https://api:6004/dealer/routemaster/symbolstreeviewwithclientrights";
        public static string getsymbolsbyrouteforclient = "https://api:6004/dealer/routemaster/getsymbolsbyrouteforclient/0";
        public static string NodeTree = "https://api:6004/dealer/routemaster/getsymbolsbyrouteforclient/";
        public static string DolorsignSymbol = "https://api:6004/dealer/routemaster/getsymbolsbyrouteforclient/";
        public static string GetPositionsForClient = "https://api:6006/query/position";
        public static string PositionOrderApiUrl = "https://api:6006/query/order";
        public static string GetHistoryForClient = "https://api:6006/query/deals/client";
        public static string GetPositionHistoryForClient = "https://api:6006/query/position/client";
        public static string ClientListURL = "https://api:6010/dealer/userdetails/clientmaster/clientholdings";
        public static string MasterClientListURL = "https://api:6004/dealer/clientmaster/clientprofile";
        public static string MarketWatchInitDataUrl = "https://api:6007/dealer/watchprofile/clientwatchprofile/web";
        public static string MarketWatchHideApiUrl = "https://api:6004/dealer/clientwatchprofile/symbolhide";
        public static string MarketWatchSaveClientProfileUrl = "https://api:6004/dealer/clientwatchprofile";
        public static string GetBanscript = "https://api:6007/dealer/watchprofile/banscripts/getbydate/";
        public static string Getinvoice = "https://api:6004/dealer/invoice/";
        public static string GetLedgerListURL = "https://api:6004/dealer/ledger/historyforclient";
        public static string LedgerAuthnticationURL = "https://api:6004/dealer/clientmaster/UpdateInvoiceVisible";
        public static string GetSymbolDataForTrade = "https://api:6010/dealer/userdetails/clientmaster/clientsymbols";
        public static string TradeOrderURL = "https://api:6005/trade/order";
        public static string ChangePasswordURL = "https://api:6004/dealer/usermaster/changepassword";
        public static string SpectificationURL = "https://api:6004/dealer/routemaster/getsymbols/";
        public static string LederUserURL = "https://api:6004/dealer/ledger/userdetail/";
        public static string FeedbackURL = "https://api:6004/dealer/feedback";
        public static string FeedbackGenerateURL = "https://api:6004/dealer/feedback/generate";
        public static string FeedbackRetrieveURL = "https://api:6004/dealer/feedback/";
        public static string FeedbackReplayURL = "https://api:6004/dealer/feedback/sendreplynew";
        public static string FeedbackDeleteURL = "https://api:6004/dealer/feedback/";
        public static string DeleteOrderURL = "https://api:6005/trade/order/deleteOrder";
        public static string HistoryDataUrl = "https://api:6004/dealer/RateHistory/GetHistoryNew";
        
        #region SocketURL 
        public static string SocketURL = "wss://skt:6011/socket.io/?EIO=4&transport=websocket";
        #endregion
        
        #endregion

        // 🔹 File Paths
        #region File Path

        public static readonly string MarketWatchInitData = @"C:\Client Desktop\Json Response\symbol_full_full_data.txt";
        public static readonly string ClientDetailData = @"C:\Client Desktop\Json Response\dealer-clientmaster-clientprofile.txt";
        public static readonly string RouteMasterGetSymbols = @"C:\Client Desktop\Json Response\routemaster_getsymbols.txt";
        public static readonly string GetInvoice = @"C:\Client Desktop\Json Response\dealer-invoice-2025-09-15--2025-09-20-PreviousWeek.txt";
        public static readonly string GetLedger = @"C:\Client Desktop\Json Response\dealer-ledger-historyforclient.txt";
        public static readonly string ServerListFile = @"C:\Client Desktop\Json Response\serverlist.txt";
        public static readonly string dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TraderApp",
            "Data"
        );
        #endregion


        public static string AppDataPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClientDesktop",
            "Data"
        );

        // 🔹 Encryption Keys (AESHelper)
        public const string EncryptionKey = "Client__Desktop!Secret__Key-2025";
        public const string EncryptionIV = "ClientDesktop_16";
    }
}