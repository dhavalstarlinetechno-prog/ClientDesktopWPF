using ClientDesktop.Core.Enums;
using System.Collections.Generic;
using System.Data;

namespace ClientDesktop.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for generating PDF documents across different screens.
    /// ViewModels depend on this interface — zero knowledge of iText7 or PDFBuilder.
    /// </summary>
    public interface IPdfService
    {
        /// <summary>Add a bold uppercase title.</summary>
        IPdfService AddTitle(string title, int fontSize = 18, bool centerAlign = true);

        /// <summary>Add a lighter sub-heading.</summary>
        IPdfService AddSubTitle(string subTitle, int fontSize = 13, bool centerAlign = false);

        /// <summary>
        /// Add a data grid from a DataTable.
        /// columnAlignments: optional per-column alignment — new() { ["Amount"] = PdfColumnAlignment.Right }
        /// footerData: summary key-values printed below the table.
        /// </summary>
        IPdfService AddGrid(
            DataTable dataTable,
            string? gridTitle = null,
            Dictionary<string, string>? footerData = null,
            Dictionary<string, PdfColumnAlignment>? columnAlignments = null);

        /// <summary>
        /// Add an info/header section — key-value pairs in a grid layout.
        /// Great for Customer details, Invoice meta, Date, etc.
        /// </summary>
        IPdfService AddInfoSection(Dictionary<string, string> data, int columns = 2);

        /// <summary>Add blank vertical spacing.</summary>
        IPdfService AddSpacing(float spacing = 10);

        /// <summary>Add an italic grey note at the bottom.</summary>
        IPdfService AddFooterNote(string note);

        /// <summary>
        /// Opens WPF SaveFileDialog and writes the PDF.
        /// Fires OnPdfSaved or OnError — no MessageBox inside.
        /// </summary>
        void BuildPDF(string baseFileName, bool landscape = true, bool autoFormat = true);

        /// <summary>
        /// Returns raw PDF bytes — useful for preview or email attachment.
        /// </summary>
        byte[] GeneratePdfBytes(bool landscape = true, bool autoFormat = true);

        /// <summary>Clear all queued components (reuse across screens).</summary>
        IPdfService Clear();
    }
}