namespace ChtotibDocsPrintNET.Models;

public class Specialty
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string Qualification { get; set; } = string.Empty;
    public string StudyForm { get; set; } = "Очная";
    public decimal StudyYears { get; set; } = 3.10m;
    public bool IsActive { get; set; } = true;

    /// <summary>Строка для ComboBox (код + название).</summary>
    public string ComboDisplay =>
        Id <= 0
            ? Name
            : string.IsNullOrWhiteSpace(Code)
                ? Name
                : $"{Code} — {Name}";
}
