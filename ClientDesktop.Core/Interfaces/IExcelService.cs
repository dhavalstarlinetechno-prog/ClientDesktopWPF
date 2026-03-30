using ClientDesktop.Core.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Core.Interfaces
{
    public interface IExcelService
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when Excel is saved successfully. Arg = full file path.</summary>
        event Action<string> OnExcelSaved;

        /// <summary>Fired when an error occurs during Excel generation.</summary>
        event Action<string> OnError;

        // ── Fluent Builder Methods ────────────────────────────────────────────────

        /// <summary>
        /// Add a DataTable as a sheet with a title row and column headers.
        /// </summary>
        /// <param name="dataTable">Data to export.</param>
        /// <param name="title">Title shown in the first row (bold, merged, centered).</param>
        /// <param name="sheetName">Excel sheet name (default: Sheet1).</param>
        /// <param name="columnAlignments">Optional per-column alignment override.</param>
        IExcelService AddSheet(
            DataTable dataTable,
            string title,
            string sheetName = "Sheet1",
            Dictionary<string, EnumExcelColumnAlignment> columnAlignments = null);

        /// <summary>
        /// Save the Excel file. Opens WPF SaveFileDialog.
        /// Fires OnExcelSaved or OnError — no MessageBox inside.
        /// </summary>
        /// <param name="baseFileName">Default file name (without extension).</param>
        void SaveExcel(string baseFileName);

        /// <summary>
        /// Returns raw Excel bytes — useful for email attachment.
        /// </summary>
        byte[] GenerateExcelBytes();

        /// <summary>Clear all queued sheets (reuse across screens).</summary>
        IExcelService Clear();
    }
}
