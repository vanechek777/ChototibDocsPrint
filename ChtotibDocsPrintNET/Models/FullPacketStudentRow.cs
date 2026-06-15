namespace ChtotibDocsPrintNET.Models;

/// <summary>Данные одного ученика для PDF «диплом + приложение» (4 страницы).</summary>
public sealed class FullPacketStudentRow
{
    public required Student Student { get; init; }
    public IReadOnlyList<Grade> CourseworkGrades { get; init; } = Array.Empty<Grade>();
    public IReadOnlyList<Grade> SubjectGrades { get; init; } = Array.Empty<Grade>();
    public IReadOnlyList<AppendixPracticeRow> StudyPracticeGrades { get; init; } = Array.Empty<AppendixPracticeRow>();
    public IReadOnlyList<AppendixPracticeRow> ProductionPracticeGrades { get; init; } = Array.Empty<AppendixPracticeRow>();
    public required string BirthDateRu { get; init; }
    public required string PreviousEducation { get; init; }
    public required DateTime IssueDate { get; init; }
    public required string IssueDateRu { get; init; }
    public required string GecDecisionDateLine { get; init; }
    public string? QrPayload { get; init; }
    public bool UseHonorTemplate { get; init; }

    public DiplomaFrontOverlayLayout DiplomaFrontLayout { get; init; } = new();

    public DiplomaBackOverlayLayout DiplomaBackLayout { get; init; } = new();

    public PreviewCropPersisted CropDiplomaFront { get; init; } = new();

    public PreviewCropPersisted CropDiplomaBack { get; init; } = new();
}
