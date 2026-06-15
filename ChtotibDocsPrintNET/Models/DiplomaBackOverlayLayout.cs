using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ChtotibDocsPrintNET.Models;

/// <summary>Смещения надписей на предпросмотре оборота диплома (px внутри холста 730×520).</summary>
public partial class DiplomaBackOverlayLayout : ObservableObject
{
    [ObservableProperty] private double _orgLeft = 80;
    [ObservableProperty] private double _orgTop = 100;
    [ObservableProperty] private double _orgWidth = 220;

    /// <summary>Заголовок «Квалификация» (Lazurski, над значением квалификации).</summary>
    [ObservableProperty] private double _qualLabelLeft = 80;
    [ObservableProperty] private double _qualLabelTop = 258;
    [ObservableProperty] private double _qualLabelWidth = 220;
    [ObservableProperty] private double _qualLabelFontSize = 11;

    [ObservableProperty] private double _qualLeft = 80;
    [ObservableProperty] private double _qualTop = 275;
    [ObservableProperty] private double _qualWidth = 220;

    [ObservableProperty] private double _regLeft = 108;
    [ObservableProperty] private double _regTop = 386;
    [ObservableProperty] private double _regWidth = 220;

    [ObservableProperty] private double _issueLeft = 108;
    [ObservableProperty] private double _issueTop = 468;
    [ObservableProperty] private double _issueWidth = 220;

    [ObservableProperty] private double _lastnameLeft = 430;
    [ObservableProperty] private double _lastnameTop = 55;
    [ObservableProperty] private double _lastnameWidth = 270;

    [ObservableProperty] private double _firstnameLeft = 430;
    [ObservableProperty] private double _firstnameTop = 80;
    [ObservableProperty] private double _firstnameWidth = 270;

    [ObservableProperty] private double _specialtyLeft = 430;
    [ObservableProperty] private double _specialtyTop = 145;
    [ObservableProperty] private double _specialtyWidth = 260;

    [ObservableProperty] private double _gecDecisionLeft = 430;
    [ObservableProperty] private double _gecDecisionTop = 200;
    [ObservableProperty] private double _gecDecisionWidth = 260;

    [ObservableProperty] private double _chairmanLeft = 408;
    [ObservableProperty] private double _chairmanTop = 384;
    [ObservableProperty] private double _chairmanWidth = 292;

    [ObservableProperty] private double _directorLeft = 408;
    [ObservableProperty] private double _directorTop = 436;
    [ObservableProperty] private double _directorWidth = 292;

    // Серия/номер бланка диплома (красные цифры)
    [ObservableProperty] private double _blankSeriesNumberLeft = 110;
    [ObservableProperty] private double _blankSeriesNumberTop = 335;
    [ObservableProperty] private double _blankSeriesNumberWidth = 240;

    /// <summary>Вертикальные полосы центровки на предпросмотре (X линии в пикселях, холст 730×520).</summary>
    [ObservableProperty] private double _centeringGuide1X = 190;
    [ObservableProperty] private double _centeringGuide2X = 560;

    /// <summary>QR защиты (левый нижний сектор бланка): левый верхний угол и сторона квадрата, px.</summary>
    [ObservableProperty] private double _qrLeft = 48;
    [ObservableProperty] private double _qrTop = 400;
    [ObservableProperty] private double _qrSize = 72;

    /// <summary>Надпись «Дубликат» (оборотная сторона).</summary>
    [ObservableProperty] private double _duplicateLeft = 220;
    [ObservableProperty] private double _duplicateTop = 28;
    [ObservableProperty] private double _duplicateWidth = 290;
    [ObservableProperty] private double _duplicateFontSize = 26;

    partial void OnDuplicateFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, 8, 48);
        if (Math.Abs(clamped - value) > 0.001)
            DuplicateFontSize = clamped;
    }

    partial void OnQualLabelFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, 8, 48);
        if (Math.Abs(clamped - value) > 0.001)
            QualLabelFontSize = clamped;
    }

    partial void OnChairmanWidthChanged(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 1)
            ChairmanWidth = 292;
    }

    partial void OnDirectorWidthChanged(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 1)
            DirectorWidth = 292;
    }
}
