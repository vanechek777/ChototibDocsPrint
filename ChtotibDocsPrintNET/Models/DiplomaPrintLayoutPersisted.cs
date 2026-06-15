namespace ChtotibDocsPrintNET.Models;

/// <summary>Сохраняется в AppData одним JSON (раскладка + кроп предпросмотра).</summary>
public class DiplomaPrintLayoutPersisted
{
    /// <summary>Раскладка для синего бланка (обычный диплом).</summary>
    public DiplomaVariantLayoutPersisted? StandardDiploma { get; set; }

    /// <summary>Раскладка для бланка с отличием (красный).</summary>
    public DiplomaVariantLayoutPersisted? HonorDiploma { get; set; }

    /// <summary>Наследие: единая раскладка до разделения профилей.</summary>
    public DiplomaFrontOverlayLayout? DiplomaFrontOverlay { get; set; }

    public DiplomaBackOverlayLayout? DiplomaBackOverlay { get; set; }

    /// <summary>
    /// Кроп диплома (наследие): раньше был один на лицо+оборот.
    /// Если есть <see cref="CropDiplomaFront"/>/<see cref="CropDiplomaBack"/> — этот игнорируется.
    /// </summary>
    public PreviewCropPersisted? CropDiploma { get; set; }

    /// <summary>Кроп лицевой стороны диплома (холст 730×520).</summary>
    public PreviewCropPersisted? CropDiplomaFront { get; set; }

    /// <summary>Кроп оборота диплома (холст 730×520).</summary>
    public PreviewCropPersisted? CropDiplomaBack { get; set; }

    public PreviewCropPersisted? CropAppendix { get; set; }

    /// <summary>Текст для QR на обороте диплома (обычно URL реестра / проверки).</summary>
    public string? DiplomaQrLink { get; set; }

    /// <summary>Направляющие центровки, приложение — лицевая (X, холст 780×560).</summary>
    public double? AppendixFrontCenteringGuide1X { get; set; }

    public double? AppendixFrontCenteringGuide2X { get; set; }

    /// <summary>Направляющие центровки, приложение — оборотная.</summary>
    public double? AppendixCenteringGuide1X { get; set; }

    public double? AppendixCenteringGuide2X { get; set; }

    public AppendixFrontOverlayLayout? AppendixFrontOverlay { get; set; }

    public AppendixBackOverlayLayout? AppendixBackOverlay { get; set; }

    /// <summary>Номера страниц на предпросмотре приложения (лицо и оборот: по два блока на разворот).</summary>
    public bool? AppendixShowPageNumbers { get; set; }
}
