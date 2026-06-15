namespace ChtotibDocsPrintNET.Models;

/// <summary>Студент и параметры печати одной страницы/комплекта.</summary>
public sealed class StudentPrintRow
{
    public required Student Student { get; init; }
    public required DateTime IssueDate { get; init; }
    public required string GecDecisionDateLine { get; init; }
    public string? QrPayload { get; init; }
    public bool UseHonorTemplate { get; init; }

    public DiplomaFrontOverlayLayout DiplomaFrontLayout { get; init; } = new();

    public DiplomaBackOverlayLayout DiplomaBackLayout { get; init; } = new();

    public PreviewCropPersisted CropDiplomaFront { get; init; } = new();

    public PreviewCropPersisted CropDiplomaBack { get; init; } = new();
}
