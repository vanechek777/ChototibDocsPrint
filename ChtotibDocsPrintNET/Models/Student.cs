namespace ChtotibDocsPrintNET.Models;

/// <summary>Студент — только поля для диплома и приложения.</summary>
public class Student
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? PreviousEducation { get; set; }
    /// <summary>Документ о предыдущем образовании (аттестат, диплом СПО и т.д.) — для приложения.</summary>
    public string? PreviousEducationDoc { get; set; }
    public string? RegistrationNumber { get; set; }
    /// <summary>Квалификация на бланке (если пусто — из специальности группы).</summary>
    public string? Qualification { get; set; }
    public bool IsGraduated { get; set; }
    public bool IsExpelled { get; set; }
    /// <summary>Код участника демонстрационного экзамена (для QR).</summary>
    public string? DemoExamParticipantCode { get; set; }
    /// <summary>Баллы демоэкзамена (для QR).</summary>
    public decimal? DemoExamScore { get; set; }
    /// <summary>Максимум баллов демоэкзамена (по умолчанию 70).</summary>
    public int DemoExamMaxScore { get; set; } = 70;
    /// <summary>Уровень демоэкзамена (по умолчанию «профильный уровень»).</summary>
    public string? DemoExamLevel { get; set; }
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    public string? GroupName { get; set; }
}
