using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.ViewModel
{

    public class HistoryViewModel
    {
        private readonly HistoryService _historyService;
        public Action OnHistoryDataLoaded { get; set; }
        public HistoryViewModel()
        {
            _historyService = new HistoryService();

            SessionManager.OnLoginSuccess += HandleLogin;
        }

        private void HandleLogin()
        {
            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var fromDate = new DateTime(1970, 1, 1);
                var toDate = DateTime.Now;

                // Run both APIs parallelly ⚡
                var historyTask = _historyService.FetchHistoryFromApiAsync(fromDate, toDate);
                var positionHistoryTask = _historyService.FetchPositionHistoryFromApiAsync(fromDate, toDate);

                // Await both
                var historyResult = await historyTask;
                var positionResult = await positionHistoryTask;

                OnHistoryDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        public List<HistoryModel> GetHistoryData() => _historyService.GetStoredHistory();

        public List<PositionHistoryModel> GetPositionHistoryData() => _historyService.GetStoredPositionHistory();

    }
}
