using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GhostClip;

public partial class MainWindow
{
    private const double OriginalWidgetHeight = 40;
    private int _appliedWidgetScalePercent;
    private bool _scaleRepositionPending;

    private void ApplyWidgetScale()
    {
        // A LayoutTransform scales both rendering and layout/hit testing.
        // RenderTransform would leave an unscaled (and sometimes invisible)
        // window around the buttons, breaking docking and mouse interaction.
        int percent = Math.Clamp(
            _settings.WidgetScalePercent,
            WidgetAppearanceOptions.MinimumScalePercent,
            WidgetAppearanceOptions.MaximumScalePercent);
        if (_appliedWidgetScalePercent == percent)
        {
            return;
        }

        _appliedWidgetScalePercent = percent;
        double factor = percent / 100.0;

        // Width is SizeToContent in MainWindow.xaml. WPF computes the new
        // window width from the scaled Border's desired size.
        WidgetBorder.LayoutTransform = new ScaleTransform(factor, factor);
        Height = OriginalWidgetHeight * factor;

        if (IsLoaded && !_scaleRepositionPending)
        {
            _scaleRepositionPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _scaleRepositionPending = false;
                if (IsDocked)
                {
                    PositionBesideTaskbar();
                }
                else
                {
                    KeepFloatingWidgetInWorkArea();
                }
            }), DispatcherPriority.Loaded);
        }
    }

    private void KeepFloatingWidgetInWorkArea()
    {
        // Preserve the existing top-left position unless enlarging the bar
        // would push it outside the current monitor's usable work area.
        if (!IsLoaded || IsDocked)
        {
            return;
        }

        if (!TryGetMonitorAreas(out _, out Rect workArea))
        {
            workArea = SystemParameters.WorkArea;
        }

        double width = ActualWidth > 0 ? ActualWidth : 1;
        double height = ActualHeight > 0 ? ActualHeight : Height;
        double newLeft = Math.Clamp(
            Left, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
        double newTop = Math.Clamp(
            Top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));

        if (Math.Abs(newLeft - Left) > 0.01 ||
            Math.Abs(newTop - Top) > 0.01)
        {
            Left = newLeft;
            Top = newTop;
            SaveCurrentPosition();
        }
    }
}
