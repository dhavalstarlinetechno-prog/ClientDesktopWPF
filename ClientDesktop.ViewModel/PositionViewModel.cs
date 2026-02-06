// ViewModels/Details/PositionViewModel.cs
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ClientDesktop.ViewModel
{
    public class PositionViewModel : INotifyPropertyChanged
    {
        private readonly IRepository<List<Position>> _positionRepo;

        // ObservableCollection updates UI automatically when data is added
        public ObservableCollection<Position> Positions { get; set; }

        public PositionViewModel()
        {
            Positions = new ObservableCollection<Position>();
            LoadData();
        }

        public void LoadData()
        {
            Positions.Clear();

            var dummyData = new List<Position>
            {
                new Position { SymbolName = "NIFTY FEB", LastInAt = DateTime.Now.AddMinutes(-10), Side = "Sell", Status = "Market", TotalVolume = 50, AveragePrice = 24990.00, CurrentPrice = 25593.60, Pnl = -181080.00m },
                new Position { SymbolName = "BANKNIFTY", LastInAt = DateTime.Now.AddMinutes(-45), Side = "Buy", Status = "Market", TotalVolume = 25, AveragePrice = 46500.50, CurrentPrice = 46800.00, Pnl = 7500.00m },
                new Position { SymbolName = "RELIANCE", LastInAt = DateTime.Now.AddHours(-2), Side = "Buy", Status = "Limit", TotalVolume = 100, AveragePrice = 2850.00, CurrentPrice = 2865.00, Pnl = 1500.00m },
                new Position { SymbolName = "HDFCBANK", LastInAt = DateTime.Now.AddHours(-4), Side = "Sell", Status = "Market", TotalVolume = 50, AveragePrice = 1450.00, CurrentPrice = 1440.00, Pnl = 500.00m },
                new Position { SymbolName = "TATAMOTORS", LastInAt = DateTime.Now.AddDays(-1), Side = "Buy", Status = "Market", TotalVolume = 200, AveragePrice = 950.00, CurrentPrice = 945.00, Pnl = -1000.00m },
                new Position { SymbolName = "INFY", LastInAt = DateTime.Now.AddMinutes(-5), Side = "Sell", Status = "Market", TotalVolume = 100, AveragePrice = 1600.00, CurrentPrice = 1610.00, Pnl = -1000.00m },
                new Position { SymbolName = "SBIN", LastInAt = DateTime.Now.AddMinutes(-30), Side = "Buy", Status = "Limit", TotalVolume = 300, AveragePrice = 750.00, CurrentPrice = 755.00, Pnl = 1500.00m }
            };

            foreach (var item in dummyData)
            {
                Positions.Add(item);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
