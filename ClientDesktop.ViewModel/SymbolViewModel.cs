using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class SymbolViewModel : ViewModelBase
    {
        #region Variables/Properties

        private readonly SessionService _sessionService;
        private readonly SymbolService _symbolService;
        public new Action? CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        private string? _symbolName;
        public string? SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        private ObservableCollection<Folder> _folders = new ObservableCollection<Folder>();
        public ObservableCollection<Folder> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                OnPropertyChanged(nameof(Folders));
            }
        }

        private ObservableCollection<SubSymbolModel> _subSymbols = new ObservableCollection<SubSymbolModel>();

        private ObservableCollection<SymbolModel> _symbolData = new ObservableCollection<SymbolModel>();

        public ObservableCollection<SubSymbolModel> SubSymbols
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(SubSymbols)); }
        }
        public ObservableCollection<SubSymbolModel> Loadsymbolsbyroute
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(Loadsymbolsbyroute)); }
        }
        public ObservableCollection<SubSymbolModel> Loaddolorsymbols
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(Loaddolorsymbols)); }
        }

        public ObservableCollection<SymbolModel> SymbolData
        {
            get => _symbolData;
            set { _symbolData = value; OnPropertyChanged(nameof(SymbolData)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        #endregion Variables/Properties

        #region Constructor
        public SymbolViewModel(SessionService sessionService, SymbolService symbolService)
        {
            _sessionService = sessionService;
            _symbolService = symbolService;
            Folders = new ObservableCollection<Folder>();
            SubSymbols = new ObservableCollection<SubSymbolModel>();
            SymbolData = new ObservableCollection<SymbolModel>();
            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        #endregion Constructor

        #region Methods
        public async Task<string?> LoadSymbolsAsync()
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return null;

                IsBusy = true;    
                var result = await _symbolService.GetSymbolsAsync();

                Folders.Clear();
                if (result != null && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        Folders.Add(item);
                    }
                }                
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {              
                FileLogger.ApplicationLog(nameof(LoadSymbolsAsync), $"Error loading symbols: {ex.Message}");
                return string.Empty; 
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string?> LoadSubSymbolsAsync()
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return null;

                var result = await _symbolService.GetSubSymbolsAsync();
                SubSymbols.Clear();
                if (result?.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        SubSymbols.Add(item);
                    }
                }
                
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadSubSymbolsAsync), $"Error loading sub-symbols: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string?> Loadsymbolsbyrouteforclient(int routeId)
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return null;

                IsBusy = true;
                
                var result = await _symbolService.Getsymbolsbyrouteforclient(routeId);

                var newItems = result?.Data?.ToList() ?? new List<SubSymbolModel>();

                Loadsymbolsbyroute.Clear();

                foreach (var item in newItems)
                {
                    Loadsymbolsbyroute.Add(item);
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadSubSymbolsAsync), "Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string?> LoadDolorSignTree(string symbolId)
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return null;

                IsBusy = true;
                
                var result = await _symbolService.GetDolorSignTree(symbolId);
                
                Loaddolorsymbols.Clear();

                if (result != null && result.Data != null)
                {                   
                    foreach (var item in result.Data)
                    {
                        Loaddolorsymbols.Add(item);
                    }
                }
                
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadDolorSignTree),"Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string?> LoadSymbolDetailsAsync(int symbolId)
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return null;

                IsBusy = true;
                
                var result = await _symbolService.GetSymbolDetailsAsync(symbolId);

                if (result != null && result != null)
                {
                    SymbolData.Clear();
                    SymbolData.Add(result);
                }
                
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadSymbolDetailsAsync),"Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion Methods
    }
}
