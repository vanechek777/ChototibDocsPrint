using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ChtotibDocsPrintNET.Models;

/// <summary>Координаты надписей на предпросмотре приложения, лицевая (холст 780×560 px).</summary>
public partial class AppendixFrontOverlayLayout : ObservableObject
{
    /// <summary>Серия и номер бланка диплома под «ПРИЛОЖЕНИЕ К ДИПЛОМУ» (правая половина разворота).</summary>
    [ObservableProperty] private double _diplomaBlankLeft = 418;
    [ObservableProperty] private double _diplomaBlankTop = 38;
    [ObservableProperty] private double _diplomaBlankWidth = 280;
    [ObservableProperty] private double _diplomaBlankHeight = 16;
    [ObservableProperty] private double _diplomaBlankFontSize = 9;

    [ObservableProperty] private double _courseTitleLeft = 22;
    [ObservableProperty] private double _courseTitleTop = 55;
    [ObservableProperty] private double _courseTitleWidth = 320;
    [ObservableProperty] private double _courseTitleHeight = 16;

    [ObservableProperty] private double _courseListLeft = 22;
    [ObservableProperty] private double _courseListTop = 75;
    [ObservableProperty] private double _courseListWidth = 380;
    [ObservableProperty] private double _courseListHeight = 200;
    /// <summary>Кегль строк курсовых (px предпросмотра).</summary>
    [ObservableProperty] private double _courseListFontSize = 7.5;
    /// <summary>Высота одной строки курсовых (px предпросмотра).</summary>
    [ObservableProperty] private double _courseListRowHeight = 10;

    [ObservableProperty] private double _orgLeft = 385;
    [ObservableProperty] private double _orgTop = 105;
    [ObservableProperty] private double _orgWidth = 160;
    [ObservableProperty] private double _orgHeight = 36;

    [ObservableProperty] private double _lastNameLeft = 575;
    [ObservableProperty] private double _lastNameTop = 63;
    [ObservableProperty] private double _lastNameWidth = 200;
    [ObservableProperty] private double _lastNameHeight = 14;

    [ObservableProperty] private double _firstNameLeft = 575;
    [ObservableProperty] private double _firstNameTop = 95;
    [ObservableProperty] private double _firstNameWidth = 200;
    [ObservableProperty] private double _firstNameHeight = 14;

    [ObservableProperty] private double _middleNameLeft = 575;
    [ObservableProperty] private double _middleNameTop = 130;
    [ObservableProperty] private double _middleNameWidth = 200;
    [ObservableProperty] private double _middleNameHeight = 14;

    [ObservableProperty] private double _birthLeft = 575;
    [ObservableProperty] private double _birthTop = 165;
    [ObservableProperty] private double _birthWidth = 200;
    [ObservableProperty] private double _birthHeight = 12;

    [ObservableProperty] private double _prevEducationLeft = 575;
    [ObservableProperty] private double _prevEducationTop = 200;
    [ObservableProperty] private double _prevEducationWidth = 180;
    [ObservableProperty] private double _prevEducationHeight = 40;

    [ObservableProperty] private double _studyPeriodLeft = 575;
    [ObservableProperty] private double _studyPeriodTop = 310;
    [ObservableProperty] private double _studyPeriodWidth = 200;
    [ObservableProperty] private double _studyPeriodHeight = 14;

    [ObservableProperty] private double _qualificationLeft = 575;
    [ObservableProperty] private double _qualificationTop = 345;
    [ObservableProperty] private double _qualificationWidth = 200;
    [ObservableProperty] private double _qualificationHeight = 14;

    [ObservableProperty] private double _specialtyLeft = 575;
    [ObservableProperty] private double _specialtyTop = 380;
    [ObservableProperty] private double _specialtyWidth = 180;
    [ObservableProperty] private double _specialtyHeight = 36;

    [ObservableProperty] private double _regNumberLeft = 430;
    [ObservableProperty] private double _regNumberTop = 380;
    [ObservableProperty] private double _regNumberWidth = 120;
    [ObservableProperty] private double _regNumberHeight = 14;

    [ObservableProperty] private double _issueDateLeft = 430;
    [ObservableProperty] private double _issueDateTop = 445;
    [ObservableProperty] private double _issueDateWidth = 120;
    [ObservableProperty] private double _issueDateHeight = 14;

