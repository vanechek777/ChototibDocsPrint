using System.Text.Json;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Клонирование профилей раскладки (отдельно для обычного диплома и с отличием).</summary>
public static class DiplomaLayoutProfileHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    public static DiplomaFrontOverlayLayout Clone(DiplomaFrontOverlayLayout source) =>
        CloneJson(source) ?? new DiplomaFrontOverlayLayout();

    public static DiplomaBackOverlayLayout Clone(DiplomaBackOverlayLayout source) =>
        CloneJson(source) ?? new DiplomaBackOverlayLayout();

    public static PreviewCropPersisted Clone(PreviewCropPersisted? source) =>
        CloneJson(source) ?? new PreviewCropPersisted();

    public static DiplomaVariantLayoutPersisted Capture(
        DiplomaFrontOverlayLayout front,
        DiplomaBackOverlayLayout back,
        PreviewCropPersisted cropFront,
        PreviewCropPersisted cropBack) =>
        new()
        {
            DiplomaFrontOverlay = Clone(front),
            DiplomaBackOverlay = Clone(back),
            CropDiplomaFront = Clone(cropFront),
            CropDiplomaBack = Clone(cropBack),
        };

    public static void ApplyToViewModel(
        DiplomaVariantLayoutPersisted? persisted,
        Action<DiplomaFrontOverlayLayout> setFront,
        Action<DiplomaBackOverlayLayout> setBack,
        Action<PreviewCropPersisted> applyCropFront,
        Action<PreviewCropPersisted> applyCropBack)
    {
        if (persisted?.DiplomaFrontOverlay != null)
            setFront(Clone(persisted.DiplomaFrontOverlay));
        if (persisted?.DiplomaBackOverlay != null)
            setBack(Clone(persisted.DiplomaBackOverlay));
        applyCropFront(Clone(persisted?.CropDiplomaFront ?? new PreviewCropPersisted()));
        applyCropBack(Clone(persisted?.CropDiplomaBack ?? new PreviewCropPersisted()));
    }

    public static DiplomaVariantLayoutPersisted CloneProfile(DiplomaVariantLayoutPersisted? source)
    {
        if (source == null)
            return new DiplomaVariantLayoutPersisted();
        return Capture(
            source.DiplomaFrontOverlay ?? new DiplomaFrontOverlayLayout(),
            source.DiplomaBackOverlay ?? new DiplomaBackOverlayLayout(),
            source.CropDiplomaFront ?? new PreviewCropPersisted(),
            source.CropDiplomaBack ?? new PreviewCropPersisted());
    }

    private static T? CloneJson<T>(T source) where T : class
    {
        if (source == null) return null;
        var json = JsonSerializer.Serialize(source, JsonOpts);
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }
}
