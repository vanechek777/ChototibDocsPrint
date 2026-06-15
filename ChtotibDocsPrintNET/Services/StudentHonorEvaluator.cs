using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>
/// Определяет, использовать ли бланк диплома с отличием (красный/отличный шаблон).
/// </summary>
public static class StudentHonorEvaluator
{
    public static bool IsHonorDiplomaType(string? diplomaType) =>
        !string.IsNullOrWhiteSpace(diplomaType)
        && diplomaType.Contains("отличи", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Все итоговые оценки (не курсовые) равны 5; при отсутствии оценок — false.
    /// </summary>
    public static bool AllFinalGradesAreExcellent(IEnumerable<Grade>? grades)
    {
        if (grades == null) return false;
        var final = grades.Where(g => !IsExcludedFromHonor(g)).ToList();
        if (final.Count == 0) return false;
        return final.All(g => g.GradeValue == 5);
    }

    public static bool ShouldUseHonorTemplate(string? diplomaType, IEnumerable<Grade>? grades) =>
        IsHonorDiplomaType(diplomaType) || AllFinalGradesAreExcellent(grades);

    private static bool IsCoursework(Grade g) =>
        string.Equals(g.GradeType, "Курсовая", StringComparison.OrdinalIgnoreCase);

    private static bool IsExcludedFromHonor(Grade g) =>
        IsCoursework(g) || AppendixGradeClassifier.IsPractice(g);
}
