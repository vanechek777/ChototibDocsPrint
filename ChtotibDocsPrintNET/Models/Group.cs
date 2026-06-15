namespace ChtotibDocsPrintNET.Models;

/// <summary>Учебная группа.</summary>
public class Group
{
    public int Id { get; set; }
    public int SpecialtyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EnrollmentYear { get; set; }
    /// <summary>Корпус (фильтр на экране групп).</summary>
    public string? Address { get; set; }
    public bool IsGraduating { get; set; }
    public int CourseNumber { get; set; } = 1;
    public string? SpecialtyName { get; set; }
    public int StudentCount { get; set; }
}
