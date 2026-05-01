using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class SymbolSpecificationViewModel : ViewModelBase
    {
        #region Variables / Properties

        public readonly SessionService _sessionService;
        private readonly SymbolSpecificationService _symbolSpecificationService;       
        public new Action? CloseAction { get; set; }
       
        public ICommand CloseCommand { get; }

        private int _symbolId;
        public int SymbolId
        {
            get => _symbolId;
            set => SetProperty(ref _symbolId, value);
        }

        private string? _symbolName;
        public string? SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        private SymbolModel? _symbolData;
        public SymbolModel? SymbolData
        {
            get => _symbolData;
            set => SetProperty(ref _symbolData, value);
        }

        #endregion Variables / Properties

        #region Constructor
        public SymbolSpecificationViewModel(SessionService sessionService, SymbolSpecificationService symbolSpecificationService)
        {
            _sessionService = sessionService;
            _symbolSpecificationService = symbolSpecificationService;
            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        #endregion Constructor

        #region Method

        public async Task LoadSymbolData()
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return;

                if (SymbolId <= 0)
                    return;

                var response = await _symbolSpecificationService.GetSymbolAsync(SymbolId);

                if (response == null)
                    return;

                SymbolData = response;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadSymbolData), $"Error occurred: {ex.Message}");
            }
            
        }

        #endregion Method
    }
}
