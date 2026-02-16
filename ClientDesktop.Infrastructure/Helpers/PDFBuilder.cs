using DocumentFormat.OpenXml.Office2010.PowerPoint;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ClientDesktop.Infrastructure.Helpers
{
    public class PDFBuilder
    {
        public void ExportToPdf<T>(List<T> data, string title, string[] columnHeaders, Func<T, object[]> rowDataSelector)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    Title = "Export to PDF",
                    FileName = $"{title}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                Document document = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(saveDialog.FileName, FileMode.Create));
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.BLACK);
                Paragraph titlePara = new Paragraph(title.ToUpper(), titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(titlePara);

                // Create table
                PdfPTable pdfTable = new PdfPTable(columnHeaders.Length)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10,
                    SpacingAfter = 10
                };

                // Headers
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
                foreach (string header in columnHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    pdfTable.AddCell(cell);
                }

                // Data rows
                Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

                foreach (var item in data)
                {
                    object[] rowData = rowDataSelector(item);
                    foreach (var cellValue in rowData)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(cellValue?.ToString() ?? "", dataFont))
                        {
                            Padding = 4
                        };
                        pdfTable.AddCell(cell);
                    }
                }

                document.Add(pdfTable);
                document.Close();

                MessageBox.Show("PDF exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
