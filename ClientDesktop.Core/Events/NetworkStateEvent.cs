namespace ClientDesktop.Core.Events
{
    /// <summary>
    /// Signal broadcasted when the OS network availability changes (e.g., Wi-Fi disconnects/reconnects).
    /// </summary>
    public class NetworkStateEvent
    {
        public bool IsConnected { get; }

        public NetworkStateEvent(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }
}