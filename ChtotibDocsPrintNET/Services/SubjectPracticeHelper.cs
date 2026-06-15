using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Определяет учебные/производственные практики по предмету и подбирает тип оценки.</summary>
public static class SubjectPracticeHelper
{
    public static bool IsPracticeSubject(Subject subject)
    {
        if (IsStudyPracticeSubject(subject) || IsProductionPracticeSubject(subject))
            return true;

        var type = subject.SubjectType ?? "";
        var name = subject.Name ?? "";
        return type.Contains("Практика", StringComparison.OrdinalIgnoreCase)
               && name.Contains("практик", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsStudyPracticeSubject(Subject subject)
    {
        var type = subject.SubjectType ?? "";
        var name = subject.Name ?? "";
        if (string.Equals(type, "УП", StringComparison.OrdinalIgnoreCase)) return true;
        return ContainsBoth(name, "учебн", "практик");
    }

    public static bool IsProductionPracticeSubject(Subject subject)
    {
        var type = subject.SubjectType ?? "";
        var name = subject.Name ?? "";
        if (string.Equals(type, "ПП", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "ПДП", StringComparison.OrdinalIgnoreCase)) return true;
        if (ContainsBoth(name, "производств", "практик")) return true;
        return name.Contains("преддиплом", StringComparison.OrdinalIgnoreCase);
    }

    public static string DefaultActivityText(Subject subject)
    {
        if (IsProductionPracticeSubject(subject))
            return "Производственная практика";
        if (IsStudyPracticeSubject(subject))
            return "Учебная практика";
        return string.IsNullOrWhiteSpace(subject.Name) ? "Практика" : subject.Name.Trim();
    }

    public static string ResolveGradeType(Subject subject)
    {
        var type = (subject.SubjectType ?? "").Trim();
        if (string.Equals(type, "УП", StringComparison.OrdinalIgnoreCase)) return "УП";
        if (string.Equals(type, "ПДП", StringComparison.OrdinalIgnoreCase)) return "ПДП";
        if (string.Equals(type, "ПП", StringComparison.OrdinalIgnoreCase)) return "ПП";
        if (IsProductionPracticeSubject(subject)) return "ПП";
        if (IsStudyPracticeSubject(subject)) return "УП";
        return "Практика";
    }

    private static bool ContainsBoth(string text, string a, string b) =>
        text.Contains(a, StringComparison.OrdinalIgnoreCase)
        && text.Contains(b, StringComparison.OrdinalIgnoreCase);
}
