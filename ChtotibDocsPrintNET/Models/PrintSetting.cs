namespace ChtotibDocsPrintNET.Models;

public class PrintSetting
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public double PositionX_mm { get; set; }
    public double PositionY_mm { get; set; }
    public double FontSize { get; set; } = 12;
    public string FontFamily { get; set; } = "Times New Roman";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string TextAlignment { get; set; } = "Left";
    public double? MaxWidth_mm { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
