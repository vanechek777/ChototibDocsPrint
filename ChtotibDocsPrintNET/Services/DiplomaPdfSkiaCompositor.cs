using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ChtotibDocsPrintNET.Converters;
using ChtotibDocsPrintNET.Models;
using SkiaSharp;

namespace ChtotibDocsPrintNET.Services;

/// <summary>
/// Рисует фон JPG + текст в том же базисе координат, что и предпросмотр WPF Canvas (730×520 диплом, 780×560 приложение),
/// затом кодирует в PNG для встраивания в QuestPDF (Image.FitArea; при кропе растр снова приводится к размеру страницы, чтобы не было полос).
/// </summary>
internal static class DiplomaPdfSkiaCompositor
{
    internal const double PreviewCanvasW = 730.0;
    internal const double PreviewCanvasH = 520.0;
    internal const double AppendixPreviewCanvasW = 780.0;
    internal const double AppendixPreviewCanvasH = 560.0;

    /// <summary>Размер страницы PDF диплома (A4 landscape) в мм, как у QuestPDF page.Size(mm).</summary>
    private const float DiplomaWidthMm = 297f;
    private const float DiplomaHeightMm = 210f;

    /// <summary>Размер страницы PDF приложения (A5 landscape).</summary>
    private const float AppendixPdfWidthMm = 210f;
    private const float AppendixPdfHeightMm = 148f;

    private const float MmToPoints = 2.8346f;
    private const string QualificationLabelText = "Квалификация";
    /// <summary>Тёмный бронзово-золотой тон заголовка на бланке диплома.</summary>
    private const string QualificationLabelColorHex = "#6E4F2A";

