using System.Collections.Generic;
using System.Threading.Tasks;
using ClientDesktop.Core.Models;

namespace ClientDesktop.Core.Interfaces
{
    public interface IChartService
    {
        Task<List<Chartmodel>> GetHistoryAsync(
         string symbol,
         long fromTime,
         long toTime,
         string resolution);
    }
}
