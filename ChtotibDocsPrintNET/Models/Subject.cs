namespace ChtotibDocsPrintNET.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
    public int? Hours { get; set; }
    public int? SpecialtyId { get; set; }

    /// <summary>
    /// Для редактирования в DataGrid: 0 = «Общие», >0 = Id специальности.
    /// </summary>
    public int SpecialtyKey
    {
        get => SpecialtyId ?? 0;
        set => SpecialtyId = value > 0 ? value : null;
    }
    /// <summary>Для отображения в списках: «Общие» или «код — название» специальности.</summary>
    public string SpecialtyDisplay { get; set; } = "Общие";
    public string SubjectType { get; set; } = "Общеобразовательный";
    public bool IsExam { get; set; }

    /// <summary>Для выбора предмета при выставлении оценки.</summary>
    public string GradePickerDisplay =>
        $"{Name} ({Course} курс, {SpecialtyDisplay})";
}