    internal static byte[] ComposeDiplomaBackPng(
        byte[]? templateBytes,
        Student student,
        DiplomaBackOverlayLayout layout,
        string orgName,
        string specialtyCodeName,
        string gecDecisionDateLine,
        DateTime issueDate,
        string? chairmanShortName,
        string? directorShortName,
        string? lazurskiTtfPath,
        string? specialtyQualificationFallback,
        float renderScale,
        PreviewCropPersisted? previewCrop,
        string? diplomaQrPayload,
        PrintDocumentOptions? options = null)
    {
        var (pw, ph, sx, sy) = ResolvePagePixels(renderScale, PreviewCanvasW, PreviewCanvasH, DiplomaWidthMm, DiplomaHeightMm);

        using var bmp = new SKBitmap(pw, ph);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        ClipCanvasToPreviewCrop(canvas, previewCrop, sx, sy, pw, ph, PreviewCanvasW, PreviewCanvasH);

        if (options?.DrawBackground != false)
            DrawTemplate(canvas, pw, sy, PreviewCanvasW, templateBytes);

        using var serif = ResolveSerifFace();
        SKTypeface? lazurskiFile = TryLoadTypefaceFromFile(lazurskiTtfPath);
        var scriptFace = lazurskiFile ?? serif;
        try
        {
        var ds = GetDyn(student, "DiplomaSeries")?.Trim() ?? "";
        var dn = GetDyn(student, "DiplomaNumber")?.Trim() ?? "";
        var blankSeriesNumber = string.Join(" ", new[] { ds, dn }.Where(static x => x.Length > 0));

        var qualificationText = QualificationResolver.Resolve(
            GetDyn(student, "Qualification"),
            specialtyQualificationFallback ?? "");

        DrawBlock(canvas, serif, orgName, layout.OrgLeft, layout.OrgTop, layout.OrgWidth, 9,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

        if (options?.PrintQualificationLabel == true)
        {
            var labelSize = layout.QualLabelFontSize is >= 8 and <= 48 ? layout.QualLabelFontSize : 11;
            DrawBlock(canvas, scriptFace, QualificationLabelText, layout.QualLabelLeft, layout.QualLabelTop,
                layout.QualLabelWidth, labelSize, Hex(QualificationLabelColorHex), LayoutAlign.Center, sx, sy);
        }

        DrawBlock(canvas, serif, qualificationText, layout.QualLeft, layout.QualTop, layout.QualWidth, 11,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

        DrawBlock(canvas, serif, blankSeriesNumber, layout.BlankSeriesNumberLeft, layout.BlankSeriesNumberTop, layout.BlankSeriesNumberWidth, 12,
            Hex("#B71C1C"), LayoutAlign.Center, sx, sy, bold: true);

        DrawBlock(canvas, serif, student.RegistrationNumber ?? "", layout.RegLeft, layout.RegTop, layout.RegWidth, 10,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, italic: true);

        var issueRu = issueDate.ToString("dd MMMM yyyy г.", CultureInfo.GetCultureInfo("ru-RU"));
        DrawBlock(canvas, serif, issueRu, layout.IssueLeft, layout.IssueTop, layout.IssueWidth, 10,
            Hex("#8B0000"), LayoutAlign.Center, sx, sy, italic: true);

        DrawBlock(canvas, serif, student.LastName ?? "", layout.LastnameLeft, layout.LastnameTop, layout.LastnameWidth, 18,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);

        var given = string.Join(' ', new[] { student.FirstName, student.MiddleName ?? "" }.Where(static x => !string.IsNullOrWhiteSpace(x)));
        DrawBlock(canvas, serif, given, layout.FirstnameLeft, layout.FirstnameTop, layout.FirstnameWidth, 15,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);

        DrawBlock(canvas, serif, specialtyCodeName, layout.SpecialtyLeft, layout.SpecialtyTop, layout.SpecialtyWidth, 9,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

        DrawBlock(canvas, serif, gecDecisionDateLine, layout.GecDecisionLeft, layout.GecDecisionTop, layout.GecDecisionWidth, 9,
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

        DrawBlock(canvas, serif, chairmanShortName ?? "", layout.ChairmanLeft, layout.ChairmanTop, layout.ChairmanWidth, 10,
            Hex("#1A2E4A"), LayoutAlign.Right, sx, sy);

        DrawBlock(canvas, serif, directorShortName ?? "", layout.DirectorLeft, layout.DirectorTop, layout.DirectorWidth, 10,
            Hex("#1A2E4A"), LayoutAlign.Right, sx, sy);

        DrawDiplomaBackQr(canvas, layout, diplomaQrPayload, sx, sy);

        if (options?.PrintDuplicate == true)
            DrawDuplicateLabel(canvas, layout, sx, sy);

        return EncodePngWithPreviewCrop(bmp, previewCrop, sx, sy, PreviewCanvasW, PreviewCanvasH);
        }
        finally
        {
            lazurskiFile?.Dispose();
        }
    }

    internal static byte[] ComposeDiplomaFrontPng(
        byte[]? templateBytes,
        Student student,
        DateTime issueDate,
        string orgName,
        string? specialtyQualificationFallback,
        float renderScale,
        PreviewCropPersisted? previewCrop,
        PrintDocumentOptions? options = null,
        DiplomaFrontOverlayLayout? duplicateLayout = null)
    {
        var (pw, ph, sx, sy) = ResolvePagePixels(renderScale, PreviewCanvasW, PreviewCanvasH, DiplomaWidthMm, DiplomaHeightMm);

        using var bmp = new SKBitmap(pw, ph);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        ClipCanvasToPreviewCrop(canvas, previewCrop, sx, sy, pw, ph, PreviewCanvasW, PreviewCanvasH);

        if (options?.DrawBackground != false)
            DrawTemplate(canvas, pw, sy, PreviewCanvasW, templateBytes);

        if (options?.PrintDuplicate == true && duplicateLayout != null)
            DrawDuplicateLabel(canvas, duplicateLayout, sx, sy);

        return EncodePngWithPreviewCrop(bmp, previewCrop, sx, sy, PreviewCanvasW, PreviewCanvasH);
    }

    internal static byte[] ComposeAppendixFrontPng(
        byte[]? templateBytes,
        Student student,
        AppendixFrontOverlayLayout layout,
        string orgName,
        string birthDateRu,
        string previousEducation,
        string studyPeriod,
        string specialtyCodeName,
        string issueDateRu,
        string directorShortName,
        string? specialtyQualificationFallback,
        IReadOnlyList<Grade> coursework,
        float renderScale,
        PreviewCropPersisted? previewCrop,
        bool showPageNumbers,
        PrintDocumentOptions? options = null,
        string? lazurskiTtfPath = null)
    {
        var (pw, ph, sx, sy) = ResolvePagePixels(renderScale, AppendixPreviewCanvasW, AppendixPreviewCanvasH, AppendixPdfWidthMm, AppendixPdfHeightMm);

        using var bmp = new SKBitmap(pw, ph);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        ClipCanvasToPreviewCrop(canvas, previewCrop, sx, sy, pw, ph, AppendixPreviewCanvasW, AppendixPreviewCanvasH);

        if (options?.DrawBackground != false)
            DrawTemplate(canvas, pw, sy, AppendixPreviewCanvasW, templateBytes);

        using var serif = ResolveSerifFace();
        SKTypeface? lazurskiFile = TryLoadTypefaceFromFile(lazurskiTtfPath);
        var scriptFace = lazurskiFile ?? serif;
        try
        {
        var qualificationText = QualificationResolver.Resolve(
            GetDyn(student, "Qualification"),
            specialtyQualificationFallback ?? "");

        var fs = (float)layout.FontSizeScale;
        float Pt(float basePt) => basePt * fs;

        var ds = GetDyn(student, "DiplomaSeries")?.Trim() ?? "";
        var dn = GetDyn(student, "DiplomaNumber")?.Trim() ?? "";
        var blankSeriesNumber = string.Join(" ", new[] { ds, dn }.Where(static x => x.Length > 0));
        if (!string.IsNullOrEmpty(blankSeriesNumber))
        {
            var blankSize = layout.DiplomaBlankFontSize is >= 7 and <= 24 ? layout.DiplomaBlankFontSize : 9;
            DrawBlock(canvas, serif, blankSeriesNumber, layout.DiplomaBlankLeft, layout.DiplomaBlankTop,
                layout.DiplomaBlankWidth, blankSize * fs, Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);
        }

        DrawBlock(canvas, serif, "Курсовые проекты (работы)", layout.CourseTitleLeft, layout.CourseTitleTop, layout.CourseTitleWidth, Pt(9),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, wrap: true);

        DrawCourseworkTable(canvas, serif, layout, coursework, sx, sy, fs);

        DrawBlock(canvas, serif, orgName, layout.OrgLeft, layout.OrgTop, layout.OrgWidth, Pt(8),
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

        DrawBlock(canvas, serif, student.LastName ?? "", layout.LastNameLeft, layout.LastNameTop, layout.LastNameWidth, Pt(12),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);
        DrawBlock(canvas, serif, student.FirstName ?? "", layout.FirstNameLeft, layout.FirstNameTop, layout.FirstNameWidth, Pt(12),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, bold: true);
        DrawBlock(canvas, serif, student.MiddleName ?? "", layout.MiddleNameLeft, layout.MiddleNameTop, layout.MiddleNameWidth, Pt(11),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);

        DrawBlock(canvas, serif, birthDateRu, layout.BirthLeft, layout.BirthTop, layout.BirthWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);
        DrawBlock(canvas, serif, previousEducation, layout.PrevEducationLeft, layout.PrevEducationTop, layout.PrevEducationWidth, Pt(9),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, wrap: true);

        DrawBlock(canvas, serif, studyPeriod, layout.StudyPeriodLeft, layout.StudyPeriodTop, layout.StudyPeriodWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, bold: true);
        DrawBlock(canvas, serif, qualificationText, layout.QualificationLeft, layout.QualificationTop, layout.QualificationWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, bold: true);
        DrawBlock(canvas, serif, specialtyCodeName, layout.SpecialtyLeft, layout.SpecialtyTop, layout.SpecialtyWidth, Pt(9),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, wrap: true);

        DrawBlock(canvas, serif, student.RegistrationNumber ?? "", layout.RegNumberLeft, layout.RegNumberTop, layout.RegNumberWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, bold: true);
        DrawBlock(canvas, serif, issueDateRu, layout.IssueDateLeft, layout.IssueDateTop, layout.IssueDateWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);
        DrawBlock(canvas, serif, directorShortName, layout.DirectorLeft, layout.DirectorTop, layout.DirectorWidth, Pt(10),
            Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);

        DrawBlock(canvas, serif, layout.PageCountText ?? "4", layout.PageCountLeft, layout.PageCountTop, layout.PageCountWidth, Pt(9),
            Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);

        if (showPageNumbers)
        {
            DrawBlock(canvas, serif, layout.PageNum1Text ?? "1", layout.PageNum1Left, layout.PageNum1Top, layout.PageNum1Width, Pt(9),
                Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);
            DrawBlock(canvas, serif, layout.PageNum2Text ?? "2", layout.PageNum2Left, layout.PageNum2Top, layout.PageNum2Width, Pt(9),
                Hex("#1A2E4A"), LayoutAlign.Right, sx, sy);
        }

        return EncodePngWithPreviewCrop(bmp, previewCrop, sx, sy, AppendixPreviewCanvasW, AppendixPreviewCanvasH);
        }
        finally
        {
            lazurskiFile?.Dispose();
        }
    }

    internal static byte[] ComposeAppendixBackPng(
        byte[]? templateBytes,
        AppendixBackOverlayLayout layout,
        IReadOnlyList<Grade> grades,
        IReadOnlyList<AppendixPracticeRow> studyPractices,
        IReadOnlyList<AppendixPracticeRow> productionPractices,
        float renderScale,
        PreviewCropPersisted? previewCrop,
        bool showPageNumbers,
        PrintDocumentOptions? options = null)
    {
        var (pw, ph, sx, sy) = ResolvePagePixels(renderScale, AppendixPreviewCanvasW, AppendixPreviewCanvasH, AppendixPdfWidthMm, AppendixPdfHeightMm);

        using var bmp = new SKBitmap(pw, ph);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        ClipCanvasToPreviewCrop(canvas, previewCrop, sx, sy, pw, ph, AppendixPreviewCanvasW, AppendixPreviewCanvasH);

        if (options?.DrawBackground != false)
            DrawTemplate(canvas, pw, sy, AppendixPreviewCanvasW, templateBytes);

        using var serif = ResolveSerifFace();

        DrawGradesTable(canvas, serif, layout, grades, sx, sy);
        DrawPracticeTableAt(canvas, serif,
            layout.StudyPracticeListLeft, layout.StudyPracticeListTop,
            layout.StudyPracticeListWidth, layout.StudyPracticeListHeight,
            layout.StudyPracticeActivityShare, layout.StudyPracticeMeansShareOfRemainder,
            layout.StudyPracticeColumnGapPx,
            studyPractices, sx, sy);
        DrawPracticeTableAt(canvas, serif,
            layout.ProductionPracticeListLeft, layout.ProductionPracticeListTop,
            layout.ProductionPracticeListWidth, layout.ProductionPracticeListHeight,
            layout.ProductionPracticeActivityShare, layout.ProductionPracticeMeansShareOfRemainder,
            layout.ProductionPracticeColumnGapPx,
            productionPractices, sx, sy);

        if (grades.Count == 0)
        {
            DrawBlock(canvas, serif, "Нет оценок в базе данных", layout.EmptyHintLeft, layout.EmptyHintTop, layout.EmptyHintWidth, 10,
                Hex("#8A8A8A"), LayoutAlign.Left, sx, sy, italic: true, wrap: true);
        }

        if (showPageNumbers)
        {
            DrawBlock(canvas, serif, layout.PageNumBackText ?? "3", layout.PageNumBackLeft, layout.PageNumBackTop, layout.PageNumBackWidth, 9,
                Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);
            DrawBlock(canvas, serif, layout.PageNumBackRightText ?? "4", layout.PageNumBackRightLeft, layout.PageNumBackRightTop,
                layout.PageNumBackRightWidth, 9, Hex("#1A2E4A"), LayoutAlign.Right, sx, sy);
        }

        return EncodePngWithPreviewCrop(bmp, previewCrop, sx, sy, AppendixPreviewCanvasW, AppendixPreviewCanvasH);
    }

    private static void DrawCourseworkTable(
        SKCanvas canvas,
        SKTypeface serif,
        AppendixFrontOverlayLayout layout,
        IReadOnlyList<Grade> rows,
        float sx,
        float sy,
        float fontSizeScale = 1f)
    {
        var rowH = layout.CourseListRowHeight is >= 8 and <= 24 ? layout.CourseListRowHeight : 10.0;
        var tableL = layout.CourseListLeft;
        var tableT = layout.CourseListTop;
        var totalW = layout.CourseListWidth;
        var totalH = layout.CourseListHeight;
        if (totalW < 8 || totalH < rowH) return;

        var w0 = AppendixTableColumnWidthConverter.CourseworkSubjectColumnWidth(totalW);
        var w1 = AppendixTableColumnWidthConverter.CourseworkGradeColumnWidth(totalW);
        var maxRows = Math.Max(0, (int)Math.Floor(totalH / rowH));

        var clipOuter = new SKRect(
            (float)(tableL * sx),
            (float)(tableT * sy),
            (float)((tableL + totalW) * sx),
            (float)((tableT + totalH) * sy));

        canvas.Save();
        canvas.ClipRect(clipOuter, SKClipOperation.Intersect, antialias: false);

        for (var i = 0; i < Math.Min(rows.Count, maxRows); i++)
        {
            var y = tableT + i * rowH;
            var rowClip = new SKRect(
                (float)(tableL * sx),
                (float)(y * sy),
                (float)((tableL + totalW) * sx),
                (float)((y + rowH) * sy));
            canvas.Save();
            canvas.ClipRect(rowClip, SKClipOperation.Intersect, antialias: false);

            var g = rows[i];
            var rowPt = (float)(layout.CourseListFontSize is >= 5 and <= 14 ? layout.CourseListFontSize : 7.5) * fontSizeScale;
            var subject = TrimTextToWidth(serif, g.SubjectName ?? "", rowPt * sy, (float)(w0 * sx));
            DrawBlock(canvas, serif, subject, tableL, y, w0, rowPt, Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, wrap: false);
            DrawBlock(canvas, serif, g.GradeText, tableL + w0, y, w1, rowPt, Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);

            canvas.Restore();
        }

        canvas.Restore();
    }

    private static void DrawGradesTable(
        SKCanvas canvas,
        SKTypeface serif,
        AppendixBackOverlayLayout layout,
        IReadOnlyList<Grade> grades,
        float sx,
        float sy) =>
        DrawGradesTableAt(canvas, serif,
            layout.GradesTableLeft, layout.GradesTableTop,
            layout.GradesTableWidth, layout.GradesTableHeight,
            layout.GradesHoursShareOfTail, layout.GradesHoursGradeGapPx,
            grades, sx, sy);

    private static void DrawGradesTableAt(
        SKCanvas canvas,
        SKTypeface serif,
        double tableL,
        double tableT,
        double totalW,
        double totalH,
        double hoursShareOfTail,
        double hoursGradeGapPx,
        IReadOnlyList<Grade> grades,
        float sx,
        float sy)
    {
        const double rowH = 10.5;
        if (totalW < 8 || totalH < rowH || grades.Count == 0) return;

        var (wSubj, wHours, wGap, wGrade) = AppendixGradesTailColumnWidthMultiConverter.ComputeTailColumnWidths(
            totalW, hoursShareOfTail, hoursGradeGapPx);

        var maxRows = Math.Max(0, (int)Math.Floor(totalH / rowH));

        var clipOuter = new SKRect(
            (float)(tableL * sx),
            (float)(tableT * sy),
            (float)((tableL + totalW) * sx),
            (float)((tableT + totalH) * sy));

        canvas.Save();
        canvas.ClipRect(clipOuter, SKClipOperation.Intersect, antialias: false);

        for (var i = 0; i < Math.Min(grades.Count, maxRows); i++)
        {
            var y = tableT + i * rowH;
            var rowClip = new SKRect(
                (float)(tableL * sx),
                (float)(y * sy),
                (float)((tableL + totalW) * sx),
                (float)((y + rowH) * sy));
            canvas.Save();
            canvas.ClipRect(rowClip, SKClipOperation.Intersect, antialias: false);

            var g = grades[i];
            var hoursText = g.Hours?.ToString() ?? "";

            DrawBlock(canvas, serif, g.SubjectName ?? "", tableL, y, wSubj, 7.5, Hex("#1A2E4A"), LayoutAlign.Left, sx, sy);
            DrawBlock(canvas, serif, hoursText, tableL + wSubj, y, wHours, 7.5, Hex("#1A2E4A"), LayoutAlign.Right, sx, sy);
            DrawBlock(canvas, serif, g.GradeText, tableL + wSubj + wHours + wGap, y, wGrade, 7.5, Hex("#1A2E4A"), LayoutAlign.Center, sx, sy);

            canvas.Restore();
        }

        canvas.Restore();
    }

    private static void DrawPracticeTableAt(
        SKCanvas canvas,
        SKTypeface serif,
        double tableL,
        double tableT,
        double totalW,
        double totalH,
        double activityShare,
        double meansShareOfRemainder,
        double columnGapPx,
        IReadOnlyList<AppendixPracticeRow> rows,
        float sx,
        float sy)
    {
        const double rowH = 14.0;
        const double fontPt = 6.5;
        if (totalW < 8 || totalH < rowH || rows.Count == 0) return;

        var (wActivity, gap, wMeans, wPlace) = AppendixPracticeColumnWidthMultiConverter.ComputeColumnWidths(
            totalW, activityShare, meansShareOfRemainder, columnGapPx);

        var maxRows = Math.Max(0, (int)Math.Floor(totalH / rowH));
        var clipOuter = new SKRect(
            (float)(tableL * sx),
            (float)(tableT * sy),
            (float)((tableL + totalW) * sx),
            (float)((tableT + totalH) * sy));

        canvas.Save();
        canvas.ClipRect(clipOuter, SKClipOperation.Intersect, antialias: false);

        for (var i = 0; i < Math.Min(rows.Count, maxRows); i++)
        {
            var y = tableT + i * rowH;
            var rowClip = new SKRect(
                (float)(tableL * sx),
                (float)(y * sy),
                (float)((tableL + totalW) * sx),
                (float)((y + rowH) * sy));
            canvas.Save();
            canvas.ClipRect(rowClip, SKClipOperation.Intersect, antialias: false);

            var r = rows[i];
            var x = tableL;
            DrawBlock(canvas, serif, r.ActivityText, x, y, wActivity, fontPt, Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);
            x += wActivity + gap;
            DrawBlock(canvas, serif, r.TrainingMeansText, x, y, wMeans, fontPt, Hex("#1A2E4A"), LayoutAlign.Left, sx, sy, wrap: true);
            x += wMeans + gap;
            DrawBlock(canvas, serif, r.PlaceText, x, y, wPlace, fontPt, Hex("#1A2E4A"), LayoutAlign.Center, sx, sy, wrap: true);

            canvas.Restore();
        }

        canvas.Restore();
    }

    private enum LayoutAlign { Left, Center, Right }

    private static void ClipCanvasToPreviewCrop(
        SKCanvas canvas,
        PreviewCropPersisted? crop,
        float sx,
        float sy,
        int pw,
        int ph,
        double canvasW,
        double canvasH)
    {
        if (TryGetPreviewCropPixmapRect(pw, ph, crop, sx, sy, canvasW, canvasH, out var rect))
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                canvas.ClipRect(rect, SKClipOperation.Intersect, antialias: false);
            }
        }
    }

    private static (int pw, int ph, float sx, float sy) ResolvePagePixels(
        float renderScale,
        double canvasW,
        double canvasH,
        float pageWidthMm,
        float pageHeightMm)
    {
        var pageWPt = pageWidthMm * MmToPoints;
        var pageHPt = pageHeightMm * MmToPoints;
        var pw = Math.Max(1, (int)Math.Round(pageWPt * renderScale));
        var ph = Math.Max(1, (int)Math.Round(pageHPt * renderScale));
        var sx = (float)(pw / canvasW);
        var sy = (float)(ph / canvasH);
        return (pw, ph, sx, sy);
    }

    private static void DrawTemplate(SKCanvas canvas, int pw, float sy, double canvasW, byte[]? templateBytes)
    {
        if (templateBytes == null || templateBytes.Length == 0) return;
        using var decoded = SKBitmap.Decode(templateBytes);
        if (decoded == null) return;
        
        double previewImageH = canvasW * decoded.Height / decoded.Width;
        float destH = (float)(previewImageH * sy);
        
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(decoded, new SKRect(0, 0, pw, destH), paint);
    }

    private static void DrawDuplicateLabel(SKCanvas canvas, double left, double top, double width, double fontSize, float sx, float sy)
    {
        using var serif = ResolveSerifFace();
        DrawBlock(canvas, serif, "Дубликат", left, top, width, fontSize, Hex("#000000"), LayoutAlign.Center, sx, sy, bold: true);
    }

    private static void DrawDuplicateLabel(SKCanvas canvas, DiplomaFrontOverlayLayout layout, float sx, float sy) =>
        DrawDuplicateLabel(canvas, layout.DuplicateLeft, layout.DuplicateTop, layout.DuplicateWidth, layout.DuplicateFontSize, sx, sy);

    private static void DrawDuplicateLabel(SKCanvas canvas, DiplomaBackOverlayLayout layout, float sx, float sy) =>
        DrawDuplicateLabel(canvas, layout.DuplicateLeft, layout.DuplicateTop, layout.DuplicateWidth, layout.DuplicateFontSize, sx, sy);

    private static void DrawDiplomaBackQr(
        SKCanvas canvas,
        DiplomaBackOverlayLayout layout,
        string? diplomaQrPayload,
        float sx,
        float sy)
    {
        if (layout.QrSize < 4) return;
        var text = diplomaQrPayload?.Trim() ?? "";
        if (text.Length == 0) return;
        var pngBytes = DiplomaQrCodeService.RenderPng(text);
        if (pngBytes == null || pngBytes.Length == 0) return;
        using var qrBmp = SKBitmap.Decode(pngBytes);
        if (qrBmp == null) return;
        // sx≠sy: квадратные модули — только side = QrSize*min(sx,sy). Ячейка на странице — QrSize*sx × QrSize*sy;
        // при привязке к левому верху остаётся «лишнее» белое поле справа/снизу (QR визуально «уезжает» в угол).
        var cellLeft = (float)(layout.QrLeft * sx);
        var cellTop = (float)(layout.QrTop * sy);
        var cellW = (float)(layout.QrSize * sx);
        var cellH = (float)(layout.QrSize * sy);
        var s = Math.Min(sx, sy);
        var side = (float)(layout.QrSize * s);
        var left = (float)Math.Round(cellLeft + (cellW - side) * 0.5);
        var top = (float)Math.Round(cellTop + (cellH - side) * 0.5);
        var sideR = Math.Max(1f, (float)Math.Round(side));
        var dest = new SKRect(left, top, left + sideR, top + sideR);
        using var paint = new SKPaint { IsAntialias = false };
        canvas.DrawBitmap(qrBmp, new SKRect(0, 0, qrBmp.Width, qrBmp.Height), dest, paint);
    }

    private static SKTypeface ResolveSerifFace() =>
        SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal)
        ?? SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold)
        ?? SKTypeface.FromFamilyName("Georgia")
        ?? SKTypeface.Default;

    private static SKTypeface? TryLoadTypefaceFromFile(string? ttfPath)
    {
        if (string.IsNullOrWhiteSpace(ttfPath) || !File.Exists(ttfPath)) return null;
        return SKTypeface.FromFile(ttfPath);
    }

    private static void DrawBlock(
        SKCanvas canvas,
        SKTypeface typeface,
        string text,
        double left,
        double top,
        double width,
        double fontSizePreview,
        SKColor color,
        LayoutAlign align,
        float sx,
        float sy,
        bool wrap = false,
        bool bold = false,
        bool italic = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = SanitizeDrawText(text);

        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0) width = 0;
        if (double.IsNaN(left) || double.IsInfinity(left)) left = 0;
        if (double.IsNaN(top) || double.IsInfinity(top)) top = 0;

        var fs = (float)(fontSizePreview * sy);
        if (double.IsNaN(fs) || double.IsInfinity(fs) || fs < 4) fs = 4;

        var style = (bold, italic) switch
        {
            (true, true) => SKFontStyle.BoldItalic,
            (true, _) => SKFontStyle.Bold,
            (_, true) => SKFontStyle.Italic,
            _ => SKFontStyle.Normal,
        };

        SKTypeface faceToUse = typeface;
        SKTypeface? faceFromStyle = null;
        if ((bold || italic) && !string.IsNullOrEmpty(typeface.FamilyName))
        {
            faceFromStyle = SKTypeface.FromFamilyName(typeface.FamilyName, style);
            if (faceFromStyle != null) faceToUse = faceFromStyle;
        }

        try
        {
        using var font = new SKFont(faceToUse, fs);
        font.GetFontMetrics(out var fm);

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            FakeBoldText = bold && faceFromStyle == null,
            TextAlign = align switch
            {
                LayoutAlign.Center => SKTextAlign.Center,
                LayoutAlign.Right => SKTextAlign.Right,
                _ => SKTextAlign.Left,
            },
        };

        font.Edging = SKFontEdging.Antialias;

        float xAnchor = align switch
        {
            LayoutAlign.Center => (float)(left * sx + width * sx * 0.5),
            LayoutAlign.Right => (float)((left + width) * sx),
            _ => (float)(left * sx),
        };

        var lines = wrap
            ? WrapLines(text, (float)(width * sx), font)
            : new List<string> { text };

        var lineHeight = fs * 1.2f;
        var ascent = Math.Abs(fm.Ascent);
        float y = (float)(top * sy) + ascent + fs * 0.06f;

        foreach (var line in lines)
        {
            try
            {
                canvas.DrawText(line, xAnchor, y, font, paint);
            }
            catch (Exception)
            {
                using var serif = ResolveSerifFace();
                using var fallbackFont = new SKFont(serif, fs);
                canvas.DrawText(line, xAnchor, y, fallbackFont, paint);
            }
            y += lineHeight;
        }
        }
        finally
        {
            faceFromStyle?.Dispose();
        }
    }

    private static string SanitizeDrawText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (!char.IsSurrogate(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string TrimTextToWidth(SKTypeface typeface, string text, float fontSizePx, float maxWidthPx)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidthPx < 4) return text;
        using var font = new SKFont(typeface, fontSizePx);
        if (font.MeasureText(text) <= maxWidthPx) return text;
        const string ell = "…";
        var t = text.Trim();
        while (t.Length > 1 && font.MeasureText(t + ell) > maxWidthPx)
            t = t[..^1];
        return t + ell;
    }

    private static List<string> WrapLines(string text, float maxWidth, SKFont font)
    {
        if (maxWidth < 8) return new List<string> { text };
        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var cur = new StringBuilder();

        foreach (var w in words)
        {
            var trial = cur.Length == 0 ? w : $"{cur} {w}";
            var tw = font.MeasureText(trial);
            if (tw <= maxWidth || cur.Length == 0)
                cur.Clear().Append(trial);
            else
            {
                lines.Add(cur.ToString());
                cur.Clear().Append(w);
            }
        }
        if (cur.Length > 0) lines.Add(cur.ToString());
        return lines.Count == 0 ? new List<string> { text } : lines;
    }

    /// <summary>
    /// Вырезаем кроп в пикселях, затем снова рисуем его на холст того же размера, что и <paramref name="bmp"/> (ширина×высота страницы в пикселях).
    /// Иначе соотношение сторон вырезки ≠ страницы A4, и QuestPDF <c>FitArea()</c> даёт белые полосы (часто снизу).
    /// Небольшое неравномерное растяжение совпадает с тем, что уже заложено в раскладку (разные sx/sy от 730×520 к странице).
    /// </summary>
    private static byte[] EncodePngWithPreviewCrop(
        SKBitmap bmp,
        PreviewCropPersisted? previewCrop,
        float sx,
        float sy,
        double canvasW,
        double canvasH)
    {
        var pw = bmp.Width;
        var ph = bmp.Height;

        if (!TryGetPreviewCropPixmapRect(pw, ph, previewCrop, sx, sy, canvasW, canvasH, out var rect)
            || (rect.Left == 0 && rect.Top == 0 && rect.Right == pw && rect.Bottom == ph))
        {
            return EncodePng(bmp);
        }

        using var img = SKImage.FromBitmap(bmp);
        using var sub = img.Subset(rect);
        if (sub == null)
            return EncodePng(bmp);

        using var outBmp = new SKBitmap(pw, ph);
        using (var canvas = new SKCanvas(outBmp))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
            canvas.DrawImage(sub, new SKRect(0, 0, pw, ph), paint);
        }

        return EncodePng(outBmp);
    }

    private static bool TryGetPreviewCropPixmapRect(
        int pw,
        int ph,
        PreviewCropPersisted? crop,
        float sx,
        float sy,
        double canvasW,
        double canvasH,
        out SKRectI rect)
    {
        var tmp = new PreviewCropInsets();
        tmp.ApplyPersisted(crop);
        tmp.ClampToCanvas(canvasW, canvasH);

        var lf = (float)(tmp.InsetLeft * sx);
        var tf = (float)(tmp.InsetTop * sy);
        var rf = (float)(pw - tmp.InsetRight * sx);
        var bf = (float)(ph - tmp.InsetBottom * sy);

        var li = (int)Math.Ceiling(lf - 1e-4f);
        var ti = (int)Math.Ceiling(tf - 1e-4f);
        // Для правой/нижней границ используем floor, чтобы не захватить лишнюю строку/столбец пикселей
        // (иначе в PDF может появляться тонкая белая полоса снизу/справа из-за округления и последующего растягивания).
        var ri = (int)Math.Floor(rf + 1e-4f);
        var bi = (int)Math.Floor(bf + 1e-4f);

        li = Math.Clamp(li, 0, Math.Max(0, pw - 1));
        ti = Math.Clamp(ti, 0, Math.Max(0, ph - 1));
        ri = Math.Clamp(ri, li + 1, pw);
        bi = Math.Clamp(bi, ti + 1, ph);

        rect = new SKRectI(li, ti, ri, bi);
        return rect.Width > 0 && rect.Height > 0;
    }

    private static byte[] EncodePng(SKBitmap bmp)
    {
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? Array.Empty<byte>();
    }

    private static SKColor Hex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
            return new SKColor((byte)((u >> 16) & 0xFF), (byte)((u >> 8) & 0xFF), (byte)(u & 0xFF));
        return SKColors.Black;
    }

    private static string? GetDyn(object obj, string propName)
    {
        try
        {
            var p = obj.GetType().GetProperty(propName);
            return p?.GetValue(obj)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
