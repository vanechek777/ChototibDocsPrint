using CommunityToolkit.Mvvm.ComponentModel;

namespace ChtotibDocsPrintNET.Models;

/// <summary>Обрез холста: отступы от краёв внутрь (px). Для лица и оборота диплома учитывается при сохранении PDF (Skia); для приложения — только предпросмотр.</summary>
public partial class PreviewCropInsets : ObservableObject
{
    private readonly Action? _onChanged;
    private bool _muteNotify;

    public PreviewCropInsets(Action? onChanged = null)
    {
        _onChanged = onChanged;
    }

    [ObservableProperty] private double _insetLeft;
    [ObservableProperty] private double _insetRight;
    [ObservableProperty] private double _insetTop;
    [ObservableProperty] private double _insetBottom;

    partial void OnInsetLeftChanged(double value) => NotifyIfNotMuted();
    partial void OnInsetRightChanged(double value) => NotifyIfNotMuted();
    partial void OnInsetTopChanged(double value) => NotifyIfNotMuted();
    partial void OnInsetBottomChanged(double value) => NotifyIfNotMuted();

    private void NotifyIfNotMuted()
    {
        if (!_muteNotify) _onChanged?.Invoke();
    }

    /// <summary>Приводит отступы к допустимым значениям (внутренняя область не меньше minInner px).</summary>
    public void ClampToCanvas(double canvasW, double canvasH, double minInner = 40)
    {
        _muteNotify = true;
        try
        {
            InsetLeft = Math.Max(0, InsetLeft);
            InsetRight = Math.Max(0, InsetRight);
            InsetTop = Math.Max(0, InsetTop);
            InsetBottom = Math.Max(0, InsetBottom);

            void clampHorizontal()
            {
                var maxSum = Math.Max(0, canvasW - minInner);
                var sumLR = InsetLeft + InsetRight;
                if (sumLR > maxSum && sumLR > 0)
                {
                    var excess = sumLR - maxSum;
                    InsetLeft -= excess * (InsetLeft / sumLR);
                    InsetRight -= excess * (InsetRight / sumLR);
                }
                InsetLeft = Math.Max(0, InsetLeft);
                InsetRight = Math.Max(0, InsetRight);
            }

            void clampVertical()
            {
                var maxSum = Math.Max(0, canvasH - minInner);
                var sumTB = InsetTop + InsetBottom;
                if (sumTB > maxSum && sumTB > 0)
                {
                    var excess = sumTB - maxSum;
                    InsetTop -= excess * (InsetTop / sumTB);
                    InsetBottom -= excess * (InsetBottom / sumTB);
                }
                InsetTop = Math.Max(0, InsetTop);
                InsetBottom = Math.Max(0, InsetBottom);
            }

            clampHorizontal();
            clampVertical();
            clampHorizontal();
        }
        finally
        {
            _muteNotify = false;
        }

        OnPropertyChanged(nameof(InsetLeft));
        OnPropertyChanged(nameof(InsetRight));
        OnPropertyChanged(nameof(InsetTop));
        OnPropertyChanged(nameof(InsetBottom));
    }

    public void ApplyPersisted(PreviewCropPersisted? source)
    {
        if (source == null) return;
        _muteNotify = true;
        try
        {
            InsetLeft = source.InsetLeft;
            InsetRight = source.InsetRight;
            InsetTop = source.InsetTop;
            InsetBottom = source.InsetBottom;
        }
        finally
        {
            _muteNotify = false;
        }

        OnPropertyChanged(nameof(InsetLeft));
        OnPropertyChanged(nameof(InsetRight));
        OnPropertyChanged(nameof(InsetTop));
        OnPropertyChanged(nameof(InsetBottom));
        _onChanged?.Invoke();
    }

    public static PreviewCropPersisted ToPersisted(PreviewCropInsets c) => new()
    {
        InsetLeft = c.InsetLeft,
        InsetRight = c.InsetRight,
        InsetTop = c.InsetTop,
        InsetBottom = c.InsetBottom,
    };
}
