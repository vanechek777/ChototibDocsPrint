using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace ChtotibDocsPrintNET.Services;

public class DiplomaPrintService
{
    private const float DiplomaWidth_mm = 297f;
    private const float DiplomaHeight_mm = 210f;

    /// <summary>Страницы приложения в PDF — A5 альбомная ориентация (как типовой разворот приложения).</summary>
    private const float AppendixWidth_mm = 210f;
    private const float AppendixHeight_mm = 148f;

    private const float MmToPoints = 2.8346f;

    private static bool IsAppendixDocumentType(string documentType) =>
        documentType.Contains("Приложение", StringComparison.Ordinal);

    private static void GetPdfPageSizeMm(string documentType, out float widthMm, out float heightMm)
    {
        if (IsAppendixDocumentType(documentType))
        {
            widthMm = AppendixWidth_mm;
            heightMm = AppendixHeight_mm;
        }
        else
        {
            widthMm = DiplomaWidth_mm;
            heightMm = DiplomaHeight_mm;
        }
    }

    static DiplomaPrintService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateDiplomaPdf(
        Student student, Diploma? diploma, List<PrintSetting> settings,
        string documentType, DateTime issueDate, string? chairmanName,
        PrintDocumentOptions? options = null)
    {
        var grades = student.Id > 0
            ? DatabaseService.Instance.GetStudentGrades(student.Id)
            : null;
        var useHonor = options?.UseHonorTemplatesWhenApplicable == true
            && StudentHonorEvaluator.ShouldUseHonorTemplate(diploma?.DiplomaType, grades);
        var templateBytes = options?.DrawBackground == false
            ? null
            : TryLoadTemplateBytes(documentType, useHonor);
        GetPdfPageSizeMm(documentType, out var pageWmm, out var pageHmm);
        var svg = BuildSvg(student, diploma, settings, issueDate, chairmanName, pageWmm, pageHmm);
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageWmm, pageHmm, Unit.Millimetre);
                page.Margin(0);
                page.Content().Layers(layers =>
                {
                    // QuestPDF требует ровно один PrimaryLayer
                    if (templateBytes != null)
                    {
                        layers.PrimaryLayer().Image(templateBytes).FitUnproportionally();
                        layers.Layer().Svg(svg);
                    }
                    else
                    {
                        layers.PrimaryLayer().Svg(svg);
                    }
                });
            });
        });
        return doc.GeneratePdf();
    }

    public byte[] GenerateBatchPdf(
        List<Student> students, List<PrintSetting> settings,
        string documentType, DateTime issueDate, string? chairmanName,
        PrintDocumentOptions? options = null,
        IProgress<PdfGenerationProgress>? progress = null)
    {
        var templateBytes = options?.DrawBackground == false
            ? null
            : TryLoadTemplateBytes(documentType);
        GetPdfPageSizeMm(documentType, out var pageWmm, out var pageHmm);
        var total = students.Count;
        var doc = Document.Create(container =>
        {
            var index = 0;
            foreach (var student in students)
            {
                index++;
                progress?.Report(new PdfGenerationProgress(index, total,
                    $"Студент {index} из {total}: {student.FullName}"));
                var svg = BuildSvg(student, null, settings, issueDate, chairmanName, pageWmm, pageHmm);
                container.Page(page =>
                {
                    page.Size(pageWmm, pageHmm, Unit.Millimetre);
                    page.Margin(0);
                    page.Content().Layers(layers =>
                    {
                        if (templateBytes != null)
                        {
                            layers.PrimaryLayer().Image(templateBytes).FitUnproportionally();
                            layers.Layer().Svg(svg);
                        }
                        else
                        {
                            layers.PrimaryLayer().Svg(svg);
                        }
                    });
                });
            }
        });
        return doc.GeneratePdf();
    }

    /// <summary>Масштаб растрового PNG перед вставкой в PDF (чёткость текста).</summary>
    private const float DiplomaPngRasterScale = 2.75f;

    public byte[] GenerateBatchDiplomaBackPdf(
        IReadOnlyList<StudentPrintRow> rows,
        DiplomaBackOverlayLayout layoutFallback,
        string orgName,
        string specialtyCodeName,
        string? chairmanShortName,
        string? directorShortName,
        string? lazurskiTtfPath,
        string? specialtyQualificationFallback,
        PreviewCropPersisted? previewCropFallback,
        PrintDocumentOptions options,
        IProgress<PdfGenerationProgress>? progress = null)
    {
        var total = rows.Count;
        var doc = Document.Create(container =>
        {
            var index = 0;
            foreach (var row in rows)
            {
                index++;
                progress?.Report(new PdfGenerationProgress(index, total,
                    $"Студент {index} из {total}: {row.Student.FullName}"));
                var honor = row.UseHonorTemplate && options.UseHonorTemplatesWhenApplicable;
                var templateBytes = DocumentTemplateService.LoadTemplateBytes("Диплом (обратная сторона)", honor);
                var backLayout = row.DiplomaBackLayout ?? layoutFallback;
                var crop = row.CropDiplomaBack ?? previewCropFallback;
                var png = DiplomaPdfSkiaCompositor.ComposeDiplomaBackPng(
                    templateBytes, row.Student, backLayout, orgName, specialtyCodeName, row.GecDecisionDateLine, row.IssueDate,
                    chairmanShortName, directorShortName, lazurskiTtfPath, specialtyQualificationFallback,
                    DiplomaPngRasterScale,
                    crop,
                    row.QrPayload,
                    options);

                container.Page(page =>
                {
                    page.Size(DiplomaWidth_mm, DiplomaHeight_mm, Unit.Millimetre);
                    page.Margin(0);
                    page.Content().Image(png).FitUnproportionally();
                });
            }
        });
        return doc.GeneratePdf();
    }

    public byte[] GenerateBatchDiplomaFrontPdf(
        IReadOnlyList<StudentPrintRow> rows,
        string orgName,
        string? specialtyQualificationFallback,
        PreviewCropPersisted? previewCropFallback,
        PrintDocumentOptions options,
        DiplomaFrontOverlayLayout diplomaFrontLayoutFallback,
        IProgress<PdfGenerationProgress>? progress = null)
    {
        var total = rows.Count;
        var doc = Document.Create(container =>
        {
            var index = 0;
            foreach (var row in rows)
            {
                index++;
                progress?.Report(new PdfGenerationProgress(index, total,
                    $"Студент {index} из {total}: {row.Student.FullName}"));
                var honor = row.UseHonorTemplate && options.UseHonorTemplatesWhenApplicable;
                var templateBytes = DocumentTemplateService.LoadTemplateBytes("Диплом (лицевая)", honor);
                var frontLayout = row.DiplomaFrontLayout ?? diplomaFrontLayoutFallback;
                var crop = row.CropDiplomaFront ?? previewCropFallback;
                var png = DiplomaPdfSkiaCompositor.ComposeDiplomaFrontPng(
                    templateBytes, row.Student, row.IssueDate, orgName, specialtyQualificationFallback, DiplomaPngRasterScale,
                    crop,
                    options,
                    frontLayout);

                container.Page(page =>
                {
                    page.Size(DiplomaWidth_mm, DiplomaHeight_mm, Unit.Millimetre);
                    page.Margin(0);
                    page.Content().Image(png).FitUnproportionally();
                });
            }
        });
        return doc.GeneratePdf();
    }

    /// <summary>
    /// Один PDF: для каждого ученика по порядку — диплом (лиц.), диплом (оборот), приложение (лиц.), приложение (оборот).
    /// </summary>
    public byte[] GenerateBatchFullDiplomaPacketPdf(
        IReadOnlyList<FullPacketStudentRow> rows,
        string orgName,
        string specialtyCodeName,
        string? chairmanShortName,
        string? directorShortName,
        string? lazurskiTtfPath,
        string? specialtyQualificationFallback,
        PreviewCropPersisted? previewCropDiplomaFrontFallback,
        PreviewCropPersisted? previewCropDiplomaBackFallback,
        PreviewCropPersisted? previewCropAppendix,
        DiplomaFrontOverlayLayout diplomaFrontLayoutFallback,
        DiplomaBackOverlayLayout diplomaBackLayoutFallback,
        AppendixFrontOverlayLayout appendixFrontLayout,
        AppendixBackOverlayLayout appendixBackLayout,
        bool appendixShowPageNumbers,
        PrintDocumentOptions options,
        string studyPeriod,
        IProgress<PdfGenerationProgress>? progress = null)
    {
        var appFrontTpl = DocumentTemplateService.LoadTemplateBytes("Приложение (лицевая)");
        var appBackTpl = DocumentTemplateService.LoadTemplateBytes("Приложение (оборотная)");
        var total = rows.Count;

        var doc = Document.Create(container =>
        {
            var index = 0;
            foreach (var row in rows)
            {
                index++;
                progress?.Report(new PdfGenerationProgress(index, total,
                    $"Студент {index} из {total}: {row.Student.FullName}"));
                var s = row.Student;
                var honor = row.UseHonorTemplate && options.UseHonorTemplatesWhenApplicable;
                var dipFrontTpl = DocumentTemplateService.LoadTemplateBytes("Диплом (лицевая)", honor);
                var dipBackTpl = DocumentTemplateService.LoadTemplateBytes("Диплом (обратная сторона)", honor);

                var frontLayout = row.DiplomaFrontLayout ?? diplomaFrontLayoutFallback;
                var backLayout = row.DiplomaBackLayout ?? diplomaBackLayoutFallback;
                var cropFront = row.CropDiplomaFront ?? previewCropDiplomaFrontFallback;
                var cropBack = row.CropDiplomaBack ?? previewCropDiplomaBackFallback;

                var pngFront = DiplomaPdfSkiaCompositor.ComposeDiplomaFrontPng(
                    dipFrontTpl, s, row.IssueDate, orgName, specialtyQualificationFallback, DiplomaPngRasterScale,
                    cropFront,
                    options,
                    frontLayout);

                var pngBack = DiplomaPdfSkiaCompositor.ComposeDiplomaBackPng(
                    dipBackTpl, s, backLayout, orgName, specialtyCodeName, row.GecDecisionDateLine, row.IssueDate,
                    chairmanShortName, directorShortName, lazurskiTtfPath, specialtyQualificationFallback,
                    DiplomaPngRasterScale,
                    cropBack,
                    row.QrPayload,
                    options);

                var pngAppF = DiplomaPdfSkiaCompositor.ComposeAppendixFrontPng(
                    appFrontTpl, s, appendixFrontLayout, orgName, row.BirthDateRu, row.PreviousEducation,
                    studyPeriod, specialtyCodeName, row.IssueDateRu, directorShortName ?? "", specialtyQualificationFallback,
                    row.CourseworkGrades, DiplomaPngRasterScale, previewCropAppendix, appendixShowPageNumbers, options,
                    lazurskiTtfPath);

                var pngAppB = DiplomaPdfSkiaCompositor.ComposeAppendixBackPng(
                    appBackTpl, appendixBackLayout, row.SubjectGrades,
                    row.StudyPracticeGrades, row.ProductionPracticeGrades,
                    DiplomaPngRasterScale, previewCropAppendix,
                    appendixShowPageNumbers, options);

                void AddDiplomaPage(byte[] png)
                {
                    container.Page(page =>
                    {
                        page.Size(DiplomaWidth_mm, DiplomaHeight_mm, Unit.Millimetre);
                        page.Margin(0);
                        page.Content().Image(png).FitUnproportionally();
                    });
                }

                void AddAppendixPage(byte[] png)
                {
                    container.Page(page =>
                    {
                        page.Size(AppendixWidth_mm, AppendixHeight_mm, Unit.Millimetre);
                        page.Margin(0);
                        page.Content().Image(png).FitUnproportionally();
                    });
                }

                AddDiplomaPage(pngFront);
                AddDiplomaPage(pngBack);
                AddAppendixPage(pngAppF);
                AddAppendixPage(pngAppB);
            }
        });
        return doc.GeneratePdf();
    }

    private static byte[]? TryLoadTemplateBytes(string documentType, bool useHonor = false) =>
        DocumentTemplateService.LoadTemplateBytes(documentType, useHonor);

    private static string BuildSvg(
        Student student, Diploma? diploma, List<PrintSetting> settings,
        DateTime issueDate, string? chairmanName,
        float pageWidthMm,
        float pageHeightMm)
    {
        var w = pageWidthMm * MmToPoints;
        var h = pageHeightMm * MmToPoints;

        var sb = new StringBuilder(16_384);
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w.ToString(CultureInfo.InvariantCulture)}\" height=\"{h.ToString(CultureInfo.InvariantCulture)}\" viewBox=\"0 0 {w.ToString(CultureInfo.InvariantCulture)} {h.ToString(CultureInfo.InvariantCulture)}\">");

        foreach (var setting in settings.Where(s => s.IsActive))
        {
            var value = GetFieldValue(student, diploma, setting.FieldName, issueDate, chairmanName);
            if (string.IsNullOrEmpty(value)) continue;

            var x = (double)setting.PositionX_mm * MmToPoints;
            var y = (double)setting.PositionY_mm * MmToPoints;
            var fontSize = (double)setting.FontSize;

            var anchor = "start";
            if (string.Equals(setting.TextAlignment, "Center", StringComparison.OrdinalIgnoreCase))
            {
                anchor = "middle";
                var maxW = setting.MaxWidth_mm.HasValue ? (double)setting.MaxWidth_mm.Value * MmToPoints : 0;
                if (maxW > 0)
                    x += maxW * 0.5;
            }
            else if (string.Equals(setting.TextAlignment, "Right", StringComparison.OrdinalIgnoreCase))
            {
                anchor = "end";
            }

            var fontFamily = EscapeXml(setting.FontFamily);
            var style = new StringBuilder(64);
            if (setting.IsBold) style.Append("font-weight:bold;");
            if (setting.IsItalic) style.Append("font-style:italic;");

            sb.Append("<text");
            sb.Append($" x=\"{x.ToString(CultureInfo.InvariantCulture)}\"");
            sb.Append($" y=\"{y.ToString(CultureInfo.InvariantCulture)}\"");
            sb.Append($" font-family=\"{fontFamily}\"");
            sb.Append($" font-size=\"{fontSize.ToString(CultureInfo.InvariantCulture)}\"");
            sb.Append($" text-anchor=\"{anchor}\"");
            sb.Append(" fill=\"#000000\"");
            if (style.Length > 0)
                sb.Append($" style=\"{style}\"");
            sb.Append(">");
            sb.Append(EscapeXml(value));
            sb.Append("</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string GetFieldValue(
        Student student, Diploma? diploma, string fieldName,
        DateTime issueDate, string? chairmanName)
    {
        return fieldName switch
        {
            "ФИО" => student.FullName,
            "Фамилия" => student.LastName,
            "Имя" => student.FirstName,
            "Отчество" => student.MiddleName ?? "",
            "СерияНомер" => diploma != null ? $"{diploma.Series} {diploma.Number}" : "",
            "РегНомер" => student.RegistrationNumber ?? "",
            "ДатаВыдачи" => issueDate.ToString("dd MMMM yyyy г.",
                CultureInfo.GetCultureInfo("ru-RU")),
            "ДатаРождения" => student.BirthDate?.ToString("dd.MM.yyyy") ?? "",
            "ПредседательГЭК" => chairmanName ?? "",
            "Руководитель" => "",
            "ПредыдущийДокумент" => StudentDiplomaPrintHelper.FormatPreviousEducation(student),
            _ => ""
        };
    }

    private static string EscapeXml(string s) => SecurityElement.Escape(s) ?? string.Empty;
}
