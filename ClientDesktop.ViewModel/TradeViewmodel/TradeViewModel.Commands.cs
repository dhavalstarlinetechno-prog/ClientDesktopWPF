using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public partial class TradeViewModel : ViewModelBase
    {
        #region 4. Commands
        public ICommand ChangeOrderTypeCommand => new RelayCommand(param =>
        {
            if (param is string typeString && Enum.TryParse<EnumTradeOrderType>(typeString, true, out var newType))
            {
                SetOrderType(newType);
            }
        });

        // Trade Action Commands
        public ICommand BuyCommand => new RelayCommand(async _ => await ExecuteTradeOperation("BUY"));
        public ICommand SellCommand => new RelayCommand(async _ => await ExecuteTradeOperation("SELL"));
        public ICommand ClosePositionCommand => new RelayCommand(async _ => await ExecuteTradeOperation("CLOSE"));
        public ICommand OkCommand => new RelayCommand(_ => ResetTradeWindow());
        #endregion

        #region 7. Trade Execution Logic
        private async Task ExecuteTradeOperation(string actionType)
        {
            IsProcessingOrDone = true;
            TradeResultMessage = "Processing Order...";

            await Task.Delay(1000);

            string currentPrice = actionType == "BUY" ? LiveAsk : LiveBid; 

            if (actionType == "CLOSE")
            {
                TradeResultMessage = $"Position Closed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}";
            }
            else
            {
                string displayAction = actionType == "BUY" ? BuyButtonText : SellButtonText;
                TradeResultMessage = $"{displayAction} Order placed successfully!\nSymbol: {SelectedSymbol}\nPrice: {currentPrice}";
            }
        }

        private void ResetTradeWindow()
        {
            IsProcessingOrDone = false;
        }
        #endregion
    }
}