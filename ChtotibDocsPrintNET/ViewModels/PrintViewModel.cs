using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChtotibDocsPrintNET.Converters;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class PrintViewModel : BaseViewModel
{
    [ObservableProperty] private Group? _selectedGroup;
    [ObservableProperty] private string _selectedDocumentType = "Полный комплект (PDF)";
    [ObservableProperty] private string _printTarget = "Всей группы";
    [ObservableProperty] private DateTime _issueDate = DateTime.Today;
    [ObservableProperty] private CommissionMember? _selectedChairman;
    [ObservableProperty] private int _totalDocuments;
    [ObservableProperty] private string _estimatedSize = "—";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showStudentsList;
    [ObservableProperty] private SelectableStudent? _previewStudent;
    [ObservableProperty] private int _currentPreviewStudentIndex;
    [ObservableProperty] private string _previewStudentLabel = "";
    [ObservableProperty] private string _issueDateRu = "";
    [ObservableProperty] private string _birthDateRu = "";
    /// <summary>Только строка «от …» для блока даты решения ГЭК (без заголовка).</summary>
    [ObservableProperty] private string _gecDecisionDateLine = "";
    [ObservableProperty] private string _chairmanShortName = "";
    [ObservableProperty] private string _directorShortName = "";
    /// <summary>Серия и номер бланка для лицевой стороны («137704 0001144»).</summary>
    [ObservableProperty] private string _diplomaBlankSeriesNumberLine = "";

    private string _specialtyQualificationFallback = "";

    // Specialty info
    [ObservableProperty] private string _qualificationText = "";
    [ObservableProperty] private string _specialtyCodeName = "";
    [ObservableProperty] private string _studyPeriod = "";
    [ObservableProperty] private string _studyForm = "Очная";
    [ObservableProperty] private string _orgName = "ЧТОТИБ";
    [ObservableProperty] private string _directorName = "";
    [ObservableProperty] private string _previousEducation = "";

    // Grades
    public ObservableCollection<Grade> StudentGrades { get; } = new();
    public ObservableCollection<AppendixPracticeRow> StudyPracticeGrades { get; } = new();
    public ObservableCollection<AppendixPracticeRow> ProductionPracticeGrades { get; } = new();
    public ObservableCollection<Grade> CourseworkGrades { get; } = new();

    // Template images
    [ObservableProperty] private BitmapImage? _diplomaFrontImage;
    [ObservableProperty] private BitmapImage? _diplomaBackImage;
    [ObservableProperty] private BitmapImage? _prilojenieFrontImage;
    [ObservableProperty] private BitmapImage? _prilojenieBackImage;
    /// <summary>QR на обороте диплома (предпросмотр). BitmapSource с Dpi 96×96 — иначе вложенный DPI PNG даёт смещение в Border.</summary>
    [ObservableProperty] private BitmapSource? _diplomaBackQrImage;

    /// <summary>Устаревшее поле в layout.json; QR формируется автоматически из данных демоэкзамена и даты выдачи.</summary>
    [ObservableProperty] private string _diplomaQrLink = "";

    [ObservableProperty] private bool _printDrawBackground = true;
    [ObservableProperty] private bool _printIsDuplicate;
    [ObservableProperty] private bool _printQualificationLabel;
    [ObservableProperty] private bool _isPdfGenerating;
    [ObservableProperty] private double _pdfProgress;
    [ObservableProperty] private string _pdfProgressText = "";
    [ObservableProperty] private string _previewQrPayload = "";

    // Lazurski font
    [ObservableProperty] private FontFamily _lazurskiFont = new("Times New Roman");
    [ObservableProperty] private double _previewZoom = 1.0;
    [ObservableProperty] private bool _fitPreviewToFrame = true;

    /// <summary>Видимая область предпросмотра диплома после кроп-линеек (пиксели холста 730×520).</summary>
    [ObservableProperty] private Rect _diplomaPreviewCropClipRect;

    /// <summary>Кроп приложений 780×560.</summary>
    [ObservableProperty] private Rect _appendixPreviewCropClipRect;

    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<string> DocumentTypes { get; } = new()
    {
        "Полный комплект (PDF)",
        "Диплом (лицевая)",
        "Диплом (обратная сторона)",
        "Приложение (лицевая)",
        "Приложение (оборотная)",
    };
    public ObservableCollection<string> PrintTargets { get; } = new() { "Всей группы", "Только без напечатанных", "Выбранные студенты" };
    public ObservableCollection<CommissionMember> Chairmen { get; } = new();
    public ObservableCollection<SelectableStudent> Students { get; } = new();

    /// <summary>Координаты надписей на предпросмотре лицевой стороны диплома.</summary>
    [ObservableProperty] private DiplomaFrontOverlayLayout _diplomaFrontOverlay = new();

    /// <summary>Координаты надписей на предпросмотре оборота диплома.</summary>
    [ObservableProperty] private DiplomaBackOverlayLayout _diplomaBackOverlay = new();

    /// <summary>Координаты блоков на предпросмотре приложения (лицевая).</summary>
    [ObservableProperty] private AppendixFrontOverlayLayout _appendixFrontOverlay = new();

    /// <summary>Координаты блоков на предпросмотре приложения (оборот).</summary>
    [ObservableProperty] private AppendixBackOverlayLayout _appendixBackOverlay = new();

    private readonly PreviewCropInsets _previewCropDiplomaFront;
    private readonly PreviewCropInsets _previewCropDiplomaBack;
    private readonly PreviewCropInsets _previewCropAppendix;
    private bool _suppressLayoutSave;
    private bool _suppressQrPayloadSync;
    private DiplomaFrontOverlayLayout? _diplomaFrontAutosaveSubscription;
    private DiplomaBackOverlayLayout? _overlayAutosaveSubscription;
    private AppendixFrontOverlayLayout? _appendixFrontAutosaveSubscription;
    private AppendixBackOverlayLayout? _appendixBackAutosaveSubscription;
    private DispatcherTimer? _overlayAutosaveDebounce;

    /// <summary>Раскладка обычного (синего) диплома.</summary>
    private DiplomaVariantLayoutPersisted _standardDiplomaProfile = new();

    /// <summary>Раскладка диплома с отличием (красный бланк).</summary>
    private DiplomaVariantLayoutPersisted _honorDiplomaProfile = new();

    private bool _activeDiplomaProfileIsHonor;

    private static readonly CultureInfo RuCulture = new("ru-RU");

    private static readonly JsonSerializerOptions PrintLayoutJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Отступы кропа предпросмотра диплома (лицевая, 730×520).</summary>
    public PreviewCropInsets PreviewCropDiplomaFront => _previewCropDiplomaFront;

    /// <summary>Отступы кропа предпросмотра диплома (оборот, 730×520).</summary>
    public PreviewCropInsets PreviewCropDiplomaBack => _previewCropDiplomaBack;

    /// <summary>Кроп приложения (лицевой и оборот, 780×560).</summary>
    public PreviewCropInsets PreviewCropAppendix => _previewCropAppendix;

    public double DiplomaFrontCropRulerLeftX => _previewCropDiplomaFront.InsetLeft;
    public double DiplomaFrontCropRulerRightX => 730 - _previewCropDiplomaFront.InsetRight;
    public double DiplomaFrontCropRulerTopY => _previewCropDiplomaFront.InsetTop;
    public double DiplomaFrontCropRulerBottomY => 520 - _previewCropDiplomaFront.InsetBottom;

    public double DiplomaBackCropRulerLeftX => _previewCropDiplomaBack.InsetLeft;
    public double DiplomaBackCropRulerRightX => 730 - _previewCropDiplomaBack.InsetRight;
    public double DiplomaBackCropRulerTopY => _previewCropDiplomaBack.InsetTop;
    public double DiplomaBackCropRulerBottomY => 520 - _previewCropDiplomaBack.InsetBottom;

    public double AppendixCropRulerLeftX => _previewCropAppendix.InsetLeft;
    public double AppendixCropRulerRightX => 780 - _previewCropAppendix.InsetRight;
    public double AppendixCropRulerTopY => _previewCropAppendix.InsetTop;
    public double AppendixCropRulerBottomY => 560 - _previewCropAppendix.InsetBottom;

    /// <summary>Направляющие центровки, приложение — лицевая.</summary>
    [ObservableProperty] private double _appendixFrontCenteringGuide1X = 203;

    [ObservableProperty] private double _appendixFrontCenteringGuide2X = 598;

    /// <summary>Направляющие центровки, приложение — оборотная.</summary>
    [ObservableProperty] private double _appendixCenteringGuide1X = 203;

    [ObservableProperty] private double _appendixCenteringGuide2X = 598;

    [ObservableProperty] private bool _appendixShowPageNumbers = true;

    private List<SelectableStudent> PreviewStudents => PrintTarget == "Выбранные студенты"
        ? Students.Where(s => s.IsSelected).ToList()
        : Students.ToList();

    public PrintViewModel()
    {
        _previewCropDiplomaFront = new PreviewCropInsets(OnPreviewCropInsetsChanged);
        _previewCropDiplomaBack = new PreviewCropInsets(OnPreviewCropInsetsChanged);
        _previewCropAppendix = new PreviewCropInsets(OnPreviewCropInsetsChanged);
        LoadLazurskiFont();
        LoadTemplateImages();
        LoadPrintLayoutFromDisk();
        LoadOrgSettings();
        LoadData();
        UpdateIssueDateRu();
        RefreshOfficialShortNames();
    }

    partial void OnDiplomaFrontOverlayChanged(DiplomaFrontOverlayLayout? oldValue, DiplomaFrontOverlayLayout newValue)
    {
        SubscribeDiplomaFrontOverlayAutosave(newValue);
    }

    partial void OnDiplomaBackOverlayChanged(DiplomaBackOverlayLayout? oldValue, DiplomaBackOverlayLayout newValue)
    {
        SubscribeOverlayAutosave(newValue);
    }

    partial void OnAppendixFrontOverlayChanged(AppendixFrontOverlayLayout? oldValue, AppendixFrontOverlayLayout newValue)
    {
        SubscribeAppendixFrontAutosave(newValue);
    }

    partial void OnAppendixBackOverlayChanged(AppendixBackOverlayLayout? oldValue, AppendixBackOverlayLayout newValue)
    {
        SubscribeAppendixBackAutosave(newValue);
    }

    partial void OnAppendixShowPageNumbersChanged(bool value) => SavePrintLayoutSilent();

    partial void OnFitPreviewToFrameChanged(bool value)
    {
        if (value)
            PreviewZoom = 1.0;
    }

    private void OnPreviewCropInsetsChanged() => RefreshPreviewCropLayouts();

    private void RefreshPreviewCropLayouts()
    {
        const double minInner = 40;
        _previewCropDiplomaFront.ClampToCanvas(730, 520, minInner);
        _previewCropDiplomaBack.ClampToCanvas(730, 520, minInner);
        _previewCropAppendix.ClampToCanvas(780, 560, minInner);

        DiplomaFrontPreviewCropClipRect = new Rect(
            _previewCropDiplomaFront.InsetLeft,
            _previewCropDiplomaFront.InsetTop,
            Math.Max(1, 730 - _previewCropDiplomaFront.InsetLeft - _previewCropDiplomaFront.InsetRight),
            Math.Max(1, 520 - _previewCropDiplomaFront.InsetTop - _previewCropDiplomaFront.InsetBottom));

        DiplomaBackPreviewCropClipRect = new Rect(
            _previewCropDiplomaBack.InsetLeft,
            _previewCropDiplomaBack.InsetTop,
            Math.Max(1, 730 - _previewCropDiplomaBack.InsetLeft - _previewCropDiplomaBack.InsetRight),
            Math.Max(1, 520 - _previewCropDiplomaBack.InsetTop - _previewCropDiplomaBack.InsetBottom));

        AppendixPreviewCropClipRect = new Rect(
            _previewCropAppendix.InsetLeft,
            _previewCropAppendix.InsetTop,
            Math.Max(1, 780 - _previewCropAppendix.InsetLeft - _previewCropAppendix.InsetRight),
            Math.Max(1, 560 - _previewCropAppendix.InsetTop - _previewCropAppendix.InsetBottom));

        OnPropertyChanged(nameof(DiplomaFrontCropRulerLeftX));
        OnPropertyChanged(nameof(DiplomaFrontCropRulerRightX));
        OnPropertyChanged(nameof(DiplomaFrontCropRulerTopY));
        OnPropertyChanged(nameof(DiplomaFrontCropRulerBottomY));
        OnPropertyChanged(nameof(DiplomaBackCropRulerLeftX));
        OnPropertyChanged(nameof(DiplomaBackCropRulerRightX));
        OnPropertyChanged(nameof(DiplomaBackCropRulerTopY));
        OnPropertyChanged(nameof(DiplomaBackCropRulerBottomY));
        OnPropertyChanged(nameof(AppendixCropRulerLeftX));
        OnPropertyChanged(nameof(AppendixCropRulerRightX));
        OnPropertyChanged(nameof(AppendixCropRulerTopY));
        OnPropertyChanged(nameof(AppendixCropRulerBottomY));
    }

    [ObservableProperty] private Rect _diplomaFrontPreviewCropClipRect;
    [ObservableProperty] private Rect _diplomaBackPreviewCropClipRect;

    private void SubscribeDiplomaFrontOverlayAutosave(DiplomaFrontOverlayLayout overlay)
    {
        if (ReferenceEquals(_diplomaFrontAutosaveSubscription, overlay)) return;
        if (_diplomaFrontAutosaveSubscription != null)
            _diplomaFrontAutosaveSubscription.PropertyChanged -= OnDiplomaFrontOverlayPropertyChanged;
        _diplomaFrontAutosaveSubscription = overlay;
        overlay.PropertyChanged += OnDiplomaFrontOverlayPropertyChanged;
    }

    private void OnDiplomaFrontOverlayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressLayoutSave) return;
        StartOverlayAutosaveDebounce();
    }

    private void SubscribeOverlayAutosave(DiplomaBackOverlayLayout overlay)
    {
        if (ReferenceEquals(_overlayAutosaveSubscription, overlay)) return;
        if (_overlayAutosaveSubscription != null)
            _overlayAutosaveSubscription.PropertyChanged -= OnDiplomaBackOverlayPropertyChanged;
        _overlayAutosaveSubscription = overlay;
        if (overlay != null)
            overlay.PropertyChanged += OnDiplomaBackOverlayPropertyChanged;
    }

    private void OnDiplomaBackOverlayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressLayoutSave) return;
        RefreshDiplomaBackQrPreview();
        StartOverlayAutosaveDebounce();
    }

    private void OnAppendixOverlayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressLayoutSave) return;
        StartOverlayAutosaveDebounce();
    }

    private void StartOverlayAutosaveDebounce()
    {
        _overlayAutosaveDebounce ??= new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(450),
        };
        _overlayAutosaveDebounce.Stop();
        _overlayAutosaveDebounce.Tick -= OnOverlayAutosaveDebounceTick;
        _overlayAutosaveDebounce.Tick += OnOverlayAutosaveDebounceTick;
        _overlayAutosaveDebounce.Start();
    }

    private void SubscribeAppendixFrontAutosave(AppendixFrontOverlayLayout overlay)
    {
        if (ReferenceEquals(_appendixFrontAutosaveSubscription, overlay)) return;
        if (_appendixFrontAutosaveSubscription != null)
            _appendixFrontAutosaveSubscription.PropertyChanged -= OnAppendixOverlayPropertyChanged;
        _appendixFrontAutosaveSubscription = overlay;
        overlay.PropertyChanged += OnAppendixOverlayPropertyChanged;
    }

    private void SubscribeAppendixBackAutosave(AppendixBackOverlayLayout overlay)
    {
        if (ReferenceEquals(_appendixBackAutosaveSubscription, overlay)) return;
        if (_appendixBackAutosaveSubscription != null)
            _appendixBackAutosaveSubscription.PropertyChanged -= OnAppendixOverlayPropertyChanged;
        _appendixBackAutosaveSubscription = overlay;
        overlay.PropertyChanged += OnAppendixOverlayPropertyChanged;
    }

    private void OnOverlayAutosaveDebounceTick(object? sender, EventArgs e)
    {
        if (_overlayAutosaveDebounce != null)
        {
            _overlayAutosaveDebounce.Stop();
            _overlayAutosaveDebounce.Tick -= OnOverlayAutosaveDebounceTick;
        }
        SavePrintLayoutSilent();
    }

    /// <summary>После ручного ввода полей кропа (LostFocus сохранения в JSON).</summary>
    public void SaveCropInsetsAfterEditorEdit() => SavePrintLayoutSilent();

    partial void OnAppendixFrontCenteringGuide1XChanged(double value) => SavePrintLayoutSilent();

    partial void OnAppendixFrontCenteringGuide2XChanged(double value) => SavePrintLayoutSilent();

    partial void OnAppendixCenteringGuide1XChanged(double value) => SavePrintLayoutSilent();

    partial void OnAppendixCenteringGuide2XChanged(double value) => SavePrintLayoutSilent();

    [RelayCommand]
    private void ResetPreviewCropDiploma()
    {
        _previewCropDiplomaFront.ApplyPersisted(new PreviewCropPersisted());
        _previewCropDiplomaBack.ApplyPersisted(new PreviewCropPersisted());
        SavePrintLayoutSilent();
    }

    [RelayCommand]
    private void ResetPreviewCropAppendix()
    {
        _previewCropAppendix.ApplyPersisted(new PreviewCropPersisted());
        SavePrintLayoutSilent();
    }

    partial void OnSelectedChairmanChanged(CommissionMember? value) => RefreshOfficialShortNames();

    partial void OnDirectorNameChanged(string value) => RefreshOfficialShortNames();

    private void RefreshOfficialShortNames()
    {
        ChairmanShortName = PersonNameFormatter.ToShortRussianOfficial(SelectedChairman?.FullName);
        DirectorShortName = PersonNameFormatter.ToShortRussianOfficial(DirectorName);
    }

    partial void OnPreviewStudentChanged(SelectableStudent? value)
    {
        ApplyQualificationForPreviewStudent();
        RefreshDiplomaBlankLine();
    }

    private void ApplyQualificationForPreviewStudent()
    {
        Specialty? sp = null;
        if (SelectedGroup != null)
        {
            try { sp = DatabaseService.Instance.GetSpecialtyById(SelectedGroup.SpecialtyId); }
            catch { /* ignore */ }
        }

        QualificationText = QualificationResolver.Resolve(PreviewStudent?.Qualification, sp);
        _specialtyQualificationFallback = QualificationResolver.Resolve(null, sp);
    }

    private void RefreshDiplomaBlankLine()
    {
        if (PreviewStudent == null)
        {
            DiplomaBlankSeriesNumberLine = "";
            return;
        }

        var ds = (PreviewStudent.DiplomaSeries ?? "").Trim();
        var dn = (PreviewStudent.DiplomaNumber ?? "").Trim();
        DiplomaBlankSeriesNumberLine = string.Join(" ", new[] { ds, dn }.Where(static x => x.Length > 0));
    }

    /// <summary>Перемещает блок на предпросмотре лицевой стороны диплома (730×520).</summary>
    public void SetDiplomaFrontOverlayPosition(string overlayKey, double left, double top)
    {
        const double cw = 730, ch = 520;
        left = Math.Clamp(left, 0, cw - 4);
        top = Math.Clamp(top, 0, ch - 4);
        var o = DiplomaFrontOverlay;
        if (overlayKey == "Dup")
        {
            o.DuplicateLeft = left;
            o.DuplicateTop = top;
        }
    }

    /// <summary>Перемещает блок на предпросмотре оборота (px, без выхода за холст 730×520).</summary>
    public void SetDiplomaBackOverlayPosition(string overlayKey, double left, double top)
    {
        const double cw = 730, ch = 520;
        left = Math.Clamp(left, 0, cw - 4);
        top = Math.Clamp(top, 0, ch - 4);
        var o = DiplomaBackOverlay;
        switch (overlayKey)
        {
            case "Org": o.OrgLeft = left; o.OrgTop = top; break;
            case "QualLabel": o.QualLabelLeft = left; o.QualLabelTop = top; break;
            case "Qual": o.QualLeft = left; o.QualTop = top; break;
            case "Blank": o.BlankSeriesNumberLeft = left; o.BlankSeriesNumberTop = top; break;
            case "Reg": o.RegLeft = left; o.RegTop = top; break;
            case "Issue": o.IssueLeft = left; o.IssueTop = top; break;
            case "Last": o.LastnameLeft = left; o.LastnameTop = top; break;
            case "Given": o.FirstnameLeft = left; o.FirstnameTop = top; break;
            case "Spec": o.SpecialtyLeft = left; o.SpecialtyTop = top; break;
            case "Gec": o.GecDecisionLeft = left; o.GecDecisionTop = top; break;
            case "Chair": o.ChairmanLeft = left; o.ChairmanTop = top; break;
            case "Dir": o.DirectorLeft = left; o.DirectorTop = top; break;
            case "Qr": o.QrLeft = left; o.QrTop = top; break;
            case "Dup": o.DuplicateLeft = left; o.DuplicateTop = top; break;
        }
    }

    /// <summary>Перемещает блок на предпросмотре приложения, лицевая (780×560).</summary>
    public void SetAppendixFrontOverlayPosition(string overlayKey, double left, double top)
    {
        const double cw = 780, ch = 560;
        left = Math.Clamp(left, 0, cw - 4);
        top = Math.Clamp(top, 0, ch - 4);
        var o = AppendixFrontOverlay;
        switch (overlayKey)
        {
            case "DipBlank": o.DiplomaBlankLeft = left; o.DiplomaBlankTop = top; break;
            case "CourseTitle": o.CourseTitleLeft = left; o.CourseTitleTop = top; break;
            case "CourseList": o.CourseListLeft = left; o.CourseListTop = top; break;
            case "Org": o.OrgLeft = left; o.OrgTop = top; break;
            case "Last": o.LastNameLeft = left; o.LastNameTop = top; break;
            case "First": o.FirstNameLeft = left; o.FirstNameTop = top; break;
            case "Middle": o.MiddleNameLeft = left; o.MiddleNameTop = top; break;
            case "Birth": o.BirthLeft = left; o.BirthTop = top; break;
            case "PrevEd": o.PrevEducationLeft = left; o.PrevEducationTop = top; break;
            case "Study": o.StudyPeriodLeft = left; o.StudyPeriodTop = top; break;
            case "Qual": o.QualificationLeft = left; o.QualificationTop = top; break;
            case "Spec": o.SpecialtyLeft = left; o.SpecialtyTop = top; break;
            case "Reg": o.RegNumberLeft = left; o.RegNumberTop = top; break;
            case "Issue": o.IssueDateLeft = left; o.IssueDateTop = top; break;
            case "Dir": o.DirectorLeft = left; o.DirectorTop = top; break;
            case "PageNum1": o.PageNum1Left = left; o.PageNum1Top = top; break;
            case "PageNum2": o.PageNum2Left = left; o.PageNum2Top = top; break;
            case "PageCount": o.PageCountLeft = left; o.PageCountTop = top; break;
        }
    }

    public double GetAppendixFrontOverlayCurrentWidth(string overlayKey) =>
        GetAppendixFrontOverlayBox(AppendixFrontOverlay, overlayKey).width;

    public double GetAppendixFrontOverlayCurrentHeight(string overlayKey) =>
        GetAppendixFrontOverlayBox(AppendixFrontOverlay, overlayKey).height;

    public (double left, double top) GetAppendixFrontOverlayPosition(string overlayKey)
    {
        var (l, t, _, _) = GetAppendixFrontOverlayBox(AppendixFrontOverlay, overlayKey);
        return (l, t);
    }

    public (double left, double top) GetAppendixBackOverlayPosition(string overlayKey)
    {
        var (l, t, _, _) = GetAppendixBackOverlayBox(AppendixBackOverlay, overlayKey);
        return (l, t);
    }

    /// <summary>Правый край блока: левый край без изменений, ширина в пределах холста 780.</summary>
    public void SetAppendixFrontOverlayWidth(string overlayKey, double width)
    {
        const double minW = 40;
        const double cw = 780;
        var o = AppendixFrontOverlay;
        var (left, _, _, _) = GetAppendixFrontOverlayBox(o, overlayKey);
        var w = Math.Clamp(width, minW, Math.Max(minW, cw - left));

        switch (overlayKey)
        {
            case "DipBlank": o.DiplomaBlankWidth = w; break;
            case "CourseTitle": o.CourseTitleWidth = w; break;
            case "CourseList": o.CourseListWidth = w; break;
            case "Org": o.OrgWidth = w; break;
            case "Last": o.LastNameWidth = w; break;
            case "First": o.FirstNameWidth = w; break;
            case "Middle": o.MiddleNameWidth = w; break;
            case "Birth": o.BirthWidth = w; break;
            case "PrevEd": o.PrevEducationWidth = w; break;
            case "Study": o.StudyPeriodWidth = w; break;
            case "Qual": o.QualificationWidth = w; break;
            case "Spec": o.SpecialtyWidth = w; break;
            case "Reg": o.RegNumberWidth = w; break;
            case "Issue": o.IssueDateWidth = w; break;
            case "Dir": o.DirectorWidth = w; break;
            case "PageNum1": o.PageNum1Width = w; break;
            case "PageNum2": o.PageNum2Width = w; break;
            case "PageCount": o.PageCountWidth = w; break;
        }
    }

    /// <summary>Нижний край блока: верх без изменений, высота в пределах холста 560.</summary>
    public void SetAppendixFrontOverlayHeight(string overlayKey, double height)
    {
        const double ch = 560;
        var o = AppendixFrontOverlay;
        var (_, top, _, _) = GetAppendixFrontOverlayBox(o, overlayKey);
        var minH = MinAppendixFrontOverlayHeight(overlayKey);
        var h = Math.Clamp(height, minH, Math.Max(minH, ch - top));

        switch (overlayKey)
        {
            case "DipBlank": o.DiplomaBlankHeight = h; break;
            case "CourseTitle": o.CourseTitleHeight = h; break;
            case "CourseList": o.CourseListHeight = h; break;
            case "Org": o.OrgHeight = h; break;
            case "Last": o.LastNameHeight = h; break;
            case "First": o.FirstNameHeight = h; break;
            case "Middle": o.MiddleNameHeight = h; break;
            case "Birth": o.BirthHeight = h; break;
            case "PrevEd": o.PrevEducationHeight = h; break;
            case "Study": o.StudyPeriodHeight = h; break;
            case "Qual": o.QualificationHeight = h; break;
            case "Spec": o.SpecialtyHeight = h; break;
            case "Reg": o.RegNumberHeight = h; break;
            case "Issue": o.IssueDateHeight = h; break;
            case "Dir": o.DirectorHeight = h; break;
            case "PageNum1": o.PageNum1Height = h; break;
            case "PageNum2": o.PageNum2Height = h; break;
            case "PageCount": o.PageCountHeight = h; break;
        }
    }

    public double GetAppendixBackOverlayCurrentWidth(string overlayKey) =>
        GetAppendixBackOverlayBox(AppendixBackOverlay, overlayKey).width;

    public double GetAppendixBackOverlayCurrentHeight(string overlayKey) =>
        GetAppendixBackOverlayBox(AppendixBackOverlay, overlayKey).height;

    public void SetAppendixBackOverlayWidth(string overlayKey, double width)
    {
        const double minW = 40;
        const double cw = 780;
        var o = AppendixBackOverlay;
        var (left, _, _, _) = GetAppendixBackOverlayBox(o, overlayKey);
        var w = Math.Clamp(width, minW, Math.Max(minW, cw - left));

        switch (overlayKey)
        {
            case "Grades":
                o.GradesTableWidth = w;
                ApplyAppendixGradesHoursGradeGapPx(o.GradesHoursGradeGapPx);
                break;
            case "StudyPractice": o.StudyPracticeListWidth = w; break;
            case "ProductionPractice": o.ProductionPracticeListWidth = w; break;
            case "EmptyHint": o.EmptyHintWidth = w; break;
            case "PageNumBack": o.PageNumBackWidth = w; break;
            case "PageNumBackRight": o.PageNumBackRightWidth = w; break;
        }
    }

    public void SetAppendixBackOverlayHeight(string overlayKey, double height)
    {
        const double ch = 560;
        var o = AppendixBackOverlay;
        var (_, top, _, _) = GetAppendixBackOverlayBox(o, overlayKey);
        var minH = overlayKey is "Grades" or "StudyPractice" or "ProductionPractice" ? 40.0 : 12.0;
        var h = Math.Clamp(height, minH, Math.Max(minH, ch - top));

        switch (overlayKey)
        {
            case "Grades": o.GradesTableHeight = h; break;
            case "StudyPractice": o.StudyPracticeListHeight = h; break;
            case "ProductionPractice": o.ProductionPracticeListHeight = h; break;
            case "EmptyHint": o.EmptyHintHeight = h; break;
            case "PageNumBack": o.PageNumBackHeight = h; break;
            case "PageNumBackRight": o.PageNumBackRightHeight = h; break;
        }
    }

    /// <summary>Зазор в px между колонками «часы» и «оценка»; поджимается под текущую ширину таблицы.</summary>
    public void ApplyAppendixGradesHoursGradeGapPx(double gapPx)
    {
        var o = AppendixBackOverlay;
        var tail = Math.Max(0, o.GradesTableWidth - AppendixGradesTailColumnWidthMultiConverter.GradesSubjectColWidth(o.GradesTableWidth));
        o.GradesHoursGradeGapPx = AppendixGradesTailColumnWidthMultiConverter.ClampGap(gapPx, tail);
    }

    public void ApplyStudyPracticeActivityShare(double share) =>
        AppendixBackOverlay.StudyPracticeActivityShare =
            AppendixPracticeColumnWidthMultiConverter.ClampActivityShare(share);

    public void ApplyProductionPracticeActivityShare(double share) =>
        AppendixBackOverlay.ProductionPracticeActivityShare =
            AppendixPracticeColumnWidthMultiConverter.ClampActivityShare(share);

    public void ApplyStudyPracticeMeansShareOfRemainder(double share) =>
        AppendixBackOverlay.StudyPracticeMeansShareOfRemainder =
            AppendixPracticeColumnWidthMultiConverter.ClampMeansShare(share);

    public void ApplyProductionPracticeMeansShareOfRemainder(double share) =>
        AppendixBackOverlay.ProductionPracticeMeansShareOfRemainder =
            AppendixPracticeColumnWidthMultiConverter.ClampMeansShare(share);

    public void ApplyStudyPracticeColumnGapPx(double gapPx)
    {
        var o = AppendixBackOverlay;
        o.StudyPracticeColumnGapPx = AppendixPracticeColumnWidthMultiConverter.ClampGap(
            gapPx, o.StudyPracticeListWidth);
    }

    public void ApplyProductionPracticeColumnGapPx(double gapPx)
    {
        var o = AppendixBackOverlay;
        o.ProductionPracticeColumnGapPx = AppendixPracticeColumnWidthMultiConverter.ClampGap(
            gapPx, o.ProductionPracticeListWidth);
    }

    private static double MinAppendixFrontOverlayHeight(string overlayKey) => overlayKey switch
    {
        "CourseList" => 60,
        "Org" or "Spec" or "PrevEd" => 20,
        "PageNum1" or "PageNum2" or "PageCount" => 12,
        _ => 12,
    };

    /// <summary>Перемещает блок на предпросмотре приложения, оборот.</summary>
    public void SetAppendixBackOverlayPosition(string overlayKey, double left, double top)
    {
        const double cw = 780, ch = 560;
        left = Math.Clamp(left, 0, cw - 4);
        top = Math.Clamp(top, 0, ch - 4);
        var o = AppendixBackOverlay;
        switch (overlayKey)
        {
            case "Grades": o.GradesTableLeft = left; o.GradesTableTop = top; break;
            case "StudyPractice": o.StudyPracticeListLeft = left; o.StudyPracticeListTop = top; break;
            case "ProductionPractice": o.ProductionPracticeListLeft = left; o.ProductionPracticeListTop = top; break;
            case "EmptyHint": o.EmptyHintLeft = left; o.EmptyHintTop = top; break;
            case "PageNumBack": o.PageNumBackLeft = left; o.PageNumBackTop = top; break;
            case "PageNumBackRight": o.PageNumBackRightLeft = left; o.PageNumBackRightTop = top; break;
        }
    }

    public void SnapAppendixFrontOverlayToGuides(string overlayKey, double renderedWidth)
    {
        const double snapPx = 10;
        const double canvasW = 780;
        var o = AppendixFrontOverlay;
        var (left, top, wLayout, _) = GetAppendixFrontOverlayBox(o, overlayKey);
        var w = wLayout;
        if (!double.IsNaN(renderedWidth) && renderedWidth > 0)
            w = Math.Max(w, renderedWidth);
        w = Math.Max(w, 8);

        var leftEdge = left;
        var centerX = left + w * 0.5;
        var rightEdge = left + w;

        SnapAppendixOverlayToGuides(
            left, top, w, canvasW, snapPx,
            AppendixFrontCenteringGuide1X, AppendixFrontCenteringGuide2X,
            (l, t) => SetAppendixFrontOverlayPosition(overlayKey, l, t));
    }

    public void SnapAppendixBackOverlayToGuides(string overlayKey, double renderedWidth)
    {
        const double snapPx = 10;
        const double canvasW = 780;
        var o = AppendixBackOverlay;
        var (left, top, wLayout, _) = GetAppendixBackOverlayBox(o, overlayKey);
        var w = wLayout;
        if (!double.IsNaN(renderedWidth) && renderedWidth > 0)
            w = Math.Max(w, renderedWidth);
        w = Math.Max(w, 8);

        SnapAppendixOverlayToGuides(
            left, top, w, canvasW, snapPx,
            AppendixCenteringGuide1X, AppendixCenteringGuide2X,
            (l, t) => SetAppendixBackOverlayPosition(overlayKey, l, t));
    }

    private static void SnapAppendixOverlayToGuides(
        double left,
        double top,
        double w,
        double canvasW,
        double snapPx,
        double guide1X,
        double guide2X,
        Action<double, double> setPosition)
    {
        var leftEdge = left;
        var centerX = left + w * 0.5;
        var rightEdge = left + w;

        var guides = new[] { guide1X, guide2X };
        double bestDist = snapPx + 1;
        double bestGuideX = 0;
        int bestWhich = -1;

        foreach (var g in guides)
        {
            var dL = Math.Abs(leftEdge - g);
            if (dL < bestDist) { bestDist = dL; bestGuideX = g; bestWhich = 0; }
            var dC = Math.Abs(centerX - g);
            if (dC < bestDist) { bestDist = dC; bestGuideX = g; bestWhich = 1; }
            var dR = Math.Abs(rightEdge - g);
            if (dR < bestDist) { bestDist = dR; bestGuideX = g; bestWhich = 2; }
        }

        if (bestWhich < 0 || bestDist > snapPx) return;

        var newLeft = bestWhich switch
        {
            0 => bestGuideX,
            1 => bestGuideX - w * 0.5,
            _ => bestGuideX - w,
        };
        newLeft = Math.Clamp(newLeft, 0, Math.Max(0, canvasW - w));
        setPosition(newLeft, top);
    }

    private static (double left, double top, double width, double height) GetAppendixFrontOverlayBox(AppendixFrontOverlayLayout o, string key) => key switch
    {
        "DipBlank" => (o.DiplomaBlankLeft, o.DiplomaBlankTop, o.DiplomaBlankWidth, o.DiplomaBlankHeight),
        "CourseTitle" => (o.CourseTitleLeft, o.CourseTitleTop, o.CourseTitleWidth, o.CourseTitleHeight),
        "CourseList" => (o.CourseListLeft, o.CourseListTop, o.CourseListWidth, o.CourseListHeight),
        "Org" => (o.OrgLeft, o.OrgTop, o.OrgWidth, o.OrgHeight),
        "Last" => (o.LastNameLeft, o.LastNameTop, o.LastNameWidth, o.LastNameHeight),
        "First" => (o.FirstNameLeft, o.FirstNameTop, o.FirstNameWidth, o.FirstNameHeight),
        "Middle" => (o.MiddleNameLeft, o.MiddleNameTop, o.MiddleNameWidth, o.MiddleNameHeight),
        "Birth" => (o.BirthLeft, o.BirthTop, o.BirthWidth, o.BirthHeight),
        "PrevEd" => (o.PrevEducationLeft, o.PrevEducationTop, o.PrevEducationWidth, o.PrevEducationHeight),
        "Study" => (o.StudyPeriodLeft, o.StudyPeriodTop, o.StudyPeriodWidth, o.StudyPeriodHeight),
        "Qual" => (o.QualificationLeft, o.QualificationTop, o.QualificationWidth, o.QualificationHeight),
        "Spec" => (o.SpecialtyLeft, o.SpecialtyTop, o.SpecialtyWidth, o.SpecialtyHeight),
        "Reg" => (o.RegNumberLeft, o.RegNumberTop, o.RegNumberWidth, o.RegNumberHeight),
        "Issue" => (o.IssueDateLeft, o.IssueDateTop, o.IssueDateWidth, o.IssueDateHeight),
        "Dir" => (o.DirectorLeft, o.DirectorTop, o.DirectorWidth, o.DirectorHeight),
        "PageNum1" => (o.PageNum1Left, o.PageNum1Top, o.PageNum1Width, o.PageNum1Height),
        "PageNum2" => (o.PageNum2Left, o.PageNum2Top, o.PageNum2Width, o.PageNum2Height),
        "PageCount" => (o.PageCountLeft, o.PageCountTop, o.PageCountWidth, o.PageCountHeight),
        _ => (0, 0, 100, 16),
    };

    private static (double left, double top, double width, double height) GetAppendixBackOverlayBox(AppendixBackOverlayLayout o, string key) => key switch
    {
        "Grades" => (o.GradesTableLeft, o.GradesTableTop, o.GradesTableWidth, o.GradesTableHeight),
        "StudyPractice" => (o.StudyPracticeListLeft, o.StudyPracticeListTop, o.StudyPracticeListWidth, o.StudyPracticeListHeight),
        "ProductionPractice" => (o.ProductionPracticeListLeft, o.ProductionPracticeListTop, o.ProductionPracticeListWidth, o.ProductionPracticeListHeight),
        "EmptyHint" => (o.EmptyHintLeft, o.EmptyHintTop, o.EmptyHintWidth, o.EmptyHintHeight),
        "PageNumBack" => (o.PageNumBackLeft, o.PageNumBackTop, o.PageNumBackWidth, o.PageNumBackHeight),
        "PageNumBackRight" => (o.PageNumBackRightLeft, o.PageNumBackRightTop, o.PageNumBackRightWidth, o.PageNumBackRightHeight),
        _ => (0, 0, 100, 16),
    };

    /// <summary>
    /// Горизонтальное примагничивание к ближайшей из двух направляющих:
    /// выбирается ближайшая пара (край блока — линия), учитываются левый край, центр и правый край прямоугольника текста.
    /// </summary>
    public void SnapDiplomaFrontOverlayToGuides(string overlayKey, double renderedWidth)
    {
        const double snapPx = 10;
        const double canvasW = 730;
        var o = DiplomaFrontOverlay;
        var (left, top, wLayout) = GetDiplomaFrontOverlayBox(o, overlayKey);
        var w = wLayout;
        if (!double.IsNaN(renderedWidth) && renderedWidth > 0)
            w = Math.Max(w, renderedWidth);
        w = Math.Max(w, 8);

        SnapDiplomaOverlayToGuides(
            left, top, w, canvasW, snapPx,
            o.CenteringGuide1X, o.CenteringGuide2X,
            (l, t) => SetDiplomaFrontOverlayPosition(overlayKey, l, t));
    }

    public void SnapDiplomaBackOverlayToGuides(string overlayKey, double renderedWidth)
    {
        const double snapPx = 10;
        const double canvasW = 730;
        var o = DiplomaBackOverlay;
        var (left, top, wLayout) = GetDiplomaBackOverlayBox(o, overlayKey);
        var w = wLayout;
        if (!double.IsNaN(renderedWidth) && renderedWidth > 0)
            w = Math.Max(w, renderedWidth);
        w = Math.Max(w, 8);

        SnapDiplomaOverlayToGuides(
            left, top, w, canvasW, snapPx,
            o.CenteringGuide1X, o.CenteringGuide2X,
            (l, t) => SetDiplomaBackOverlayPosition(overlayKey, l, t));
    }

    private static void SnapDiplomaOverlayToGuides(
        double left,
        double top,
        double w,
        double canvasW,
        double snapPx,
        double guide1X,
        double guide2X,
        Action<double, double> setPosition)
    {
        var leftEdge = left;
        var centerX = left + w * 0.5;
        var rightEdge = left + w;

        var guides = new[] { guide1X, guide2X };
        double bestDist = snapPx + 1;
        double bestGuideX = 0;
        int bestWhich = -1;

        foreach (var g in guides)
        {
            var dL = Math.Abs(leftEdge - g);
            if (dL < bestDist) { bestDist = dL; bestGuideX = g; bestWhich = 0; }
            var dC = Math.Abs(centerX - g);
            if (dC < bestDist) { bestDist = dC; bestGuideX = g; bestWhich = 1; }
            var dR = Math.Abs(rightEdge - g);
            if (dR < bestDist) { bestDist = dR; bestGuideX = g; bestWhich = 2; }
        }

        if (bestWhich < 0 || bestDist > snapPx) return;

        var newLeft = bestWhich switch
        {
            0 => bestGuideX,
            1 => bestGuideX - w * 0.5,
            _ => bestGuideX - w,
        };
        newLeft = Math.Clamp(newLeft, 0, Math.Max(0, canvasW - w));
        setPosition(newLeft, top);
    }

    public double GetDiplomaFrontOverlayCurrentWidth(string overlayKey) =>
        GetDiplomaFrontOverlayBox(DiplomaFrontOverlay, overlayKey).width;

    public void SetDiplomaFrontOverlayWidth(string overlayKey, double width)
    {
        const double minW = 40;
        const double cw = 730;
        var o = DiplomaFrontOverlay;
        var (left, _, _) = GetDiplomaFrontOverlayBox(o, overlayKey);
        var w = Math.Clamp(width, minW, Math.Max(minW, cw - left));
        if (overlayKey == "Dup")
            o.DuplicateWidth = w;
    }

    /// <summary>Ширина блока предпросмотра по ключу overlay (соответствует полю в DiplomaBackOverlayLayout).</summary>
    public double GetDiplomaBackOverlayCurrentWidth(string overlayKey) =>
        GetDiplomaBackOverlayBox(DiplomaBackOverlay, overlayKey).width;

    /// <summary>Задаёт ширину текстового блока (левый край без изменений, не выходит за холст 730).</summary>
    public void SetDiplomaBackOverlayWidth(string overlayKey, double width)
    {
        const double minW = 32;
        const double cw = 730;
        var o = DiplomaBackOverlay;
        var (left, _, _) = GetDiplomaBackOverlayBox(o, overlayKey);
        var w = Math.Clamp(width, minW, Math.Max(minW, cw - left));

        switch (overlayKey)
        {
            case "Org": o.OrgWidth = w; break;
            case "QualLabel": o.QualLabelWidth = w; break;
            case "Qual": o.QualWidth = w; break;
            case "Blank": o.BlankSeriesNumberWidth = w; break;
            case "Reg": o.RegWidth = w; break;
            case "Issue": o.IssueWidth = w; break;
            case "Last": o.LastnameWidth = w; break;
            case "Given": o.FirstnameWidth = w; break;
            case "Spec": o.SpecialtyWidth = w; break;
            case "Gec": o.GecDecisionWidth = w; break;
            case "Chair": o.ChairmanWidth = w; break;
            case "Dir": o.DirectorWidth = w; break;
            case "Qr": o.QrSize = Math.Clamp(w, 24, 200); break;
            case "Dup": o.DuplicateWidth = w; break;
        }
    }

    public void SetDiplomaFrontOverlayDuplicateFontSize(double fontSize) =>
        DiplomaFrontOverlay.DuplicateFontSize = Math.Clamp(fontSize, 8, 48);

    /// <summary>Кегль надписи «Дубликат» на обороте (px, 8–48).</summary>
    public void SetDiplomaBackOverlayDuplicateFontSize(double fontSize) =>
        DiplomaBackOverlay.DuplicateFontSize = Math.Clamp(fontSize, 8, 48);

    private static (double left, double top, double width) GetDiplomaFrontOverlayBox(DiplomaFrontOverlayLayout o, string key) => key switch
    {
        "Dup" => (o.DuplicateLeft, o.DuplicateTop, o.DuplicateWidth),
        _ => (0, 0, 100),
    };

    private static (double left, double top, double width) GetDiplomaBackOverlayBox(DiplomaBackOverlayLayout o, string key) => key switch
    {
        "Org" => (o.OrgLeft, o.OrgTop, o.OrgWidth),
        "QualLabel" => (o.QualLabelLeft, o.QualLabelTop, o.QualLabelWidth),
        "Qual" => (o.QualLeft, o.QualTop, o.QualWidth),
        "Blank" => (o.BlankSeriesNumberLeft, o.BlankSeriesNumberTop, o.BlankSeriesNumberWidth),
        "Reg" => (o.RegLeft, o.RegTop, o.RegWidth),
        "Issue" => (o.IssueLeft, o.IssueTop, o.IssueWidth),
        "Last" => (o.LastnameLeft, o.LastnameTop, o.LastnameWidth),
        "Given" => (o.FirstnameLeft, o.FirstnameTop, o.FirstnameWidth),
        "Spec" => (o.SpecialtyLeft, o.SpecialtyTop, o.SpecialtyWidth),
        "Gec" => (o.GecDecisionLeft, o.GecDecisionTop, o.GecDecisionWidth),
        "Chair" => (o.ChairmanLeft, o.ChairmanTop, o.ChairmanWidth),
        "Dir" => (o.DirectorLeft, o.DirectorTop, o.DirectorWidth),
        "Qr" => (o.QrLeft, o.QrTop, o.QrSize),
        "Dup" => (o.DuplicateLeft, o.DuplicateTop, o.DuplicateWidth),
        _ => (0, 0, 100),
    };

    /// <summary>Сохраняет раскладку и кроп в %AppData% (без сообщений при ошибке — см. текст статуса).</summary>
    public void SavePrintLayoutSilent()
    {
        if (_suppressLayoutSave) return;
        try
        {
            WritePrintLayoutJson();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось сохранить раскладку: {ex.Message}";
        }
    }

    private void FlushActiveDiplomaProfileToStore()
    {
        var captured = DiplomaLayoutProfileHelper.Capture(
            DiplomaFrontOverlay,
            DiplomaBackOverlay,
            PreviewCropInsets.ToPersisted(_previewCropDiplomaFront),
            PreviewCropInsets.ToPersisted(_previewCropDiplomaBack));
        if (_activeDiplomaProfileIsHonor)
            _honorDiplomaProfile = captured;
        else
            _standardDiplomaProfile = captured;
    }

    private DiplomaVariantLayoutPersisted ProfileStore(bool honor) =>
        honor ? _honorDiplomaProfile : _standardDiplomaProfile;

    private void EnsureDiplomaLayoutProfileForCurrentPreview()
    {
        var honor = PreviewStudent is SelectableStudent ss
            && ShouldUseHonorTemplate(ss.DiplomaType, StudentGrades.Concat(CourseworkGrades));
        if (honor == _activeDiplomaProfileIsHonor)
            return;
        SwitchDiplomaLayoutProfile(honor);
    }

    private void SwitchDiplomaLayoutProfile(bool honor)
    {
        FlushActiveDiplomaProfileToStore();
        _activeDiplomaProfileIsHonor = honor;
        ApplyDiplomaLayoutProfileFromStore(honor);
    }

    private void ApplyDiplomaLayoutProfileFromStore(bool honor)
    {
        var wasSuppressed = _suppressLayoutSave;
        _suppressLayoutSave = true;
        try
        {
            var store = ProfileStore(honor);
            DiplomaLayoutProfileHelper.ApplyToViewModel(
                store,
                f => DiplomaFrontOverlay = f,
                b => DiplomaBackOverlay = b,
                cf => _previewCropDiplomaFront.ApplyPersisted(cf),
                cb => _previewCropDiplomaBack.ApplyPersisted(cb));
            SubscribeDiplomaFrontOverlayAutosave(DiplomaFrontOverlay);
            SubscribeOverlayAutosave(DiplomaBackOverlay);
            RefreshPreviewCropLayouts();
        }
        finally
        {
            _suppressLayoutSave = wasSuppressed;
        }
    }

    private (DiplomaFrontOverlayLayout Front, DiplomaBackOverlayLayout Back, PreviewCropPersisted CropFront, PreviewCropPersisted CropBack)
        GetDiplomaLayoutSnapshotForPrint(bool honor)
    {
        FlushActiveDiplomaProfileToStore();
        var store = ProfileStore(honor);
        return (
            DiplomaLayoutProfileHelper.Clone(store.DiplomaFrontOverlay ?? new DiplomaFrontOverlayLayout()),
            DiplomaLayoutProfileHelper.Clone(store.DiplomaBackOverlay ?? new DiplomaBackOverlayLayout()),
            DiplomaLayoutProfileHelper.Clone(store.CropDiplomaFront),
            DiplomaLayoutProfileHelper.Clone(store.CropDiplomaBack));
    }

    private static DiplomaVariantLayoutPersisted BuildLegacyDiplomaProfile(DiplomaPrintLayoutPersisted persisted)
    {
        var profile = new DiplomaVariantLayoutPersisted
        {
            DiplomaBackOverlay = persisted.DiplomaBackOverlay,
            CropDiplomaFront = persisted.CropDiplomaFront,
            CropDiplomaBack = persisted.CropDiplomaBack,
        };
        if (persisted.DiplomaFrontOverlay != null)
            profile.DiplomaFrontOverlay = persisted.DiplomaFrontOverlay;
        else if (persisted.DiplomaBackOverlay != null)
        {
            profile.DiplomaFrontOverlay = new DiplomaFrontOverlayLayout
            {
                DuplicateLeft = persisted.DiplomaBackOverlay.DuplicateLeft,
                DuplicateTop = persisted.DiplomaBackOverlay.DuplicateTop,
                DuplicateWidth = persisted.DiplomaBackOverlay.DuplicateWidth,
                DuplicateFontSize = persisted.DiplomaBackOverlay.DuplicateFontSize,
                CenteringGuide1X = persisted.DiplomaBackOverlay.CenteringGuide1X,
                CenteringGuide2X = persisted.DiplomaBackOverlay.CenteringGuide2X,
            };
        }

        if (profile.CropDiplomaFront == null && profile.CropDiplomaBack == null && persisted.CropDiploma != null)
        {
            profile.CropDiplomaFront = persisted.CropDiploma;
            profile.CropDiplomaBack = persisted.CropDiploma;
        }

        return profile;
    }

    private void WritePrintLayoutJson()
    {
        FlushActiveDiplomaProfileToStore();
        var dir = GetUserLayoutDir();
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "diploma_print_layout.json");
        var standard = DiplomaLayoutProfileHelper.CloneProfile(_standardDiplomaProfile);
        var persisted = new DiplomaPrintLayoutPersisted
        {
            StandardDiploma = standard,
            HonorDiploma = DiplomaLayoutProfileHelper.CloneProfile(_honorDiplomaProfile),
            DiplomaFrontOverlay = standard.DiplomaFrontOverlay,
            DiplomaBackOverlay = standard.DiplomaBackOverlay,
            CropDiplomaFront = standard.CropDiplomaFront,
            CropDiplomaBack = standard.CropDiplomaBack,
            CropAppendix = PreviewCropInsets.ToPersisted(_previewCropAppendix),
            DiplomaQrLink = string.IsNullOrWhiteSpace(DiplomaQrLink) ? null : DiplomaQrLink.Trim(),
            AppendixFrontCenteringGuide1X = AppendixFrontCenteringGuide1X,
            AppendixFrontCenteringGuide2X = AppendixFrontCenteringGuide2X,
            AppendixCenteringGuide1X = AppendixCenteringGuide1X,
            AppendixCenteringGuide2X = AppendixCenteringGuide2X,
            AppendixFrontOverlay = AppendixFrontOverlay,
            AppendixBackOverlay = AppendixBackOverlay,
            AppendixShowPageNumbers = AppendixShowPageNumbers,
        };
        var json = JsonSerializer.Serialize(persisted, PrintLayoutJsonOptions);
        File.WriteAllText(path, json);
    }

    private void LoadPrintLayoutFromDisk()
    {
        try
        {
            _suppressLayoutSave = true;
            var dir = GetUserLayoutDir();
            var unifiedPath = Path.Combine(dir, "diploma_print_layout.json");
            var legacyUser = Path.Combine(dir, "diploma_back_overlay_layout.json");
            var legacyTemplates = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocsTemplates", "diploma_back_overlay_layout.json");

            string path;
            if (File.Exists(unifiedPath))
                path = unifiedPath;
            else if (File.Exists(legacyUser))
                path = legacyUser;
            else if (File.Exists(legacyTemplates))
                path = legacyTemplates;
            else
                return;

            var json = File.ReadAllText(path);
            var persisted = JsonSerializer.Deserialize<DiplomaPrintLayoutPersisted>(json, PrintLayoutJsonOptions);
            var hasUnifiedPayload = persisted != null && (
                persisted.StandardDiploma != null ||
                persisted.HonorDiploma != null ||
                persisted.DiplomaFrontOverlay != null ||
                persisted.DiplomaBackOverlay != null ||
                persisted.CropDiploma != null ||
                persisted.CropAppendix != null ||
                !string.IsNullOrEmpty(persisted.DiplomaQrLink) ||
                persisted.AppendixFrontCenteringGuide1X.HasValue ||
                persisted.AppendixFrontCenteringGuide2X.HasValue ||
                persisted.AppendixCenteringGuide1X.HasValue ||
                persisted.AppendixCenteringGuide2X.HasValue ||
                persisted.AppendixFrontOverlay != null ||
                persisted.AppendixBackOverlay != null);

            if (hasUnifiedPayload && persisted != null)
            {
                if (persisted.StandardDiploma != null)
                    _standardDiplomaProfile = DiplomaLayoutProfileHelper.CloneProfile(persisted.StandardDiploma);
                else
                    _standardDiplomaProfile = DiplomaLayoutProfileHelper.CloneProfile(BuildLegacyDiplomaProfile(persisted));

                if (persisted.HonorDiploma != null)
                    _honorDiplomaProfile = DiplomaLayoutProfileHelper.CloneProfile(persisted.HonorDiploma);
                else
                    _honorDiplomaProfile = DiplomaLayoutProfileHelper.CloneProfile(_standardDiplomaProfile);

                _activeDiplomaProfileIsHonor = false;
                ApplyDiplomaLayoutProfileFromStore(false);

                _previewCropAppendix.ApplyPersisted(persisted.CropAppendix);
                DiplomaQrLink = persisted.DiplomaQrLink ?? "";
                var legacyG1 = persisted.AppendixCenteringGuide1X;
                var legacyG2 = persisted.AppendixCenteringGuide2X;
                if (persisted.AppendixFrontCenteringGuide1X is double fg1)
                    AppendixFrontCenteringGuide1X = fg1;
                else if (legacyG1 is double lg1)
                    AppendixFrontCenteringGuide1X = lg1;
                if (persisted.AppendixFrontCenteringGuide2X is double fg2)
                    AppendixFrontCenteringGuide2X = fg2;
                else if (legacyG2 is double lg2)
                    AppendixFrontCenteringGuide2X = lg2;
                if (legacyG1 is double ag1)
                    AppendixCenteringGuide1X = ag1;
                if (legacyG2 is double ag2)
                    AppendixCenteringGuide2X = ag2;
                if (persisted.AppendixFrontOverlay != null)
                    AppendixFrontOverlay = persisted.AppendixFrontOverlay;
                if (persisted.AppendixBackOverlay != null)
                    AppendixBackOverlay = persisted.AppendixBackOverlay;
                if (persisted.AppendixShowPageNumbers is bool showPn)
                    AppendixShowPageNumbers = showPn;
            }
            else
            {
                var legacy = JsonSerializer.Deserialize<DiplomaBackOverlayLayout>(json, PrintLayoutJsonOptions);
                if (legacy != null)
                {
                    _standardDiplomaProfile = new DiplomaVariantLayoutPersisted { DiplomaBackOverlay = legacy };
                    MigrateDiplomaFrontOverlayFromLegacy(legacy);
                    _standardDiplomaProfile.DiplomaFrontOverlay = DiplomaFrontOverlay;
                    _honorDiplomaProfile = DiplomaLayoutProfileHelper.CloneProfile(_standardDiplomaProfile);
                    _activeDiplomaProfileIsHonor = false;
                    ApplyDiplomaLayoutProfileFromStore(false);
                }
            }
        }
        catch
        {
            /* сохранить значения по умолчанию */
        }
        finally
        {
            _suppressLayoutSave = false;
            SubscribeAppendixFrontAutosave(AppendixFrontOverlay);
            SubscribeAppendixBackAutosave(AppendixBackOverlay);
            NormalizeAppendixOverlayHeightsAfterLoad(AppendixFrontOverlay, AppendixBackOverlay);
            EnsureDiplomaLayoutProfileForCurrentPreview();
            RefreshDiplomaBackQrPreview();
        }
    }

    /// <summary>Раньше «Дубликат» на лицевой брал координаты с оборота — копируем при первой загрузке.</summary>
    private void MigrateDiplomaFrontOverlayFromLegacy(DiplomaBackOverlayLayout legacyBack)
    {
        DiplomaFrontOverlay = new DiplomaFrontOverlayLayout
        {
            DuplicateLeft = legacyBack.DuplicateLeft,
            DuplicateTop = legacyBack.DuplicateTop,
            DuplicateWidth = legacyBack.DuplicateWidth,
            DuplicateFontSize = legacyBack.DuplicateFontSize,
            CenteringGuide1X = legacyBack.CenteringGuide1X,
            CenteringGuide2X = legacyBack.CenteringGuide2X,
        };
    }

    /// <summary>Старые JSON без полей высоты могли дать 0 при десериализации — восстанавливаем типовые значения.</summary>
    private static void NormalizeAppendixOverlayHeightsAfterLoad(AppendixFrontOverlayLayout f, AppendixBackOverlayLayout b)
    {
        if (Math.Abs(f.DiplomaBlankFontSize - 11) < 0.01)
            f.DiplomaBlankFontSize = 9;

        if (f.CourseTitleHeight < 4) f.CourseTitleHeight = 16;
        if (f.CourseListHeight < 4) f.CourseListHeight = 200;
        if (f.OrgHeight < 4) f.OrgHeight = 36;
        if (f.LastNameHeight < 4) f.LastNameHeight = 14;
        if (f.FirstNameHeight < 4) f.FirstNameHeight = 14;
        if (f.MiddleNameHeight < 4) f.MiddleNameHeight = 14;
        if (f.BirthHeight < 4) f.BirthHeight = 12;
        if (f.PrevEducationHeight < 4) f.PrevEducationHeight = 40;
        if (f.StudyPeriodHeight < 4) f.StudyPeriodHeight = 14;
        if (f.QualificationHeight < 4) f.QualificationHeight = 14;
        if (f.SpecialtyHeight < 4) f.SpecialtyHeight = 36;
        if (f.RegNumberHeight < 4) f.RegNumberHeight = 14;
        if (f.IssueDateHeight < 4) f.IssueDateHeight = 14;
        if (f.DirectorHeight < 4) f.DirectorHeight = 14;

        if (b.GradesTableHeight < 4) b.GradesTableHeight = 360;
        if (b.StudyPracticeListWidth < 4)
        {
            b.StudyPracticeListLeft = 402;
            b.StudyPracticeListTop = 158;
            b.StudyPracticeListWidth = 355;
            b.StudyPracticeListHeight = 115;
        }
        else if (b.StudyPracticeListHeight < 4)
            b.StudyPracticeListHeight = 115;

        if (b.ProductionPracticeListWidth < 4)
        {
            b.ProductionPracticeListLeft = 402;
            b.ProductionPracticeListTop = 348;
            b.ProductionPracticeListWidth = 355;
            b.ProductionPracticeListHeight = 115;
        }
        else if (b.ProductionPracticeListHeight < 4)
            b.ProductionPracticeListHeight = 115;
        if (b.EmptyHintHeight < 4) b.EmptyHintHeight = 16;
        if (b.GradesHoursShareOfTail < 0.07 || b.GradesHoursShareOfTail > 0.93 || double.IsNaN(b.GradesHoursShareOfTail))
            b.GradesHoursShareOfTail = AppendixGradesTailColumnWidthMultiConverter.DefaultShare;
        if (double.IsNaN(b.GradesHoursGradeGapPx) || b.GradesHoursGradeGapPx < 0)
            b.GradesHoursGradeGapPx = 0;
        var tailW = Math.Max(0, b.GradesTableWidth - AppendixGradesTailColumnWidthMultiConverter.GradesSubjectColWidth(b.GradesTableWidth));
        b.GradesHoursGradeGapPx = AppendixGradesTailColumnWidthMultiConverter.ClampGap(b.GradesHoursGradeGapPx, tailW);
        b.StudyPracticeActivityShare = AppendixPracticeColumnWidthMultiConverter.ClampActivityShare(b.StudyPracticeActivityShare);
        b.StudyPracticeMeansShareOfRemainder = AppendixPracticeColumnWidthMultiConverter.ClampMeansShare(b.StudyPracticeMeansShareOfRemainder);
        b.StudyPracticeColumnGapPx = AppendixPracticeColumnWidthMultiConverter.ClampGap(
            b.StudyPracticeColumnGapPx, b.StudyPracticeListWidth);

        b.ProductionPracticeActivityShare = AppendixPracticeColumnWidthMultiConverter.ClampActivityShare(b.ProductionPracticeActivityShare);
        b.ProductionPracticeMeansShareOfRemainder = AppendixPracticeColumnWidthMultiConverter.ClampMeansShare(b.ProductionPracticeMeansShareOfRemainder);
        b.ProductionPracticeColumnGapPx = AppendixPracticeColumnWidthMultiConverter.ClampGap(
            b.ProductionPracticeColumnGapPx, b.ProductionPracticeListWidth);

        MigrateLegacyPracticeColumnShares(b);

        f.EnsurePageNumberBlocksDefaults();
        b.EnsurePageNumberBackDefaults();
        if (f.PageNum1Height < 4) f.PageNum1Height = 20;
        if (f.PageNum2Height < 4) f.PageNum2Height = 20;
        if (f.PageCountHeight < 4) f.PageCountHeight = 16;
        if (b.PageNumBackHeight < 4) b.PageNumBackHeight = 20;
        if (b.PageNumBackRightHeight < 4) b.PageNumBackRightHeight = 20;

        if (string.IsNullOrWhiteSpace(f.PageNum1Text)) f.PageNum1Text = "1";
        else f.PageNum1Text = f.PageNum1Text.Trim();
        if (string.IsNullOrWhiteSpace(f.PageNum2Text)) f.PageNum2Text = "2";
        else f.PageNum2Text = f.PageNum2Text.Trim();
        if (string.IsNullOrWhiteSpace(f.PageCountText)) f.PageCountText = "4";
        else f.PageCountText = f.PageCountText.Trim();
        if (string.IsNullOrWhiteSpace(b.PageNumBackText)) b.PageNumBackText = "3";
        else b.PageNumBackText = b.PageNumBackText.Trim();
        if (string.IsNullOrWhiteSpace(b.PageNumBackRightText)) b.PageNumBackRightText = "4";
        else b.PageNumBackRightText = b.PageNumBackRightText.Trim();
    }

    private static void MigrateLegacyPracticeColumnShares(AppendixBackOverlayLayout b)
    {
        var legacyActivityDiffers = Math.Abs(
            b.PracticeActivityShare - AppendixPracticeColumnWidthMultiConverter.DefaultActivityShare) > 0.0001;
        var legacyMeansDiffers = Math.Abs(
            b.PracticeMeansShareOfRemainder - AppendixPracticeColumnWidthMultiConverter.DefaultMeansShareOfRemainder) > 0.0001;

        if (legacyActivityDiffers
            && Math.Abs(b.StudyPracticeActivityShare - AppendixPracticeColumnWidthMultiConverter.DefaultActivityShare) < 0.0001
            && Math.Abs(b.ProductionPracticeActivityShare - AppendixPracticeColumnWidthMultiConverter.DefaultActivityShare) < 0.0001)
        {
            b.StudyPracticeActivityShare = b.PracticeActivityShare;
            b.ProductionPracticeActivityShare = b.PracticeActivityShare;
        }

        if (legacyMeansDiffers
            && Math.Abs(b.StudyPracticeMeansShareOfRemainder - AppendixPracticeColumnWidthMultiConverter.DefaultMeansShareOfRemainder) < 0.0001
            && Math.Abs(b.ProductionPracticeMeansShareOfRemainder - AppendixPracticeColumnWidthMultiConverter.DefaultMeansShareOfRemainder) < 0.0001)
        {
            b.StudyPracticeMeansShareOfRemainder = b.PracticeMeansShareOfRemainder;
            b.ProductionPracticeMeansShareOfRemainder = b.PracticeMeansShareOfRemainder;
        }

        b.PracticeActivityShare = b.StudyPracticeActivityShare;
        b.PracticeMeansShareOfRemainder = b.StudyPracticeMeansShareOfRemainder;
    }

    private static string GetUserLayoutDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChtotibDocsPrintNET");

    [RelayCommand]
    private void ZoomIn()
    {
        FitPreviewToFrame = false;
        PreviewZoom = Math.Min(5.0, Math.Round(PreviewZoom + 0.1, 2));
    }

    [RelayCommand]
    private void ZoomOut()
    {
        FitPreviewToFrame = false;
        PreviewZoom = Math.Max(0.25, Math.Round(PreviewZoom - 0.1, 2));
    }

    [RelayCommand]
    private void ZoomReset()
    {
        FitPreviewToFrame = false;
        PreviewZoom = 1.0;
    }

    private void LoadLazurskiFont()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var fontPath = Path.Combine(baseDir, "DocsTemplates", "Lazurski.ttf");
        if (File.Exists(fontPath))
        {
            var dir = Path.GetDirectoryName(fontPath)!.Replace("\\", "/");
            var dirUri = new Uri("file:///" + dir + "/");
            try
            {
                // Внутреннее имя шрифта может не совпадать с именем файла.
                var families = Fonts.GetFontFamilies(dirUri).ToList();
                var laz = families.FirstOrDefault(f =>
                    f.FamilyNames.Values.Any(n => n.Contains("Lazur", StringComparison.OrdinalIgnoreCase)) ||
                    f.Source.Contains("Lazur", StringComparison.OrdinalIgnoreCase));
                LazurskiFont = laz ?? families.FirstOrDefault() ?? new FontFamily(dirUri, "./#Lazurski");
            }
            catch
            {
                LazurskiFont = new FontFamily(dirUri, "./#Lazurski");
            }
        }
    }

    private void LoadTemplateImages() => RefreshPreviewTemplateImages();

    private void RefreshPreviewTemplateImages()
    {
        EnsureDiplomaLayoutProfileForCurrentPreview();
        var honor = _activeDiplomaProfileIsHonor;
        DiplomaFrontImage = LoadTemplateBitmap("Диплом (лицевая)", honor);
        DiplomaBackImage = LoadTemplateBitmap("Диплом (обратная сторона)", honor);
        PrilojenieFrontImage = LoadTemplateBitmap("Приложение (лицевая)", false);
        PrilojenieBackImage = LoadTemplateBitmap("Приложение (оборотная)", false);
    }

    private static BitmapImage? LoadTemplateBitmap(string documentType, bool useHonor)
    {
        var bytes = DocumentTemplateService.LoadTemplateBytes(documentType, useHonor);
        if (bytes == null || bytes.Length == 0) return null;
        using var ms = new MemoryStream(bytes);
        var img = new BitmapImage();
        img.BeginInit();
        img.StreamSource = ms;
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }

    [RelayCommand]
    private void ReplaceDiplomaFrontTemplate() => PickAndReplaceTemplate("Диплом (лицевая)", false);

    [RelayCommand]
    private void ReplaceDiplomaBackTemplate() => PickAndReplaceTemplate("Диплом (обратная сторона)", false);

    [RelayCommand]
    private void ReplaceDiplomaExcellentFrontTemplate() => PickAndReplaceTemplate("Диплом (лицевая)", true);

    [RelayCommand]
    private void ReplaceDiplomaExcellentBackTemplate() => PickAndReplaceTemplate("Диплом (обратная сторона)", true);

    [RelayCommand]
    private void ReplaceAppendixFrontTemplate() => PickAndReplaceTemplate("Приложение (лицевая)", false);

    [RelayCommand]
    private void ReplaceAppendixBackTemplate() => PickAndReplaceTemplate("Приложение (оборотная)", false);

    private void PickAndReplaceTemplate(string documentType, bool useHonor)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Выберите файл подложки (JPG/PNG)",
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            DocumentTemplateService.SetCustomTemplate(documentType, useHonor, dlg.FileName);
            RefreshPreviewTemplateImages();
            StatusMessage = $"✓ Шаблон «{documentType}» обновлён.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Не удалось заменить шаблон: {ex.Message}";
        }
    }

    private void LoadOrgSettings()
    {
        try
        {
            var data = AppSettings.Load();
            OrgName = (data.OrganizationName ?? "").Trim();
            DirectorName = data.DirectorName ?? "";
        }
        catch { }
    }

    private static BitmapImage? LoadImage(string path)
    {
        if (!File.Exists(path)) return null;
        var img = new BitmapImage();
        img.BeginInit();
        img.UriSource = new Uri(path, UriKind.Absolute);
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }

    partial void OnIssueDateChanged(DateTime value) => UpdateIssueDateRu();

    partial void OnDiplomaQrLinkChanged(string value)
    {
        if (_suppressLayoutSave) return;
        SavePrintLayoutSilent();
    }

    partial void OnPrintDrawBackgroundChanged(bool value) => RefreshPreviewTemplateImages();

    partial void OnPrintIsDuplicateChanged(bool value) { }

    private PrintDocumentOptions CurrentPrintOptions => new()
    {
        DrawBackground = PrintDrawBackground,
        PrintDuplicate = PrintIsDuplicate,
        PrintQualificationLabel = PrintQualificationLabel,
        UseHonorTemplatesWhenApplicable = true,
    };

    public static bool IsHonorDiploma(string? diplomaType) =>
        StudentHonorEvaluator.IsHonorDiplomaType(diplomaType);

    private static bool ShouldUseHonorTemplate(string? diplomaType, IEnumerable<Grade>? grades) =>
        StudentHonorEvaluator.ShouldUseHonorTemplate(diplomaType, grades);

    private string BuildQrPayloadFor(SelectableStudent student)
    {
        if (!string.IsNullOrWhiteSpace(student.CustomQrPayload))
            return student.CustomQrPayload.Trim();

        var issueDt = DiplomaQrPayloadBuilder.ResolveQrIssueDate(student.DiplomaIssueDate, IssueDate);
        return DiplomaQrPayloadBuilder.Build(
            student,
            issueDt,
            OrgName,
            student.DemoExamParticipantCode,
            student.DemoExamScore,
            student.DemoExamMaxScore,
            student.DemoExamLevel);
    }

    private List<StudentPrintRow> ToPrintRows(IEnumerable<SelectableStudent> students) =>
        students.Select(s =>
        {
            var issueDt = StudentDiplomaPrintHelper.ResolveIssueDate(s.DiplomaIssueDate, IssueDate);
            var honor = ShouldUseHonorTemplate(s.DiplomaType, DatabaseService.Instance.GetStudentGrades(s.Id));
            var layout = GetDiplomaLayoutSnapshotForPrint(honor);
            return new StudentPrintRow
            {
                Student = s,
                IssueDate = issueDt,
                GecDecisionDateLine = StudentDiplomaPrintHelper.FormatGecDecisionLine(issueDt, RuCulture),
                QrPayload = BuildQrPayloadFor(s),
                UseHonorTemplate = honor,
                DiplomaFrontLayout = layout.Front,
                DiplomaBackLayout = layout.Back,
                CropDiplomaFront = layout.CropFront,
                CropDiplomaBack = layout.CropBack,
            };
        }).ToList();

    private FullPacketStudentRow BuildPacketRow(SelectableStudent sel, List<Grade> grades)
    {
        var coursework = new List<Grade>();
        foreach (var g in grades)
        {
            if (g.GradeType == "Курсовая") coursework.Add(g);
        }

        var (subject, study, production) = AppendixGradeClassifier.Split(
            grades.Where(g => g.GradeType != "Курсовая"));

        var birthRu = sel.BirthDate != null
            ? sel.BirthDate.Value.ToString("dd MMMM yyyy", RuCulture) + " года"
            : "";
        var issueDt = StudentDiplomaPrintHelper.ResolveIssueDate(sel.DiplomaIssueDate, IssueDate);
        var honor = ShouldUseHonorTemplate(sel.DiplomaType, grades);
        var layout = GetDiplomaLayoutSnapshotForPrint(honor);

        return new FullPacketStudentRow
        {
            Student = sel,
            CourseworkGrades = coursework,
            SubjectGrades = subject,
            StudyPracticeGrades = AppendixPracticeDisplayBuilder.BuildStudyRows(study, sel.Id),
            ProductionPracticeGrades = AppendixPracticeDisplayBuilder.BuildProductionRows(production, sel.Id),
            BirthDateRu = birthRu,
            PreviousEducation = StudentDiplomaPrintHelper.FormatPreviousEducation(sel),
            IssueDate = issueDt,
            IssueDateRu = StudentDiplomaPrintHelper.FormatIssueDateRu(issueDt, RuCulture),
            GecDecisionDateLine = StudentDiplomaPrintHelper.FormatGecDecisionLine(issueDt, RuCulture),
            QrPayload = BuildQrPayloadFor(sel),
            UseHonorTemplate = honor,
            DiplomaFrontLayout = layout.Front,
            DiplomaBackLayout = layout.Back,
            CropDiplomaFront = layout.CropFront,
            CropDiplomaBack = layout.CropBack,
        };
    }

    private void UpdateIssueDateRu()
    {
        // Совпадает с форматом в PDF (DiplomaPdfSkiaCompositor оборота диплома).
        IssueDateRu = IssueDate.ToString("dd MMMM yyyy г.", RuCulture);
        GecDecisionDateLine = "от " + IssueDateRu;
    }

    private void UpdateBirthDateRu()
    {
        if (PreviewStudent?.BirthDate != null)
            BirthDateRu = PreviewStudent.BirthDate.Value.ToString("dd MMMM yyyy", RuCulture) + " года";
        else
            BirthDateRu = "";
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        LoadStudents();
        LoadSpecialtyInfo();
        UpdateDocumentCount();
        ResetPreview();
    }

    partial void OnPrintTargetChanged(string value)
    {
        ShowStudentsList = value == "Выбранные студенты";
        UpdateDocumentCount();
        ResetPreview();
    }

    partial void OnSelectedDocumentTypeChanged(string value)
    {
        UpdatePreview();
    }

    private void LoadSpecialtyInfo()
    {
        if (SelectedGroup == null)
        {
            _specialtyQualificationFallback = "";
            QualificationText = ""; SpecialtyCodeName = ""; StudyPeriod = "";
            return;
        }
        try
        {
            var sp = DatabaseService.Instance.GetSpecialtyById(SelectedGroup.SpecialtyId);
            if (sp != null)
            {
                _specialtyQualificationFallback = QualificationResolver.Resolve(null, sp);
                SpecialtyCodeName = $"{sp.Code} {sp.Name}";
                StudyForm = sp.StudyForm;
                var years = (int)sp.StudyYears;
                var months = (int)((sp.StudyYears - years) * 10);
                StudyPeriod = months > 0
                    ? $"{years} {YearWord(years)} {months} {MonthWord(months)}"
                    : $"{years} {YearWord(years)}";
                ApplyQualificationForPreviewStudent();
            }
        }
        catch { }
    }

    private static string YearWord(int n) => n switch
    {
        1 => "год", >= 2 and <= 4 => "года", _ => "лет"
    };
    private static string MonthWord(int n) => n switch
    {
        1 => "месяц", >= 2 and <= 4 => "месяца", _ => "месяцев"
    };

    private void LoadData()
    {
        try
        {
            var db = DatabaseService.Instance;
            Groups.Clear(); foreach (var g in db.GetGroups(false, null, null)) Groups.Add(g);
            Chairmen.Clear(); foreach (var c in db.GetCommissionMembers()) Chairmen.Add(c);
        }
        catch { }
    }

    public void ReloadData()
    {
        LoadData();
        LoadStudents();
    }

    /// <summary>
    /// Настраивает экран печати для одного студента (группа, «выбранные студенты», галочка только у него).
    /// </summary>
    /// <returns>false, если студент или группа не найдены (сообщение уже показано).</returns>
    public bool PreparePrintForStudent(int studentId)
    {
        try
        {
            var db = DatabaseService.Instance;
            var student = db.GetStudentById(studentId);
            if (student == null)
            {
                MessageBox.Show("Студент не найден в базе.", "Печать диплома",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (Groups.Count == 0)
                LoadData();

            var group = Groups.FirstOrDefault(g => g.Id == student.GroupId);
            if (group == null)
            {
                MessageBox.Show("Группа студента отсутствует в списке. Проверьте экран «Группы».", "Печать диплома",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            PrintTarget = "Выбранные студенты";

            if (SelectedGroup?.Id != group.Id)
                SelectedGroup = group;
            else
                LoadStudents();

            foreach (var s in Students)
                s.IsSelected = s.Id == studentId;

            if (Students.Count == 0)
                return false;

            UpdateDocumentCount();
            UpdatePreview();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть печать: {ex.Message}", "Печать диплома",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void LoadStudents()
    {
        Students.Clear();
        if (SelectedGroup == null) return;
        try
        {
            var db = DatabaseService.Instance;
            var list = db.GetStudentsByGroup(SelectedGroup.Id, null);
            foreach (var s in list)
            {
                var full = db.GetStudentById(s.Id);
                var dip = db.GetDiplomaByStudent(s.Id);
                Students.Add(new SelectableStudent
                {
                    Id = s.Id, LastName = s.LastName, FirstName = s.FirstName,
                    MiddleName = s.MiddleName, GroupId = s.GroupId, GroupName = s.GroupName,
                    RegistrationNumber = full?.RegistrationNumber ?? s.RegistrationNumber,
                    Qualification = full?.Qualification,
                    BirthDate = full?.BirthDate,
                    PreviousEducation = full?.PreviousEducation,
                    PreviousEducationDoc = full?.PreviousEducationDoc,
                    DiplomaSeries = dip?.Series,
                    DiplomaNumber = dip?.Number,
                    DiplomaType = dip?.DiplomaType ?? "Обычный",
                    DiplomaIssueDate = dip?.IssueDate,
                    DemoExamParticipantCode = full?.DemoExamParticipantCode,
                    DemoExamScore = full?.DemoExamScore,
                    DemoExamMaxScore = full?.DemoExamMaxScore ?? 70,
                    DemoExamLevel = full?.DemoExamLevel,
                    IsSelected = true
                });
            }
        }
        catch { }
    }

    private void LoadStudentGrades()
    {
        StudentGrades.Clear();
        StudyPracticeGrades.Clear();
        ProductionPracticeGrades.Clear();
        CourseworkGrades.Clear();
        if (PreviewStudent == null) return;
        try
        {
            var grades = DatabaseService.Instance.GetStudentGrades(PreviewStudent.Id);
            foreach (var g in grades)
            {
                if (g.GradeType == "Курсовая")
                    CourseworkGrades.Add(g);
            }

            var (disciplines, study, production) = AppendixGradeClassifier.Split(
                grades.Where(g => g.GradeType != "Курсовая"));
            foreach (var g in disciplines) StudentGrades.Add(g);
            foreach (var row in AppendixPracticeDisplayBuilder.BuildStudyRows(study, PreviewStudent.Id))
                StudyPracticeGrades.Add(row);
            foreach (var row in AppendixPracticeDisplayBuilder.BuildProductionRows(production, PreviewStudent.Id))
                ProductionPracticeGrades.Add(row);
        }
        catch { }
    }

    private void ResetPreview()
    {
        _currentPreviewStudentIndex = 0;
        UpdatePreview();
    }

    [RelayCommand]
    private void ResetPreviewCropDiplomaFront()
    {
        _previewCropDiplomaFront.ApplyPersisted(new PreviewCropPersisted());
        SavePrintLayoutSilent();
    }

    [RelayCommand]
    private void ResetPreviewCropDiplomaBack()
    {
        _previewCropDiplomaBack.ApplyPersisted(new PreviewCropPersisted());
        SavePrintLayoutSilent();
    }

    private void UpdatePreview()
    {
        var list = PreviewStudents;
        if (list.Count == 0)
        {
            PreviewStudent = null;
            PreviewStudentLabel = "Нет студентов";
            BirthDateRu = "";
            PreviousEducation = "";
            return;
        }
        if (_currentPreviewStudentIndex >= list.Count) _currentPreviewStudentIndex = 0;
        PreviewStudent = list[_currentPreviewStudentIndex];
        OnPropertyChanged(nameof(CurrentPreviewStudentIndex));
        PreviewStudentLabel = $"{_currentPreviewStudentIndex + 1} / {list.Count}: {PreviewStudent.FullName}";
        UpdateBirthDateRu();
        PreviousEducation = StudentDiplomaPrintHelper.FormatPreviousEducation(PreviewStudent);
        if (PreviewStudent is SelectableStudent sel)
        {
            var issueDt = StudentDiplomaPrintHelper.ResolveIssueDate(sel.DiplomaIssueDate, IssueDate);
            IssueDateRu = StudentDiplomaPrintHelper.FormatIssueDateRu(issueDt, RuCulture);
            GecDecisionDateLine = StudentDiplomaPrintHelper.FormatGecDecisionLine(issueDt, RuCulture);
        }
        LoadStudentGrades();
        RefreshPreviewTemplateImages();
        RefreshDiplomaBackQrPreview();
    }

    partial void OnPreviewQrPayloadChanged(string value)
    {
        if (_suppressQrPayloadSync)
            return;

        if (PreviewStudent is SelectableStudent sel)
            sel.CustomQrPayload = value;

        RenderDiplomaBackQrImage(value);
    }

    private void RefreshDiplomaBackQrPreview()
    {
        try
        {
            var text = PreviewStudent is SelectableStudent sel
                ? BuildQrPayloadFor(sel)
                : "";

            _suppressQrPayloadSync = true;
            PreviewQrPayload = text;
            _suppressQrPayloadSync = false;

            RenderDiplomaBackQrImage(text);
        }
        catch
        {
            DiplomaBackQrImage = null;
        }
    }

    private void RenderDiplomaBackQrImage(string text)
    {
        try
        {
            if (text.Length == 0)
            {
                DiplomaBackQrImage = null;
                return;
            }

            var png = DiplomaQrCodeService.RenderPng(text);
            if (png == null || png.Length == 0)
            {
                DiplomaBackQrImage = null;
                return;
            }

            DiplomaBackQrImage = CreateQrPreviewBitmapNormalized(png);
        }
        catch
        {
            DiplomaBackQrImage = null;
        }
    }

    /// <summary>
    /// PNG из QRCoder копируем в Bgr32 с DpiX/Y=96. Иначе встроенный DPI/индексированный формат даёт неверный aspect в dips,
    /// Image в квадратном Border визуально «съезжает» (поля слева/справа неравномерные).
    /// </summary>
    private static BitmapSource? CreateQrPreviewBitmapNormalized(byte[] png)
    {
        try
        {
            using var ms = new MemoryStream(png, writable: false);
            var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) return null;
            var frame = decoder.Frames[0];
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgr32, null, 0);
            var w = converted.PixelWidth;
            var h = converted.PixelHeight;
            if (w <= 0 || h <= 0) return null;
            var stride = (w * PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
            var pixels = new byte[stride * h];
            converted.CopyPixels(pixels, stride, 0);
            var result = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            result.Freeze();
            return result;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateDocumentCount()
    {
        if (SelectedGroup == null) { TotalDocuments = 0; EstimatedSize = "—"; return; }
        var count = PrintTarget == "Выбранные студенты"
            ? Students.Count(s => s.IsSelected)
            : Students.Count;
        TotalDocuments = count;
        EstimatedSize = $"~{count * 4} стр.";
    }

    [RelayCommand]
    private void PrevPreviewStudent()
    {
        var list = PreviewStudents;
        if (list.Count == 0) return;
        _currentPreviewStudentIndex = (_currentPreviewStudentIndex - 1 + list.Count) % list.Count;
        UpdatePreview();
    }

    [RelayCommand]
    private void NextPreviewStudent()
    {
        var list = PreviewStudents;
        if (list.Count == 0) return;
        _currentPreviewStudentIndex = (_currentPreviewStudentIndex + 1) % list.Count;
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectAllStudents()
    {
        foreach (var s in Students) s.IsSelected = true;
        UpdateDocumentCount(); ResetPreview();
    }

    [RelayCommand]
    private void DeselectAllStudents()
    {
        foreach (var s in Students) s.IsSelected = false;
        UpdateDocumentCount(); UpdatePreview();
    }

    [RelayCommand]
    private void StudentSelectionChanged()
    {
        UpdateDocumentCount(); UpdatePreview();
    }

    [RelayCommand]
    private void RefreshPreview()
    {
        try
        {
            var currentId = PreviewStudent?.Id;
            var selectedIds = Students.Where(s => s.IsSelected).Select(s => s.Id).ToHashSet();

            // Перечитать данные из БД
            LoadStudents();
            LoadSpecialtyInfo();
            RefreshOfficialShortNames();

            // Восстановить выбор в режиме «Выбранные студенты»
            if (selectedIds.Count > 0)
            {
                foreach (var s in Students)
                    s.IsSelected = selectedIds.Contains(s.Id);
            }

            UpdateDocumentCount();

            // Вернуть текущего студента в предпросмотр (если он остался в списке)
            if (currentId.HasValue)
            {
                var list = PreviewStudents;
                var idx = list.FindIndex(s => s.Id == currentId.Value);
                if (idx >= 0)
                    _currentPreviewStudentIndex = idx;
            }

            UpdatePreview();
            StatusMessage = "Предпросмотр обновлён.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось обновить предпросмотр: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveDiplomaBackOverlayLayout()
    {
        try
        {
            WritePrintLayoutJson();
            StatusMessage = "Раскладка (оборот диплома и кроп предпросмотра) сохранена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось сохранить раскладку: {ex.Message}";
        }
    }

    private bool TryGetStudentsForOutput(out List<SelectableStudent> students)
    {
        students = [];
        if (SelectedGroup == null)
        {
            StatusMessage = "Выберите группу";
            return false;
        }

        students = PrintTarget == "Выбранные студенты"
            ? Students.Where(s => s.IsSelected).ToList()
            : Students.ToList();
        if (students.Count == 0)
        {
            StatusMessage = "Нет студентов для печати";
            return false;
        }

        return true;
    }

    private static string FormatOutputDocLabel(string documentType, int studentCount) =>
        documentType == "Полный комплект (PDF)"
            ? $"{studentCount} учен. × 4 стр."
            : $"{studentCount} док.";

    private byte[] GeneratePrintPdf(
        IReadOnlyList<SelectableStudent> students,
        IProgress<PdfGenerationProgress>? progress = null)
    {
        var db = DatabaseService.Instance;
        var chairShort = PersonNameFormatter.ToShortRussianOfficial(SelectedChairman?.FullName);

        if (SelectedDocumentType == "Полный комплект (PDF)")
        {
            var lazTtf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocsTemplates", "Lazurski.ttf");
            var packetRows = new List<FullPacketStudentRow>();
            foreach (var sel in students)
                packetRows.Add(BuildPacketRow(sel, db.GetStudentGrades(sel.Id)));

            return new DiplomaPrintService().GenerateBatchFullDiplomaPacketPdf(
                packetRows,
                OrgName ?? "",
                SpecialtyCodeName ?? "",
                chairShort,
                DirectorShortName,
                File.Exists(lazTtf) ? lazTtf : null,
                _specialtyQualificationFallback,
                PreviewCropInsets.ToPersisted(_previewCropDiplomaFront),
                PreviewCropInsets.ToPersisted(_previewCropDiplomaBack),
                PreviewCropInsets.ToPersisted(_previewCropAppendix),
                DiplomaFrontOverlay,
                DiplomaBackOverlay,
                AppendixFrontOverlay,
                AppendixBackOverlay,
                AppendixShowPageNumbers,
                CurrentPrintOptions,
                StudyPeriod ?? "",
                progress);
        }

        if (SelectedDocumentType == "Диплом (обратная сторона)")
        {
            var lazTtf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocsTemplates", "Lazurski.ttf");
            return new DiplomaPrintService().GenerateBatchDiplomaBackPdf(
                ToPrintRows(students),
                DiplomaBackOverlay,
                OrgName ?? "",
                SpecialtyCodeName ?? "",
                chairShort,
                DirectorShortName,
                File.Exists(lazTtf) ? lazTtf : null,
                _specialtyQualificationFallback,
                PreviewCropInsets.ToPersisted(_previewCropDiplomaBack),
                CurrentPrintOptions,
                progress);
        }

        if (SelectedDocumentType == "Диплом (лицевая)")
        {
            return new DiplomaPrintService().GenerateBatchDiplomaFrontPdf(
                ToPrintRows(students),
                OrgName ?? "",
                _specialtyQualificationFallback,
                PreviewCropInsets.ToPersisted(_previewCropDiplomaFront),
                CurrentPrintOptions,
                DiplomaFrontOverlay,
                progress);
        }

        var settings = db.GetPrintSettings(SelectedDocumentType.Contains("Диплом") ? "ТитульныйЛист" : "Приложение");
        return new DiplomaPrintService().GenerateBatchPdf(
            students.Cast<Student>().ToList(), settings, SelectedDocumentType, IssueDate, chairShort,
            CurrentPrintOptions,
            progress);
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePdf))]
    private void PrintToPrinter()
    {
        if (!TryGetStudentsForOutput(out var students))
            return;

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;

        var printerName = printDialog.PrintQueue?.Name;
        if (string.IsNullOrWhiteSpace(printerName))
            printerName = printDialog.PrintQueue?.FullName;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            StatusMessage = "Не удалось определить принтер";
            return;
        }

        try
        {
            StatusMessage = "Подготовка к печати…";
            var pdf = GeneratePrintPdf(students);
            var jobName = $"{SelectedDocumentType}_{SelectedGroup!.Name}";
            PdfPrintService.Print(pdf, printerName, jobName);
            var docLabel = FormatOutputDocLabel(SelectedDocumentType, students.Count);
            StatusMessage = $"✓ Отправлено на печать ({printerName}): {docLabel}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Ошибка печати: {ex.Message}";
            MessageBox.Show($"Не удалось напечатать документ:\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePdf))]
    private async Task SaveToPdfAsync()
    {
        if (!TryGetStudentsForOutput(out var students))
            return;

        var defaultName = SelectedDocumentType == "Полный комплект (PDF)"
            ? $"Комплект_{SelectedGroup!.Name}_{DateTime.Now:yyyyMMdd}.pdf"
            : $"Дипломы_{SelectedGroup.Name}_{DateTime.Now:yyyyMMdd}.pdf";
        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = defaultName
        };
        if (dlg.ShowDialog() != true)
            return;

        IsPdfGenerating = true;
        PdfProgress = 0;
        PdfProgressText = "Подготовка…";
        StatusMessage = "Формирование PDF…";
        SaveToPdfCommand.NotifyCanExecuteChanged();
        PrintToPrinterCommand.NotifyCanExecuteChanged();

        try
        {
            var progress = new Progress<PdfGenerationProgress>(p =>
            {
                PdfProgress = p.Total > 0 ? 100.0 * p.Current / p.Total : 0;
                PdfProgressText = p.Message;
                StatusMessage = p.Message;
            });

            var pdf = await Task.Run(() => GeneratePrintPdf(students, progress));
            await Task.Run(() => File.WriteAllBytes(dlg.FileName, pdf));
            var docLabel = FormatOutputDocLabel(SelectedDocumentType, students.Count);
            PdfProgress = 100;
            PdfProgressText = "Готово";
            StatusMessage = $"✓ Сохранено: {dlg.FileName} ({docLabel})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Ошибка: {ex.Message}";
            PdfProgressText = "Ошибка";
        }
        finally
        {
            IsPdfGenerating = false;
            SaveToPdfCommand.NotifyCanExecuteChanged();
            PrintToPrinterCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanGeneratePdf() => !IsPdfGenerating;

    partial void OnIsPdfGeneratingChanged(bool value)
    {
        SaveToPdfCommand.NotifyCanExecuteChanged();
        PrintToPrinterCommand.NotifyCanExecuteChanged();
    }
}

public class SelectableStudent : Student, INotifyPropertyChanged
{
    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value == _isSelected) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Серия бланка диплома (красные цифры), таблица Diplomas.</summary>
    public string? DiplomaSeries { get; set; }
    public string? DiplomaNumber { get; set; }
    public string DiplomaType { get; set; } = "Обычный";
    public DateTime? DiplomaIssueDate { get; set; }
    public string? PreviousEducationDoc { get; set; }
    public bool IsWithHonors => PrintViewModel.IsHonorDiploma(DiplomaType);

    /// <summary>Ручная правка текста QR для этого студента (если задано — используется при предпросмотре и печати).</summary>
    public string? CustomQrPayload { get; set; }
}
