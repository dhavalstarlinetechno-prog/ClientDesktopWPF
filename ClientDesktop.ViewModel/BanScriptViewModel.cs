using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;

namespace ClientDesktop.ViewModel
{
    public class BanScriptViewModel : ViewModelBase, ICloseable
    {
        #region Variables/Properties

        public readonly SessionService _sessionService;
        private readonly BanScriptService _banScriptService;
        public ObservableCollection<BanscriptGridRow> GridRows { get; set; }

        private bool _isDataLoaded;
        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set
            {
                _isDataLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNoDataVisible));
            }
        }

        public bool IsNoDataVisible => IsDataLoaded && (GridRows?.Count ?? 0) == 0;

        #endregion Variables/Properties

        #region Constructor
        public BanScriptViewModel(SessionService sessionService, BanScriptService banScriptService)
        {
            _sessionService = sessionService;
            _banScriptService = banScriptService;
            GridRows = new ObservableCollection<BanscriptGridRow>();
        }

        #endregion Constructor

        #region Method
        public async Task LoadBanScriptData()
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return;

                IsDataLoaded = false;

                GridRows.Clear();

                var banResult = await _banScriptService.GetBanScript();

                var banscriptList = banResult?.BanScripts ?? new List<BanScripts>();

                foreach (var ban in banscriptList)
                {
                    GridRows.Add(new BanscriptGridRow
                    {
                        Symbol = ban.SymbolDisplayName
                    });
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadBanScriptData), "Data Load Error: " + ex.Message);
            }
            finally
            {
                IsDataLoaded = true;
            }
        }

        #endregion Method
    }
}