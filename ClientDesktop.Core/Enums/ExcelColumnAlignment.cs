using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Core.Enums
{
    /// <summary>
    /// Excel column text alignment.
    /// Core layer ka apna enum — ClosedXML ka koi reference nahi.
    /// IPdfService ki tarah IPdfService bhi isi pattern ko follow karta hai.
    /// </summary>
    public enum ExcelColumnAlignment
    {
        Left,
        Center,
        Right
    }
}
