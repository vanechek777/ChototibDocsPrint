using System.Globalization;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

public static class StudentDiplomaPrintHelper
{
    /// <summary>Только реквизиты документа (серия/номер аттестата), без названия школы.</summary>
    public static string FormatPreviousEducation(Student? student)
    {
        if (student == null) return "";
        var doc = student.PreviousEducationDoc?.Trim();
        if (!string.IsNullOrEmpty(doc))
            return doc;
        var legacy = student.PreviousEducation?.Trim() ?? "";
        if (legacy.Contains("МБОУ", StringComparison.OrdinalIgnoreCase)
            || legacy.Contains("СОШ", StringComparison.OrdinalIgnoreCase))
            return "";
        return legacy;
    }

    public static DateTime ResolveIssueDate(DateTime? diplomaIssueDate, DateTime fallback) =>
        diplomaIssueDate?.Date ?? fallback.Date;

    public static string FormatIssueDateRu(DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.GetCultureInfo("ru-RU");
        return date.ToString("dd MMMM yyyy г.", culture);
    }

    public static string FormatGecDecisionLine(DateTime issueDate, CultureInfo? culture = null) =>
        "от " + FormatIssueDateRu(issueDate, culture);
}
