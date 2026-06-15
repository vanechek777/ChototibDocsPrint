namespace ChtotibDocsPrintNET.Models;

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int GradeValue { get; set; }
    public string GradeType { get; set; } = "Итоговая";
    public string? SubjectName { get; set; }
    public int Course { get; set; }
    public int? Hours { get; set; }
    public string? PracticePlace { get; set; }
    public string? PracticeTrainingMeans { get; set; }
    public string GradeText => GradeValue switch
    {
        5 => "отлично",
        4 => "хорошо",
        3 => "удовл.",
        2 => "неудовлетворительно",
        _ => GradeValue.ToString()
    };
}
