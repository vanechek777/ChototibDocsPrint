using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

public static class AppendixPracticeDisplayBuilder
{
    public const string TrainingMeansText =
        "При прохождении практики использовались следующие средства обучения и воспитания, включая оборудование, программное обеспечение, технологии и другое.";

    public static readonly string[] PracticePlaces =
    [
        "ООО \"Ромашка\"",
        "ООО \"Василёк\"",
        "ООО \"ТехноПлюс\"",
        "ООО \"СибИнфо\"",
        "АО \"ВостокТелеком\"",
        "ООО \"Альфа-Сервис\"",
        "ООО \"Горный экспресс\"",
        "ИП Петров А.В.",
    ];

    public static List<AppendixPracticeRow> BuildStudyRows(IEnumerable<Grade> grades, int studentId) =>
        BuildRows(grades, studentId, isProduction: false, "Учебная практика");

    public static List<AppendixPracticeRow> BuildProductionRows(IEnumerable<Grade> grades, int studentId) =>
        BuildRows(grades, studentId, isProduction: true, "Производственная практика");

    private static List<AppendixPracticeRow> BuildRows(
        IEnumerable<Grade> grades, int studentId, bool isProduction, string defaultActivity)
    {
        var list = grades.ToList();
        if (list.Count == 0)
            return [];

        var rows = new List<AppendixPracticeRow>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            var g = list[i];
            rows.Add(new AppendixPracticeRow
            {
                ActivityText = ResolveActivityText(g, defaultActivity),
                TrainingMeansText = ResolveTrainingMeansText(g),
                PlaceText = ResolvePlaceText(g, studentId, isProduction, i),
            });
        }

        return rows;
    }

    private static string ResolveActivityText(Grade g, string fallback)
    {
        var name = (g.SubjectName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return fallback;
        if (name.Contains("практик", StringComparison.OrdinalIgnoreCase))
            return fallback;
        return name;
    }

    private static string ResolveTrainingMeansText(Grade g)
    {
        var custom = (g.PracticeTrainingMeans ?? "").Trim();
        return string.IsNullOrEmpty(custom) ? TrainingMeansText : custom;
    }

    private static string ResolvePlaceText(Grade g, int studentId, bool isProduction, int rowIndex)
    {
        var stored = (g.PracticePlace ?? "").Trim();
        if (!string.IsNullOrEmpty(stored))
            return stored;
        return PickPlace(studentId, isProduction, rowIndex);
    }

    private static string PickPlace(int studentId, bool isProduction, int rowIndex)
    {
        var key = HashCode.Combine(studentId, isProduction, rowIndex);
        var idx = Math.Abs(key) % PracticePlaces.Length;
        return PracticePlaces[idx];
    }
}
