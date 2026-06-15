using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChtotibDocsPrintNET.Converters;
using ChtotibDocsPrintNET.ViewModels;

namespace ChtotibDocsPrintNET.Views
{
    public partial class PrintPage : UserControl
    {
        private string? _diplomaDragKey;
        private Point _diplomaGrabOffset;
        private int _guideDragIndex;
        private double _guideGrabDx;
        private string? _resizeWidthKey;
        private double _resizeStartMouseX;
        private double _resizeStartWidth;
        private (string Sheet, string Edge)? _cropRulerDrag;
        private string? _appendixDragTag;
        private Point _appendixGrabOffset;
        private FrameworkElement? _appendixDragElement;

        private enum AppendixSheetInteraction { None, Drag, ResizeWidth, ResizeHeight, GradesGap }

        private AppendixSheetInteraction _appendixInteraction;
        private Canvas? _appendixInteractionCanvas;
        private bool _appendixWidthGripFront;
        private string? _appendixWidthGripKey;
        private double _appendixWidthResizeStartMouseX;
        private double _appendixWidthResizeStartWidth;
        private bool _appendixHeightGripFront;
        private string? _appendixHeightGripKey;
        private double _appendixHeightResizeStartMouseY;
        private double _appendixHeightResizeStartHeight;
        public PrintPage()
        {
            InitializeComponent();
        }

