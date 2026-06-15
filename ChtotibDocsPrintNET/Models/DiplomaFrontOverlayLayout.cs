using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ChtotibDocsPrintNET.Models;

/// <summary>Координаты надписей на предпросмотре лицевой стороны диплома (холст 730×520 px).</summary>
public partial class DiplomaFrontOverlayLayout : ObservableObject
{
    /// <summary>Надпись «Дубликат» (лицевая сторона).</summary>
    [ObservableProperty] private double _duplicateLeft = 220;
    [ObservableProperty] private double _duplicateTop = 28;
    [ObservableProperty] private double _duplicateWidth = 290;
    [ObservableProperty] private double _duplicateFontSize = 26;

    /// <summary>Вертикальные направляющие центровки (X, px).</summary>
    [ObservableProperty] private double _centeringGuide1X = 190;
    [ObservableProperty] private double _centeringGuide2X = 560;

    partial void OnDuplicateFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, 8, 48);
        if (Math.Abs(clamped - value) > 0.001)
            DuplicateFontSize = clamped;
    }
}
