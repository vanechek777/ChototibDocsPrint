using CommunityToolkit.Mvvm.ComponentModel;

namespace ChtotibDocsPrintNET.Models;

/// <summary>Координаты на предпросмотре приложения, оборот (холст 780×560 px).</summary>
public partial class AppendixBackOverlayLayout : ObservableObject
{
    [ObservableProperty] private double _gradesTableLeft = 16;
    [ObservableProperty] private double _gradesTableTop = 68;
    [ObservableProperty] private double _gradesTableWidth = 475;
    [ObservableProperty] private double _gradesTableHeight = 360;

    /// <summary>Учебные практики — правая половина разворота, блок «Учебная практика».</summary>
    [ObservableProperty] private double _studyPracticeListLeft = 402;
    [ObservableProperty] private double _studyPracticeListTop = 158;
    [ObservableProperty] private double _studyPracticeListWidth = 355;
    [ObservableProperty] private double _studyPracticeListHeight = 115;

    /// <summary>Производственные практики — правая половина, блок «Производственная практика».</summary>
    [ObservableProperty] private double _productionPracticeListLeft = 402;
    [ObservableProperty] private double _productionPracticeListTop = 348;
    [ObservableProperty] private double _productionPracticeListWidth = 355;
    [ObservableProperty] private double _productionPracticeListHeight = 115;

    /// <summary>Доля ширины хвоста (после предмета и зазора), отдаваемая колонке «часы»; остаток — «оценка». По умолчанию 70:(70+125).</summary>
    [ObservableProperty] private double _gradesHoursShareOfTail = 70.0 / (70 + 125);

    /// <summary>Пустой зазор в px между колонкой «часы» и «оценка» (расстояние от текста часов до текста оценки).</summary>
    [ObservableProperty] private double _gradesHoursGradeGapPx;

    /// <summary>Доля ширины таблицы учебных практик для колонки «виды деятельности».</summary>
    [ObservableProperty] private double _studyPracticeActivityShare = 0.18;

    /// <summary>Доля остатка (после «виды») для колонки «средства» в учебных практиках.</summary>
    [ObservableProperty] private double _studyPracticeMeansShareOfRemainder = 0.50 / (0.50 + 0.32);

    /// <summary>Зазор в px между столбцами таблицы учебных практик (между 1–2 и 2–3).</summary>
    [ObservableProperty] private double _studyPracticeColumnGapPx;

    /// <summary>Доля ширины таблицы производственных практик для колонки «виды деятельности».</summary>
    [ObservableProperty] private double _productionPracticeActivityShare = 0.18;

    /// <summary>Доля остатка (после «виды») для колонки «средства» в производственных практиках.</summary>
    [ObservableProperty] private double _productionPracticeMeansShareOfRemainder = 0.50 / (0.50 + 0.32);

    /// <summary>Зазор в px между столбцами таблицы производственных практик.</summary>
    [ObservableProperty] private double _productionPracticeColumnGapPx;

    /// <summary>Устаревшее общее поле (миграция из layout.json).</summary>
    [ObservableProperty] private double _practiceActivityShare = 0.18;

    /// <summary>Устаревшее общее поле (миграция из layout.json).</summary>
    [ObservableProperty] private double _practiceMeansShareOfRemainder = 0.50 / (0.50 + 0.32);

    [ObservableProperty] private double _emptyHintLeft = 100;
    [ObservableProperty] private double _emptyHintTop = 100;
    [ObservableProperty] private double _emptyHintWidth = 280;
    [ObservableProperty] private double _emptyHintHeight = 16;

    /// <summary>Номер страницы, левая половина разворота (оборот).</summary>
    [ObservableProperty] private double _pageNumBackLeft = 20;
    [ObservableProperty] private double _pageNumBackTop = 532;
    [ObservableProperty] private double _pageNumBackWidth = 130;
    [ObservableProperty] private double _pageNumBackHeight = 20;

    [ObservableProperty] private string _pageNumBackText = "3";

    /// <summary>Номер страницы, правая половина разворота (оборот).</summary>
    [ObservableProperty] private double _pageNumBackRightLeft = 595;
    [ObservableProperty] private double _pageNumBackRightTop = 532;
    [ObservableProperty] private double _pageNumBackRightWidth = 160;
    [ObservableProperty] private double _pageNumBackRightHeight = 20;

    [ObservableProperty] private string _pageNumBackRightText = "4";

    public void EnsurePageNumberBackDefaults()
    {
        if (PageNumBackWidth <= 0)
        {
            PageNumBackLeft = 20;
            PageNumBackTop = 532;
            PageNumBackWidth = 130;
            PageNumBackHeight = 20;
        }
        if (PageNumBackRightWidth <= 0)
        {
            PageNumBackRightLeft = 595;
            PageNumBackRightTop = 532;
            PageNumBackRightWidth = 160;
            PageNumBackRightHeight = 20;
        }
        if (string.IsNullOrWhiteSpace(PageNumBackText)) PageNumBackText = "3";
        else PageNumBackText = PageNumBackText.Trim();
        if (string.IsNullOrWhiteSpace(PageNumBackRightText)) PageNumBackRightText = "4";
        else PageNumBackRightText = PageNumBackRightText.Trim();
    }
}
