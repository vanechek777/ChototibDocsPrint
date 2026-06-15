using System.Text.RegularExpressions;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Квалификация для бланка: студент → специальность → текст в скобках в названии → код ФГОС.</summary>
public static partial class QualificationResolver
{
    [GeneratedRegex(@"\(([^)]+)\)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex QualificationInNameRegex();

    public static string Resolve(string? studentQualification, string? specialtyQualificationFallback) =>
        Resolve(studentQualification,
            string.IsNullOrWhiteSpace(specialtyQualificationFallback)
                ? null
                : new Specialty { Qualification = specialtyQualificationFallback });

    public static string Resolve(string? studentQualification, Specialty? specialty)
    {
        if (!string.IsNullOrWhiteSpace(studentQualification))
            return studentQualification.Trim();

        if (specialty != null && !string.IsNullOrWhiteSpace(specialty.Qualification))
            return specialty.Qualification.Trim();

        var fromName = TryExtractFromSpecialtyName(specialty?.Name);
        if (!string.IsNullOrEmpty(fromName))
            return fromName;

        return specialty?.Code switch
        {
            "09.02.07" => "Программист",
            "09.02.06" => "Специалист по защите информации",
            _ => "",
        };
    }

    private static string TryExtractFromSpecialtyName(string? specialtyName)
    {
        if (string.IsNullOrWhiteSpace(specialtyName)) return "";
        var m = QualificationInNameRegex().Match(specialtyName.Trim());
        if (!m.Success) return "";
        var raw = m.Groups[1].Value.Trim();
        if (raw.Length == 0) return "";
        return char.ToUpper(raw[0], System.Globalization.CultureInfo.GetCultureInfo("ru-RU")) + raw[1..];
    }
}
