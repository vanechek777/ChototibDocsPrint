namespace ChtotibDocsPrintNET.Models;

public class CommissionMember
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string Role { get; set; } = "Председатель";
    public bool IsActive { get; set; } = true;
}
