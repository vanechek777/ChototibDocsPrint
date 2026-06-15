namespace ChtotibDocsPrintNET.Models;

/// <summary>Фрагмент JSON (без Observable/событий) для сериализации отступов кропа.</summary>
public class PreviewCropPersisted
{
    public double InsetLeft { get; set; }
    public double InsetRight { get; set; }
    public double InsetTop { get; set; }
    public double InsetBottom { get; set; }
}
