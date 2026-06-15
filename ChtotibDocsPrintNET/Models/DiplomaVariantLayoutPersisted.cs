namespace ChtotibDocsPrintNET.Models;

/// <summary>Раскладка и кроп диплома для одного варианта бланка (обычный или с отличием).</summary>
public class DiplomaVariantLayoutPersisted
{
    public DiplomaFrontOverlayLayout? DiplomaFrontOverlay { get; set; }

    public DiplomaBackOverlayLayout? DiplomaBackOverlay { get; set; }

    public PreviewCropPersisted? CropDiplomaFront { get; set; }

    public PreviewCropPersisted? CropDiplomaBack { get; set; }
}
