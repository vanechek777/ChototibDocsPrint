using System.Drawing.Printing;
using System.IO;
using PdfiumViewer;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Отправка PDF на выбранный принтер Windows.</summary>
public static class PdfPrintService
{
    public static string ResolveInstalledPrinterName(string printerFromDialog)
    {
        if (string.IsNullOrWhiteSpace(printerFromDialog))
            throw new ArgumentException("Не указан принтер.", nameof(printerFromDialog));

        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            if (string.Equals(name, printerFromDialog, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            if (printerFromDialog.EndsWith('\\' + name, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(printerFromDialog, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        throw new InvalidOperationException(
            $"Принтер «{printerFromDialog}» не найден среди установленных в Windows.");
    }

    public static void Print(byte[] pdfBytes, string printerName, string documentName)
    {
        if (pdfBytes.Length == 0)
            throw new InvalidOperationException("PDF пустой.");

        printerName = ResolveInstalledPrinterName(printerName);

        var tempPath = Path.Combine(Path.GetTempPath(), $"cht_print_{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(tempPath, pdfBytes);
            using var document = PdfDocument.Load(tempPath);
            using var printDocument = document.CreatePrintDocument();
            printDocument.DocumentName = string.IsNullOrWhiteSpace(documentName) ? "ChtotibDocsPrint" : documentName;
            printDocument.PrinterSettings.PrinterName = printerName;
            printDocument.PrintController = new StandardPrintController();
            printDocument.Print();
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // временный файл может остаться — не критично
            }
        }
    }
}
