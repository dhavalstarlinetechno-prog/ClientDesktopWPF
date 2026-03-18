using ClientDesktop.Core.Models;
using System.Threading.Tasks;

namespace ClientDesktop.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for all trade-related API and data operations.
    /// </summary>
    public interface ITradeService
    {
        Task<MarketWatchData> GetMarketWatchDataAsync();

        Task<(bool Success, string ErrorMessage, SymbolData SymbolData)> GetSymbolDataAsync(string clientId, int symbolId);

        Task<bool> DeleteOrderAsync(string orderId);

        Task<(bool Success, string ErrorMessage, string TradeMessage)> PlaceOrModifyOrderAsync(object payload, bool isModify = false);
    }
}