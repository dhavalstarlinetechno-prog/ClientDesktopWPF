using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace ClientDesktop.Services
{
    public class SocketService : ISocketService
    {
        #region Private Fields
        private WebSocket _ws;
        private string _socketIoSid;
        private bool _namespaceOpened;
        private bool _manualDisconnect;
        private readonly object _reconnectLock = new object();
        private bool _isNetworkEventSubscribed = false;

        // Note: I kept SocketData as a private instance variable instead of static
        private readonly SocketData _socketData = new SocketData();
        #endregion

        #region Public Events
        public event Action<Position, bool> OnPositionUpdated;
        public event Action OnSocketReconnected;
        public event Action<OrderModel, bool, string> OnOrderUpdated;
        public event Action<string> OnForceLogout;
        public event Action<ClientDetails> OnUpdateUserBalance;

        public event Action<bool> OnViewLockChanged;
        public event Action<FeedbackReplyData> OnFeedbackReplyReceived;
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        #endregion

        #region Public Properties
        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
        #endregion

        #region Connection Lifecycle (Start / Stop)

        public SocketService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        public void Start()
        {
            try
            {
                _manualDisconnect = false;

                if (!_isNetworkEventSubscribed)
                {
                    NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
                    _isNetworkEventSubscribed = true;
                }

                InitializeConnection();
            }
            catch
            {
            }
        }

        public void Stop()
        {
            try
            {
                _manualDisconnect = true;

                if (_isNetworkEventSubscribed)
                {
                    NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                    _isNetworkEventSubscribed = false;
                }

                if (_ws != null)
                {
                    _ws.Opened -= Ws_Opened;
                    _ws.Closed -= Ws_Closed;
                    _ws.Error -= Ws_Error;
                    _ws.MessageReceived -= Ws_MessageReceived;

                    if (_ws.State == WebSocketState.Open)
                        _ws.Close();

                    _ws.Dispose();
                }

                _ws = null;
                _namespaceOpened = false;
            }
            catch { }
        }
        #endregion

        #region Network & Connection Management
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                if (!_manualDisconnect && !IsConnected)
                {
                    Task.Delay(1000).ContinueWith(_ => InitializeConnection());
                    Task.Run(async () =>
                    {
                        var formData = new Dictionary<string, string>
            {
                { "username", _sessionService.UserId },
                { "password", _sessionService.Password },
                { "licenseId", _sessionService.LicenseId }
            };

                        string url = CommonHelper.ToReplaceUrl(AppConfig.AuthURL, _sessionService.PrimaryDomain);

                        var result = await _apiService.PostFormAsync<AuthResponse>(url, formData);

                        if (result == null || !result.isSuccess)
                            OnForceLogout?.Invoke(_sessionService.UserId);
                        else
                            OnSocketReconnected?.Invoke();
                    });
                }
            }
            else
            {
                if (_ws != null && _ws.State != WebSocketState.None)
                {
                    _ws.Close();
                }
            }
        }

        private void InitializeConnection()
        {
            lock (_reconnectLock)
            {
                try
                {
                    if (_manualDisconnect) return;

                    if (_ws != null && (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting))
                        return;

                    if (_ws != null)
                    {
                        _ws.Opened -= Ws_Opened;
                        _ws.Closed -= Ws_Closed;
                        _ws.Error -= Ws_Error;
                        _ws.MessageReceived -= Ws_MessageReceived;
                        _ws.Dispose();
                    }

                    var serverName = _sessionService.ServerListData
                        .FirstOrDefault(w => w.licenseId.ToString() == _sessionService.LicenseId)
                        ?.primaryDomain;

                    if (string.IsNullOrEmpty(serverName)) return;

                    _ws = new WebSocket(serverName.ToWebSocketUrl());
                    _ws.Opened += Ws_Opened;
                    _ws.Closed += Ws_Closed;
                    _ws.Error += Ws_Error;
                    _ws.MessageReceived += Ws_MessageReceived;

                    _ws.Open();
                }
                catch
                {
                }
            }
        }
        #endregion

        #region Base WebSocket Event Handlers
        private void Ws_Opened(object sender, EventArgs e)
        {
            _namespaceOpened = false;
        }

        private void Ws_Closed(object sender, EventArgs e)
        {
            _namespaceOpened = false;
        }

        private void Ws_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {

        }
        #endregion
        #region WebSocket Event Handlers (Continued)
        private void Ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                string msg = e.Message;
                if (string.IsNullOrEmpty(msg))
                    return;

                char type = msg[0];

                switch (type)
                {
                    case '0': // handshake
                        _ws.Send("40");
                        break;

                    case '2': // ping
                        _ws.Send("3");
                        break;

                    case '4': // event payload
                        string frame = msg.Substring(1);

                        if (frame.StartsWith("0"))
                        {
                            JObject obj = JObject.Parse(frame.Substring(1));
                            _socketIoSid = obj["sid"]?.ToString();
                            _namespaceOpened = true;

                            SendInitialEmits();
                        }
                        else if (frame.StartsWith("2"))
                        {
                            HandleEvent(frame.Substring(1));
                        }
                        break;
                }
            }
            catch
            {
                // Swallow exceptions; malformed messages should not crash the app.
            }
        }
        #endregion

        #region Event Routing
        private void HandleEvent(string json)
        {
            try
            {
                JArray arr = JArray.Parse(json);
                string eventName = arr[0].ToString();
                JToken payload = arr[1];

                if (eventName == "ON_COMMON_ROOM")
                {
                    HandleCommonRoom(payload);
                }
                else if (eventName == "site_room")
                {
                    HandleSiteRoom(payload);
                }
            }
            catch { }
        }
        #endregion

        #region Common Room Handling
        private void HandleCommonRoom(JToken payload)
        {
            try
            {
                JObject data = (JObject)payload;
                string type = data["Type"]?.ToString();
                JToken playLoad = data["Data"];

                switch (type)
                {
                    case "CLIENT_UPDATE":
                        _socketData.Balance.balance = (decimal)playLoad["balance"];
                        _socketData.Balance.creditAmount = (decimal)playLoad["creditAmount"];
                        _socketData.Balance.pnl = (decimal)playLoad["pnl"];
                        _socketData.Balance.freeMarginAmount = (decimal)playLoad["freeMarginAmount"];
                        _socketData.Balance.occupiedMarginAmount = (decimal)playLoad["occupiedMarginAmount"];

                        _socketData.Balance.uplineAmount = (decimal)(playLoad["uplineAmount"] ?? 0);
                        _socketData.Balance.uplineCommission = (decimal)(playLoad["uplineCommission"] ?? 0);
                        _socketData.Balance.floatingPLAmount = (decimal)(playLoad["floatingPLAmount"] ?? 0);
                        bool isViewLocked = (bool)(playLoad["isViewLocked"] ?? false);
                        OnViewLockChanged?.Invoke(isViewLocked);
                        break;

                    case "SYMBOL_SPREAD_BALANCE_UPDATE":
                        int symbolId = (int)playLoad["symbolId"];
                        decimal newSpread = (decimal)playLoad["spreadBalance"];
                        var symbol = _socketData.Symbols.FirstOrDefault(s => s.symbolId == symbolId);
                        if (symbol != null) symbol.spreadBalance = newSpread;
                        break;

                    case "CLIENT_SYMBOL_DEACTIVATE":
                    case "SYMBOL_DELETE":
                        int symId = (int)playLoad["symbolId"];
                        _socketData.Symbols.RemoveAll(s => s.symbolId == symId);
                        break;

                    case "SECURITY_UPDATE":
                        int securityId = (int)playLoad["securityId"];
                        bool status = (bool)playLoad["securityStatus"];
                        foreach (var sym in _socketData.Symbols.Where(s => s.securityId == securityId))
                            sym.securityStatus = status;
                        break;

                    case "BAN_SCRIPT_ADD":
                        string masterNameAdd = playLoad["masterSymbolName"]?.ToString();
                        _socketData.BanScripts.Add(new Skt_BanScript { masterSymbolName = masterNameAdd });
                        break;

                    case "BAN_SCRIPT_DELETE":
                        string masterNameDel = playLoad["masterSymbolName"]?.ToString();
                        _socketData.BanScripts.RemoveAll(b => b.masterSymbolName == masterNameDel);
                        break;

                    case "REPLY_FEEDBACK":
                        var feedbackReply = playLoad?.ToObject<FeedbackReplyData>();
                        if (feedbackReply != null)
                            OnFeedbackReplyReceived?.Invoke(feedbackReply);
                        break;

                    case "FORCE_LOGOUT":
                    case "FORCE_LOGOUT_ONLINE":
                        var userId = data["Message"]?.ToString();
                        OnForceLogout?.Invoke(userId);
                        break;
                }
            }
            catch { }
        }
        #endregion

        #region Site Room Handling
        private void HandleSiteRoom(JToken payload)
        {
            try
            {
                string jsonString = payload.ToString();
                if (jsonString.StartsWith("\""))
                    jsonString = JsonConvert.DeserializeObject<string>(jsonString);

                JObject tradeData = JObject.Parse(jsonString);
                string command = tradeData["command"]?.ToString();
                JObject play = (JObject)tradeData["payload"];

                string username = play["username"]?.ToString();
                if (!string.IsNullOrEmpty(username) && username != _sessionService.socketLoginInfos.UserSubId)
                {
                    return;
                }

                switch (command)
                {
                    case "TRADE.POSITION":
                        HandlePosition(play);
                        break;
                    case "PENDING.ORDER":
                        HandlePendingOrder(play);
                        break;
                    case "LIMIT.PASSED":
                        HandleLimitPassed(play);
                        break;
                    case "DEAL.MESSAGE":
                        HandleDeal(play);
                        break;
                    case "USER.BALANCE":
                        HandleBalance(play);
                        break;
                }
            }
            catch { }
        }
        #endregion

        #region Position & Order Handling
        private void HandlePosition(JObject play)
        {
            decimal totalVolume = (decimal)(play["totalVolume"] ?? 0);
            var position = new Position
            {
                Id = play["id"]?.ToString(),
                SymbolName = play["symbolName"]?.ToString(),
                CreatedAt = play["createdAt"]?.ToObject<DateTime?>(),
                LastInAt = play["lastInAt"]?.ToObject<DateTime?>(),
                UpdatedAt = play["updatedAt"]?.ToObject<DateTime?>(),
                Side = play["side"]?.ToString(),
                Status = play["status"]?.ToString(),
                TotalVolume = (double)(play["totalVolume"] ?? 0),
                SpreadValue = (double)(play["spreadValue"] ?? 0),
                SpreadBalance = (double)(play["spreadBalance"] ?? 0),
                SymbolContractSize = (double)(play["symbolContractSize"] ?? 0),
                SpreadType = play["spreadType"]?.ToString(),
                AveragePrice = (double)(play["averagePrice"] ?? 0),
                CurrentPrice = (double)(play["currentPrice"] ?? 0),
                Pnl = (decimal?)(play["pnl"] ?? 0),
                Comment = play["comment"]?.ToString(),
                SymbolDigit = play["symbolDigit"] != null ? (int)play["symbolDigit"] : 0,
            };

            OnPositionUpdated?.Invoke(position, totalVolume <= 0);
        }

        private void HandlePendingOrder(JObject play)
        {
            string orderId = play["orderId"]?.ToString();
            string status = play["orderFulfillment"]?.ToString();
            double volume = (double)(play["volume"] ?? 0);

            var order = new OrderModel
            {
                OrderId = play["orderId"]?.ToString(),
                Device = play["device"]?.ToString(),

                SymbolId = play["symbolId"] != null ? (int)play["symbolId"] : 0,
                SymbolName = play["symbolName"]?.ToString(),
                SecurityId = play["securityId"] != null ? (int)play["securityId"] : 0,
                SymbolDigit = play["symbolDigit"] != null ? (int)play["symbolDigit"] : 0,

                Side = play["side"]?.ToString(),

                SymbolExpiry = play["symbolExpiry"]?.Type == JTokenType.Null ? null : (DateTime?)play["symbolExpiry"],
                SymbolExpiryClose = play["symbolExpiryClose"]?.Type == JTokenType.Null ? null : (DateTime?)play["symbolExpiryClose"],

                SymbolContractSize = play["symbolContractSize"] != null ? (double)play["symbolContractSize"] : 0,

                CurrentPrice = play["currentPrice"] != null ? (double)play["currentPrice"] : 0,
                Reason = play["reason"]?.ToString(),
                ClientIp = play["clientIp"]?.ToString(),

                Margin = play["margin"] != null ? (decimal)play["margin"] : 0,
                Price = play["price"] != null ? (double)play["price"] : 0,
                Volume = play["volume"] != null ? (double)play["volume"] : 0,

                ParentSharing = play["parentSharing"] != null
        ? play["parentSharing"].ToObject<List<OrderParentSharing>>()
        : new List<OrderParentSharing>(),

                // 🔥 IMPORTANT (UTC → IST)
                CreatedAt = play["createdAt"] != null
        ? CommonHelper.ConvertUtcToIst((DateTime)play["createdAt"])
        : DateTime.MinValue,

                UpdatedAt = play["updatedAt"] != null
        ? CommonHelper.ConvertUtcToIst((DateTime)play["updatedAt"])
        : DateTime.MinValue,

                MasterSymbolName = play["masterSymbolName"]?.ToString(),
                OrderType = play["orderType"]?.ToString(),
                MarginType = play["marginType"]?.ToString(),
                OrderFulfillment = play["orderFulfillment"]?.ToString(),
                Comment = play["comment"]?.ToString(),

                SecurityName = play["securityName"]?.ToString(),
                SymbolDetail = play["symbolDetail"]?.ToString(),

                SpreadType = play["spreadType"]?.ToString(),
                SpreadValue = play["spreadValue"] != null ? (double)play["spreadValue"] : 0,
                SpreadBalance = play["spreadBalance"] != null ? (double)play["spreadBalance"] : 0,

                OperatorId = play["operatorId"]?.ToString(),
                UserName = play["username"]?.ToString(), 
                UserId = play["userId"]?.ToString()
            };

            if (status == "CANCELLED" || volume <= 0)
            {
                OnOrderUpdated?.Invoke(order, true, orderId);
            }
            else
            {
                OnOrderUpdated?.Invoke(order, false, orderId);
            }
        }

        private void HandleLimitPassed(JObject play)
        {
            string orderId = play["orderId"]?.ToString();
            OnOrderUpdated?.Invoke(null, true, orderId);
        }

        private void HandleDeal(JObject play)
        {
            var deal = new Skt_Deal
            {
                orderId = play["orderId"]?.ToString(),
                symbolName = play["symbolName"]?.ToString(),
                volume = (decimal)(play["volume"] ?? 0),
                pnl = (decimal)(play["pnl"] ?? 0),
                closeTime = DateTime.Now,
                type = play["orderType"]?.ToString()
            };
            _socketData.HistoryDeals.Add(deal);
        }

        private void HandleBalance(JObject play)
        {
            var client = new ClientDetails
            {
                CreditAmount = (double)(play["creditAmount"] ?? 0),
                UplineAmount = (double)(play["uplineAmount"] ?? 0),
                Balance = (double)(play["balance"] ?? 0),
                OccupiedMarginAmount = (double)(play["occupiedMargin"] ?? 0),
                UplineCommission = (double)(play["uplineCommission"] ?? 0)
            };
            OnUpdateUserBalance?.Invoke(client);
        }
        #endregion

        #region Emit Helpers
        private void SendInitialEmits()
        {
            if (!_namespaceOpened) return;

            Emit("disconnect_client", _socketIoSid);
            Thread.Sleep(100);

            Emit("Online_Dealers_Client", new
            {
                userId = _sessionService.socketLoginInfos.UserSubId,
                ip = _sessionService.socketLoginInfos.IpAddress,
                device = _sessionService.socketLoginInfos.Device,
                DealerId = _sessionService.socketLoginInfos.Intime,
                role = _sessionService.socketLoginInfos.Role,
                time = DateTime.UtcNow.ToString("o"),
                isreadonlypassword = "false"
            });
            Thread.Sleep(100);

            Emit("user_room", $"{_sessionService.socketLoginInfos.UserIss}:{_sessionService.socketLoginInfos.UserSubId}");
            Thread.Sleep(100);
            Emit("site_room", _sessionService.socketLoginInfos.UserSubId);
            Thread.Sleep(100);
            Emit("Notification", "EMIT_COMMON_NOTIFICATION");
            Thread.Sleep(100);
            Emit("Notification", $"EMIT_OPERATOR_{_sessionService.socketLoginInfos.OperatorId}");
        }

        private void Emit(string eventName, object payload)
        {
            if (!_namespaceOpened || _ws == null || _ws.State != WebSocketState.Open) return;

            JArray arr = new JArray { eventName };
            if (payload != null) arr.Add(JToken.FromObject(payload));

            string frame = "42" + arr.ToString(Formatting.None);
            _ws.Send(frame);
        }
        #endregion
    }
}