    [ObservableProperty] private double _directorLeft = 85;
    [ObservableProperty] private double _directorTop = 460;
    [ObservableProperty] private double _directorWidth = 200;
    [ObservableProperty] private double _directorHeight = 14;

    /// <summary>Номер страницы, левая половина разворота (лицевая).</summary>
    [ObservableProperty] private double _pageNum1Left = 20;
    [ObservableProperty] private double _pageNum1Top = 532;
    [ObservableProperty] private double _pageNum1Width = 130;
    [ObservableProperty] private double _pageNum1Height = 20;

    /// <summary>Номер страницы, правая половина разворота (лицевая).</summary>
    [ObservableProperty] private double _pageNum2Left = 595;
    [ObservableProperty] private double _pageNum2Top = 532;
    [ObservableProperty] private double _pageNum2Width = 160;
    [ObservableProperty] private double _pageNum2Height = 20;

    [ObservableProperty] private string _pageNum1Text = "1";
    [ObservableProperty] private string _pageNum2Text = "2";

    /// <summary>Число в фразе «Настоящее приложение содержит … страниц» (левая половина разворота).</summary>
    [ObservableProperty] private double _pageCountLeft = 258;
    [ObservableProperty] private double _pageCountTop = 502;
    [ObservableProperty] private double _pageCountWidth = 40;
    [ObservableProperty] private double _pageCountHeight = 16;

    [ObservableProperty] private string _pageCountText = "4";

    /// <summary>Масштаб кегля текста на лицевой (1.0 — по умолчанию).</summary>
    [ObservableProperty] private double _fontSizeScale = 1.0;

    public double Fs8 => 8 * FontSizeScale;
    public double Fs9 => 9 * FontSizeScale;
    public double Fs10 => 10 * FontSizeScale;
    public double Fs11 => 11 * FontSizeScale;
    public double Fs12 => 12 * FontSizeScale;

    partial void OnCourseListFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, 5, 14);
        if (Math.Abs(clamped - value) > 0.001)
            CourseListFontSize = clamped;
    }

    partial void OnCourseListRowHeightChanged(double value)
    {
        var clamped = Math.Clamp(value, 8, 24);
        if (Math.Abs(clamped - value) > 0.001)
            CourseListRowHeight = clamped;
    }

    partial void OnDiplomaBlankFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, 7, 24);
        if (Math.Abs(clamped - value) > 0.001)
            DiplomaBlankFontSize = clamped;
    }

    partial void OnFontSizeScaleChanged(double value)
    {
        var clamped = Math.Clamp(value, 0.65, 1.35);
        if (Math.Abs(clamped - value) > 0.001)
        {
            FontSizeScale = clamped;
            return;
        }

        OnPropertyChanged(nameof(Fs8));
        OnPropertyChanged(nameof(Fs9));
        OnPropertyChanged(nameof(Fs10));
        OnPropertyChanged(nameof(Fs11));
        OnPropertyChanged(nameof(Fs12));
    }

    /// <summary>Если в сохранённом JSON не было блоков — выставить разумные координаты.</summary>
    public void EnsurePageNumberBlocksDefaults()
    {
        if (PageNum1Width <= 0)
        {
            PageNum1Left = 20;
            PageNum1Top = 532;
            PageNum1Width = 130;
            PageNum1Height = 20;
        }
        if (PageNum2Width <= 0)
        {
            PageNum2Left = 595;
            PageNum2Top = 532;
            PageNum2Width = 160;
            PageNum2Height = 20;
        }
        if (string.IsNullOrWhiteSpace(PageNum1Text)) PageNum1Text = "1";
        else PageNum1Text = PageNum1Text.Trim();
        if (string.IsNullOrWhiteSpace(PageNum2Text)) PageNum2Text = "2";
        else PageNum2Text = PageNum2Text.Trim();

        if (PageCountWidth <= 0)
        {
            PageCountLeft = 258;
            PageCountTop = 502;
            PageCountWidth = 40;
            PageCountHeight = 16;
        }
        if (string.IsNullOrWhiteSpace(PageCountText)) PageCountText = "4";
        else PageCountText = PageCountText.Trim();
    }
}