        private void CropInsetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is PrintViewModel vm)
                vm.SaveCropInsetsAfterEditorEdit();
        }

        private void PreviewArea_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (ctrl && DataContext is PrintViewModel vm)
            {
                vm.FitPreviewToFrame = false;
                var notch = e.Delta / 120.0;
                vm.PreviewZoom = Math.Round(
                    Math.Clamp(vm.PreviewZoom + notch * 0.1, 0.25, 5.0),
                    2);
                e.Handled = true;
                return;
            }

            if (shift)
            {
                var notch = -(e.Delta / 120.0) * 48.0;
                var sv = PreviewScrollViewer;
                var next = sv.HorizontalOffset + notch;
                var max = Math.Max(0, sv.ScrollableWidth);
                sv.ScrollToHorizontalOffset(Math.Clamp(next, 0, max));
                e.Handled = true;
            }
        }

        private static Canvas? FindDiplomaOverlayCanvas(FrameworkElement fe)
        {
            for (DependencyObject? d = fe; d != null; d = VisualTreeHelper.GetParent(d))
            {
                if (d is Canvas { Name: nameof(DiplomaBackCanvas) or nameof(DiplomaFrontCanvas) } c)
                    return c;
            }

            return null;
        }

        private static bool IsDiplomaFrontCanvas(FrameworkElement fe) =>
            FindDiplomaOverlayCanvas(fe)?.Name == nameof(DiplomaFrontCanvas);

        private void DiplomaOverlay_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string key) return;
            var canvas = FindDiplomaOverlayCanvas(fe) ?? DiplomaBackCanvas;
            _diplomaDragKey = key;
            var p = e.GetPosition(canvas);
            _diplomaGrabOffset = new Point(p.X - Canvas.GetLeft(fe), p.Y - Canvas.GetTop(fe));
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void DiplomaOverlay_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_diplomaDragKey == null || sender is not FrameworkElement fe) return;
            if (!ReferenceEquals(Mouse.Captured, fe)) return;
            var canvas = FindDiplomaOverlayCanvas(fe) ?? DiplomaBackCanvas;
            var p = e.GetPosition(canvas);
            var left = p.X - _diplomaGrabOffset.X;
            var top = p.Y - _diplomaGrabOffset.Y;
            if (DataContext is PrintViewModel vm)
            {
                if (IsDiplomaFrontCanvas(fe))
                    vm.SetDiplomaFrontOverlayPosition(_diplomaDragKey, left, top);
                else
                    vm.SetDiplomaBackOverlayPosition(_diplomaDragKey, left, top);
            }
            e.Handled = true;
        }

        private void DiplomaOverlay_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (!ReferenceEquals(Mouse.Captured, fe)) return;
            var key = _diplomaDragKey;
            var front = IsDiplomaFrontCanvas(fe);
            fe.ReleaseMouseCapture();
            _diplomaDragKey = null;
            if (DataContext is PrintViewModel vm)
            {
                if (key != null)
                {
                    var w = fe is TextBlock tb ? tb.ActualWidth : fe.ActualWidth;
                    if (front)
                        vm.SnapDiplomaFrontOverlayToGuides(key, w);
                    else
                        vm.SnapDiplomaBackOverlayToGuides(key, w);
                }
                vm.SavePrintLayoutSilent();
            }
            e.Handled = true;
        }

        private void AppendixSheetCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Canvas canvas || DataContext is not PrintViewModel vm)
                return;

            var front = ReferenceEquals(canvas, AppendixFrontSheetCanvas);
            if (!TryResolveAppendixHit(e.OriginalSource as DependencyObject, front,
                    out var fe, out var interaction, out var keyOrTag))
                return;

            _appendixInteraction = interaction;
            _appendixInteractionCanvas = canvas;
            _appendixDragElement = fe;

            var p = e.GetPosition(canvas);
            switch (interaction)
            {
                case AppendixSheetInteraction.Drag:
                    _appendixDragTag = keyOrTag;
                    var key = keyOrTag[5..];
                    var (left, top) = front
                        ? vm.GetAppendixFrontOverlayPosition(key)
                        : vm.GetAppendixBackOverlayPosition(key);
                    _appendixGrabOffset = new Point(p.X - left, p.Y - top);
                    break;
                case AppendixSheetInteraction.ResizeWidth:
                    _appendixWidthGripFront = front;
                    _appendixWidthGripKey = keyOrTag;
                    _appendixWidthResizeStartMouseX = p.X;
                    _appendixWidthResizeStartWidth = front
                        ? vm.GetAppendixFrontOverlayCurrentWidth(keyOrTag)
                        : vm.GetAppendixBackOverlayCurrentWidth(keyOrTag);
                    break;
                case AppendixSheetInteraction.ResizeHeight:
                    _appendixHeightGripFront = front;
                    _appendixHeightGripKey = keyOrTag;
                    _appendixHeightResizeStartMouseY = p.Y;
                    _appendixHeightResizeStartHeight = front
                        ? vm.GetAppendixFrontOverlayCurrentHeight(keyOrTag)
                        : vm.GetAppendixBackOverlayCurrentHeight(keyOrTag);
                    break;
                case AppendixSheetInteraction.GradesGap:
                    _appendixGradesGapDragStartCanvasX = p.X;
                    _appendixGradesGapDragStartGapPx = vm.AppendixBackOverlay.GradesHoursGradeGapPx;
                    break;
            }

            canvas.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixSheetCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_appendixInteraction == AppendixSheetInteraction.None ||
                sender is not Canvas canvas ||
                !ReferenceEquals(Mouse.Captured, canvas) ||
                DataContext is not PrintViewModel vm)
                return;

            var p = e.GetPosition(canvas);
            switch (_appendixInteraction)
            {
                case AppendixSheetInteraction.Drag when _appendixDragTag != null:
                    var front = _appendixDragTag.StartsWith("AppF|", StringComparison.Ordinal);
                    var key = _appendixDragTag[5..];
                    var left = p.X - _appendixGrabOffset.X;
                    var top = p.Y - _appendixGrabOffset.Y;
                    if (front)
                        vm.SetAppendixFrontOverlayPosition(key, left, top);
                    else
                        vm.SetAppendixBackOverlayPosition(key, left, top);
                    break;
                case AppendixSheetInteraction.ResizeWidth when _appendixWidthGripKey != null:
                    var dx = p.X - _appendixWidthResizeStartMouseX;
                    if (_appendixWidthGripFront)
                        vm.SetAppendixFrontOverlayWidth(_appendixWidthGripKey, _appendixWidthResizeStartWidth + dx);
                    else
                        vm.SetAppendixBackOverlayWidth(_appendixWidthGripKey, _appendixWidthResizeStartWidth + dx);
                    break;
                case AppendixSheetInteraction.ResizeHeight when _appendixHeightGripKey != null:
                    var dy = p.Y - _appendixHeightResizeStartMouseY;
                    if (_appendixHeightGripFront)
                        vm.SetAppendixFrontOverlayHeight(_appendixHeightGripKey, _appendixHeightResizeStartHeight + dy);
                    else
                        vm.SetAppendixBackOverlayHeight(_appendixHeightGripKey, _appendixHeightResizeStartHeight + dy);
                    break;
                case AppendixSheetInteraction.GradesGap:
                    vm.ApplyAppendixGradesHoursGradeGapPx(
                        _appendixGradesGapDragStartGapPx + (p.X - _appendixGradesGapDragStartCanvasX));
                    break;
            }

            e.Handled = true;
        }

        private void AppendixSheetCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Canvas canvas || !ReferenceEquals(Mouse.Captured, canvas))
                return;

            var tag = _appendixDragTag;
            var element = _appendixDragElement;
            var interaction = _appendixInteraction;

            canvas.ReleaseMouseCapture();
            _appendixInteraction = AppendixSheetInteraction.None;
            _appendixInteractionCanvas = null;
            _appendixDragTag = null;
            _appendixDragElement = null;
            _appendixWidthGripKey = null;
            _appendixHeightGripKey = null;

            if (DataContext is PrintViewModel vm)
            {
                if (interaction == AppendixSheetInteraction.Drag && tag != null && tag.Length > 5 && element != null)
                {
                    var front = tag.StartsWith("AppF|", StringComparison.Ordinal);
                    var key = tag[5..];
                    var w = element is TextBlock ? element.ActualWidth : element.ActualWidth;
                    if (front)
                        vm.SnapAppendixFrontOverlayToGuides(key, w);
                    else
                        vm.SnapAppendixBackOverlayToGuides(key, w);
                }

                vm.SavePrintLayoutSilent();
            }

            e.Handled = true;
        }

        private static bool TryResolveAppendixHit(
            DependencyObject? source,
            bool canvasIsFront,
            out FrameworkElement fe,
            out AppendixSheetInteraction interaction,
            out string keyOrTag)
        {
            fe = null!;
            interaction = AppendixSheetInteraction.None;
            keyOrTag = "";

            for (var d = source; d != null; d = VisualTreeHelper.GetParent(d))
            {
                if (d is not FrameworkElement el || el.Tag is not string tag || string.IsNullOrEmpty(tag))
                    continue;

                if (tag.StartsWith("AppF|", StringComparison.Ordinal) ||
                    tag.StartsWith("AppB|", StringComparison.Ordinal))
                {
                    var tagFront = tag.StartsWith("AppF|", StringComparison.Ordinal);
                    if (tagFront != canvasIsFront)
                        return false;
                    fe = el;
                    interaction = AppendixSheetInteraction.Drag;
                    keyOrTag = tag;
                    return true;
                }

                if (TryParseAppendixWidthGripTag(tag, out var wf, out var wkey) && wf == canvasIsFront)
                {
                    fe = el;
                    interaction = AppendixSheetInteraction.ResizeWidth;
                    keyOrTag = wkey;
                    return true;
                }

                if (TryParseAppendixHeightGripTag(tag, out var hf, out var hkey) && hf == canvasIsFront)
                {
                    fe = el;
                    interaction = AppendixSheetInteraction.ResizeHeight;
                    keyOrTag = hkey;
                    return true;
                }

                if (string.Equals(tag, "AppB|GradesGap", StringComparison.Ordinal) && !canvasIsFront)
                {
                    fe = el;
                    interaction = AppendixSheetInteraction.GradesGap;
                    keyOrTag = tag;
                    return true;
                }
            }

            return false;
        }

        // Обработка на уровне AppendixFront/BackSheetCanvas; заглушки для совместимости с XAML.
        private void AppendixOverlay_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
        private void AppendixOverlay_PreviewMouseMove(object sender, MouseEventArgs e) { }
        private void AppendixOverlay_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

        private static bool TryParseAppendixWidthGripTag(string tag, out bool front, out string key)
        {
            const string f = "RWAppF|";
            const string b = "RWAppB|";
            if (tag.StartsWith(f, StringComparison.Ordinal))
            {
                front = true;
                key = tag[f.Length..];
                return key.Length > 0;
            }

            if (tag.StartsWith(b, StringComparison.Ordinal))
            {
                front = false;
                key = tag[b.Length..];
                return key.Length > 0;
            }

            front = false;
            key = "";
            return false;
        }

        private void AppendixWidthGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string tag)
                return;
            if (!TryParseAppendixWidthGripTag(tag, out var front, out var key) || DataContext is not PrintViewModel vm)
                return;
            var canvas = front ? AppendixFrontSheetCanvas : AppendixBackSheetCanvas;
            if (canvas == null) return;

            _appendixWidthGripFront = front;
            _appendixWidthGripKey = key;
            _appendixWidthResizeStartMouseX = e.GetPosition(canvas).X;
            _appendixWidthResizeStartWidth = front
                ? vm.GetAppendixFrontOverlayCurrentWidth(key)
                : vm.GetAppendixBackOverlayCurrentWidth(key);
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixWidthGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_appendixWidthGripKey == null || DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            var canvas = _appendixWidthGripFront ? AppendixFrontSheetCanvas : AppendixBackSheetCanvas;
            if (canvas == null) return;
            var dx = e.GetPosition(canvas).X - _appendixWidthResizeStartMouseX;
            if (_appendixWidthGripFront)
                vm.SetAppendixFrontOverlayWidth(_appendixWidthGripKey, _appendixWidthResizeStartWidth + dx);
            else
                vm.SetAppendixBackOverlayWidth(_appendixWidthGripKey, _appendixWidthResizeStartWidth + dx);
            e.Handled = true;
        }

        private void AppendixWidthGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            _appendixWidthGripKey = null;
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private static bool TryParseAppendixHeightGripTag(string tag, out bool front, out string key)
        {
            const string f = "RHAppF|";
            const string b = "RHAppB|";
            if (tag.StartsWith(f, StringComparison.Ordinal))
            {
                front = true;
                key = tag[f.Length..];
                return key.Length > 0;
            }

            if (tag.StartsWith(b, StringComparison.Ordinal))
            {
                front = false;
                key = tag[b.Length..];
                return key.Length > 0;
            }

            front = false;
            key = "";
            return false;
        }

        private void AppendixHeightGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string tag)
                return;
            if (!TryParseAppendixHeightGripTag(tag, out var front, out var key) || DataContext is not PrintViewModel vm)
                return;
            var canvas = front ? AppendixFrontSheetCanvas : AppendixBackSheetCanvas;
            if (canvas == null) return;

            _appendixHeightGripFront = front;
            _appendixHeightGripKey = key;
            _appendixHeightResizeStartMouseY = e.GetPosition(canvas).Y;
            _appendixHeightResizeStartHeight = front
                ? vm.GetAppendixFrontOverlayCurrentHeight(key)
                : vm.GetAppendixBackOverlayCurrentHeight(key);
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixHeightGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_appendixHeightGripKey == null || DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            var canvas = _appendixHeightGripFront ? AppendixFrontSheetCanvas : AppendixBackSheetCanvas;
            if (canvas == null) return;
            var dy = e.GetPosition(canvas).Y - _appendixHeightResizeStartMouseY;
            if (_appendixHeightGripFront)
                vm.SetAppendixFrontOverlayHeight(_appendixHeightGripKey, _appendixHeightResizeStartHeight + dy);
            else
                vm.SetAppendixBackOverlayHeight(_appendixHeightGripKey, _appendixHeightResizeStartHeight + dy);
            e.Handled = true;
        }

        private void AppendixHeightGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            _appendixHeightGripKey = null;
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private double _appendixGradesGapDragStartCanvasX;
        private double _appendixGradesGapDragStartGapPx;

        private void AppendixGradesSplitGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || DataContext is not PrintViewModel vm)
                return;
            if (AppendixBackSheetCanvas == null) return;
            _appendixGradesGapDragStartCanvasX = e.GetPosition(AppendixBackSheetCanvas).X;
            _appendixGradesGapDragStartGapPx = vm.AppendixBackOverlay.GradesHoursGradeGapPx;
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixGradesSplitGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            if (AppendixBackSheetCanvas == null) return;
            var dx = e.GetPosition(AppendixBackSheetCanvas).X - _appendixGradesGapDragStartCanvasX;
            vm.ApplyAppendixGradesHoursGradeGapPx(_appendixGradesGapDragStartGapPx + dx);
            e.Handled = true;
        }

        private void AppendixGradesSplitGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private int _practiceSplitGripIndex;
        private string _practiceSplitTableKind = "Study";
        private double _practiceSplitTableLeft;
        private double _practiceSplitTableWidth;
        private double _practiceGapDragStartCanvasX;
        private double _practiceGapDragStartGapPx;
        private string _practiceGapTableKind = "Study";

        private void AppendixPracticeSplitGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string tag || DataContext is not PrintViewModel vm)
                return;
            if (AppendixBackSheetCanvas == null) return;
            var parts = tag.Split('|');
            if (parts.Length < 3 || !int.TryParse(parts[2], out _practiceSplitGripIndex))
                return;

            _practiceSplitTableKind = parts[1];
            var o = vm.AppendixBackOverlay;
            if (_practiceSplitTableKind == "Production")
            {
                _practiceSplitTableLeft = o.ProductionPracticeListLeft;
                _practiceSplitTableWidth = o.ProductionPracticeListWidth;
            }
            else
            {
                _practiceSplitTableLeft = o.StudyPracticeListLeft;
                _practiceSplitTableWidth = o.StudyPracticeListWidth;
            }
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixPracticeSplitGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            if (AppendixBackSheetCanvas == null) return;

            var mouseX = e.GetPosition(AppendixBackSheetCanvas).X;
            var width = _practiceSplitTableWidth;
            if (width < 8) return;

            var o = vm.AppendixBackOverlay;
            var isProduction = _practiceSplitTableKind == "Production";
            var gapPx = isProduction ? o.ProductionPracticeColumnGapPx : o.StudyPracticeColumnGapPx;
            var activityShare = isProduction ? o.ProductionPracticeActivityShare : o.StudyPracticeActivityShare;

            if (_practiceSplitGripIndex == 0)
            {
                var (w0, gap, _, _) = AppendixPracticeColumnWidthMultiConverter.ComputeColumnWidths(
                    width, activityShare, isProduction ? o.ProductionPracticeMeansShareOfRemainder : o.StudyPracticeMeansShareOfRemainder, gapPx);
                var share = (mouseX - _practiceSplitTableLeft - gap * 0.5) / Math.Max(1, width - gap * 2);
                if (isProduction)
                    vm.ApplyProductionPracticeActivityShare(share);
                else
                    vm.ApplyStudyPracticeActivityShare(share);
            }
            else
            {
                var (w0, gap, _, _) = AppendixPracticeColumnWidthMultiConverter.ComputeColumnWidths(
                    width, activityShare, isProduction ? o.ProductionPracticeMeansShareOfRemainder : o.StudyPracticeMeansShareOfRemainder, gapPx);
                var rem = Math.Max(1, width - gap * 2 - w0);
                var share = (mouseX - _practiceSplitTableLeft - w0 - gap * 1.5) / rem;
                if (isProduction)
                    vm.ApplyProductionPracticeMeansShareOfRemainder(share);
                else
                    vm.ApplyStudyPracticeMeansShareOfRemainder(share);
            }

            e.Handled = true;
        }

        private void AppendixPracticeSplitGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private void AppendixPracticeGapGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string tag || DataContext is not PrintViewModel vm)
                return;
            if (AppendixBackSheetCanvas == null) return;
            var parts = tag.Split('|');
            if (parts.Length < 2) return;

            _practiceGapTableKind = parts[1];
            var o = vm.AppendixBackOverlay;
            _practiceGapDragStartGapPx = _practiceGapTableKind == "Production"
                ? o.ProductionPracticeColumnGapPx
                : o.StudyPracticeColumnGapPx;
            _practiceGapDragStartCanvasX = e.GetPosition(AppendixBackSheetCanvas).X;
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void AppendixPracticeGapGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            if (AppendixBackSheetCanvas == null) return;

            var dx = e.GetPosition(AppendixBackSheetCanvas).X - _practiceGapDragStartCanvasX;
            var gap = _practiceGapDragStartGapPx + dx;
            if (_practiceGapTableKind == "Production")
                vm.ApplyProductionPracticeColumnGapPx(gap);
            else
                vm.ApplyStudyPracticeColumnGapPx(gap);
            e.Handled = true;
        }

        private void AppendixPracticeGapGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        /// <returns>Org, Qual, … или null, если не RW_*.</returns>
        private static string? RwTagToOverlayKey(string tag) =>
            tag.StartsWith("RW_", StringComparison.Ordinal) ? tag[3..] : null;

        private void DiplomaWidthGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string tag)
                return;
            var key = RwTagToOverlayKey(tag);
            if (key == null || DataContext is not PrintViewModel vm)
                return;
            _resizeWidthKey = key;
            var canvas = FindDiplomaOverlayCanvas(fe) ?? DiplomaBackCanvas;
            _resizeStartMouseX = e.GetPosition(canvas).X;
            _resizeStartWidth = IsDiplomaFrontCanvas(fe)
                ? vm.GetDiplomaFrontOverlayCurrentWidth(key)
                : vm.GetDiplomaBackOverlayCurrentWidth(key);
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void DiplomaWidthGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_resizeWidthKey == null || DataContext is not PrintViewModel vm) return;
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            var canvas = FindDiplomaOverlayCanvas(fe) ?? DiplomaBackCanvas;
            var dx = e.GetPosition(canvas).X - _resizeStartMouseX;
            if (IsDiplomaFrontCanvas(fe))
                vm.SetDiplomaFrontOverlayWidth(_resizeWidthKey, _resizeStartWidth + dx);
            else
                vm.SetDiplomaBackOverlayWidth(_resizeWidthKey, _resizeStartWidth + dx);
            e.Handled = true;
        }

        private void DiplomaWidthGrip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || !ReferenceEquals(Mouse.Captured, fe)) return;
            fe.ReleaseMouseCapture();
            _resizeWidthKey = null;
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private static Canvas? FindAppendixOuterCanvas(Line line)
        {
            for (DependencyObject? d = line; d != null; d = VisualTreeHelper.GetParent(d))
            {
                if (d is Canvas c && Math.Abs(c.Width - 780) < 0.01 && Math.Abs(c.Height - 560) < 0.01)
                    return c;
            }

            return null;
        }

        private void DiplomaGuide_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Line line || line.Tag is not string tag)
                return;
            if (DataContext is not PrintViewModel vm)
                return;

            double gx;
            Point p;
            switch (tag)
            {
                case "Guide1":
                    _guideDragIndex = 1;
                    gx = vm.DiplomaBackOverlay.CenteringGuide1X;
                    p = e.GetPosition(DiplomaBackCanvas);
                    break;
                case "Guide2":
                    _guideDragIndex = 2;
                    gx = vm.DiplomaBackOverlay.CenteringGuide2X;
                    p = e.GetPosition(DiplomaBackCanvas);
                    break;
                case "GuideF1":
                    _guideDragIndex = 11;
                    gx = vm.DiplomaFrontOverlay.CenteringGuide1X;
                    p = e.GetPosition(DiplomaFrontCanvas);
                    break;
                case "GuideF2":
                    _guideDragIndex = 12;
                    gx = vm.DiplomaFrontOverlay.CenteringGuide2X;
                    p = e.GetPosition(DiplomaFrontCanvas);
                    break;
                case "AppGuideF1":
                    _guideDragIndex = 3;
                    gx = vm.AppendixFrontCenteringGuide1X;
                    var c1 = FindAppendixOuterCanvas(line);
                    if (c1 == null) return;
                    p = e.GetPosition(c1);
                    break;
                case "AppGuideF2":
                    _guideDragIndex = 4;
                    gx = vm.AppendixFrontCenteringGuide2X;
                    var c2 = FindAppendixOuterCanvas(line);
                    if (c2 == null) return;
                    p = e.GetPosition(c2);
                    break;
                case "AppGuideB1":
                    _guideDragIndex = 5;
                    gx = vm.AppendixCenteringGuide1X;
                    var c3 = FindAppendixOuterCanvas(line);
                    if (c3 == null) return;
                    p = e.GetPosition(c3);
                    break;
                case "AppGuideB2":
                    _guideDragIndex = 6;
                    gx = vm.AppendixCenteringGuide2X;
                    var c4 = FindAppendixOuterCanvas(line);
                    if (c4 == null) return;
                    p = e.GetPosition(c4);
                    break;
                default:
                    return;
            }

            _guideGrabDx = p.X - gx;
            line.CaptureMouse();
            e.Handled = true;
        }

        private void DiplomaGuide_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_guideDragIndex == 0 || DataContext is not PrintViewModel vm) return;
            if (sender is not Line line || !ReferenceEquals(Mouse.Captured, line)) return;

            double maxX;
            Point p;
            if (_guideDragIndex <= 2)
            {
                p = e.GetPosition(DiplomaBackCanvas);
                maxX = 730;
            }
            else if (_guideDragIndex is 11 or 12)
            {
                p = e.GetPosition(DiplomaFrontCanvas);
                maxX = 730;
            }
            else
            {
                var c = FindAppendixOuterCanvas(line);
                if (c == null) return;
                p = e.GetPosition(c);
                maxX = 780;
            }

            var nx = Math.Clamp(p.X - _guideGrabDx, 0, maxX);
            switch (_guideDragIndex)
            {
                case 1: vm.DiplomaBackOverlay.CenteringGuide1X = nx; break;
                case 2: vm.DiplomaBackOverlay.CenteringGuide2X = nx; break;
                case 11: vm.DiplomaFrontOverlay.CenteringGuide1X = nx; break;
                case 12: vm.DiplomaFrontOverlay.CenteringGuide2X = nx; break;
                case 3: vm.AppendixFrontCenteringGuide1X = nx; break;
                case 4: vm.AppendixFrontCenteringGuide2X = nx; break;
                case 5: vm.AppendixCenteringGuide1X = nx; break;
                case 6: vm.AppendixCenteringGuide2X = nx; break;
            }

            e.Handled = true;
        }

        private void DiplomaGuide_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Line line && ReferenceEquals(Mouse.Captured, line))
                line.ReleaseMouseCapture();
            _guideDragIndex = 0;
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }

        private void CropRuler_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Line line || line.Tag is not string tag || DataContext is not PrintViewModel)
                return;
            var parts = tag.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || (parts[0] != "DiplomaF" && parts[0] != "DiplomaB" && parts[0] != "Appendix"))
                return;
            _cropRulerDrag = (parts[0], parts[1]);
            line.CaptureMouse();
            e.Handled = true;
        }

        private void CropRuler_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_cropRulerDrag == null || sender is not Line line || DataContext is not PrintViewModel vm)
                return;
            if (!ReferenceEquals(Mouse.Captured, line)) return;
            var canvas = VisualTreeHelper.GetParent(line) as Canvas;
            if (canvas == null) return;
            var p = e.GetPosition(canvas);
            const double minInner = 40;
            var cw = canvas.Width;
            var ch = canvas.Height;
            var c = _cropRulerDrag.Value.Sheet switch
            {
                "Appendix" => vm.PreviewCropAppendix,
                "DiplomaF" => vm.PreviewCropDiplomaFront,
                "DiplomaB" => vm.PreviewCropDiplomaBack,
                _ => vm.PreviewCropDiplomaBack,
            };
            switch (_cropRulerDrag.Value.Edge)
            {
                case "L":
                    c.InsetLeft = Math.Clamp(p.X, 0, cw - c.InsetRight - minInner);
                    break;
                case "R":
                    c.InsetRight = Math.Clamp(cw - p.X, 0, cw - c.InsetLeft - minInner);
                    break;
                case "T":
                    c.InsetTop = Math.Clamp(p.Y, 0, ch - c.InsetBottom - minInner);
                    break;
                case "B":
                    c.InsetBottom = Math.Clamp(ch - p.Y, 0, ch - c.InsetTop - minInner);
                    break;
            }
            e.Handled = true;
        }

        private void CropRuler_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Line line) return;
            if (ReferenceEquals(Mouse.Captured, line))
                line.ReleaseMouseCapture();
            _cropRulerDrag = null;
            if (DataContext is PrintViewModel vm)
                vm.SavePrintLayoutSilent();
            e.Handled = true;
        }
    }
}
