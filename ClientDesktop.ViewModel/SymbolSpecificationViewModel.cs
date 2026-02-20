using ClientDesktop.Core.Base;
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
        public Action CloseAction { get; set; }
       
        public ICommand CloseCommand { get; }

        private int _symbolId;
        public int SymbolId
        {
            get => _symbolId;
            set
            {
                SetProperty(ref _symbolId, value);               
                LoadSymbolData();
            }
        }

        private string _symbolName;
        public string SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        public SymbolSpecificationViewModel()
        {
          
            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        private void LoadSymbolData()
        {
            
            Console.WriteLine($"Loading specification for Symbol ID: {_symbolId}, Name: {_symbolName}");
        }

    }
}
