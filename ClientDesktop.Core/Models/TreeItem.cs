using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Core.Models
{
    public class TreeItem
    {
        public string Title { get; set; }
        public string IconData { get; set; }
        public string IconColor { get; set; }
        public string TargetPage { get; set; }
        public ObservableCollection<TreeItem> Children { get; set; }

        public TreeItem()
        {
            Children = new ObservableCollection<TreeItem>();
        }
    }
}
