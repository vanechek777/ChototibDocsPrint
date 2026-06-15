namespace ChtotibDocsPrintNET.Models;

/// <summary>Бланк диплома студента (серия/номер, тип).</summary>
public class Diploma
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? Series { get; set; }
    public string? Number { get; set; }
    public string DiplomaType { get; set; } = "Обычный";
    public DateTime? IssueDate { get; set; }
    public bool IsPrinted { get; set; }
}
