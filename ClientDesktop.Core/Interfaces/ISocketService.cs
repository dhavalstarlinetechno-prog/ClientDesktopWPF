using ClientDesktop.Core.Models;
using ClientDesktop.Models;
using System;

namespace ClientDesktop.Services
{
    public interface ISocketService
    {
        bool IsConnected { get; }

        void Start();
        void Stop();

        event Action<Position, bool> OnPositionUpdated;
        event Action OnSocketReconnected;
        event Action<OrderModel, bool, string> OnOrderUpdated;
        event Action<string> OnForceLogout;
        event Action<ClientDetails> OnUpdateUserBalance;
    }
}