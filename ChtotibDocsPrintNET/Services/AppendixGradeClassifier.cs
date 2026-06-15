using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Разделяет оценки приложения: дисциплины (стр. 3), учебные и производственные практики (стр. 4).</summary>
public static class AppendixGradeClassifier
{
    public static bool IsStudyPractice(Grade g)
    {
        var name = g.SubjectName ?? "";
        var type = g.GradeType ?? "";
        if (ContainsBoth(name, "учебн", "практик")) return true;
        if (string.Equals(name, "УП", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(type, "УП", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProductionPractice(Grade g)
    {
        var name = g.SubjectName ?? "";
        var type = g.GradeType ?? "";
        if (ContainsBoth(name, "производств", "практик")) return true;
        if (name.Contains("преддиплом", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(name, "ПП", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "ПДП", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(type, "ПП", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "ПДП", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPractice(Grade g) =>
        IsStudyPractice(g) || IsProductionPractice(g)
        || string.Equals(g.GradeType, "Практика", StringComparison.OrdinalIgnoreCase);

    public static (List<Grade> Disciplines, List<Grade> Study, List<Grade> Production) Split(IEnumerable<Grade> grades)
    {
        var disciplines = new List<Grade>();
        var study = new List<Grade>();
        var production = new List<Grade>();

        foreach (var g in grades)
        {
            if (IsStudyPractice(g))
                study.Add(g);
            else if (IsProductionPractice(g))
                production.Add(g);
            else if (IsPractice(g))
            {
                var name = g.SubjectName ?? "";
                if (name.Contains("производств", StringComparison.OrdinalIgnoreCase))
                    production.Add(g);
                else
                    study.Add(g);
            }
            else
                disciplines.Add(g);
        }

        return (disciplines, study, production);
    }

    private static bool ContainsBoth(string text, string a, string b) =>
        text.Contains(a, StringComparison.OrdinalIgnoreCase)
        && text.Contains(b, StringComparison.OrdinalIgnoreCase);
}
