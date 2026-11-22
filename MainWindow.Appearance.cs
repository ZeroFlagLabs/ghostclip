using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostClip;

public partial class MainWindow
{
    // Surface opacity is independent of button opacity. Changing either does
    // not hide button labels or change GhostClip's clipboard lifecycle.
    private void ApplyWidgetAppearance()
    {
        WidgetBorder.Background = _settings.ShowWidgetBackground
            ? WidgetAppearanceOptions.Brush(
                _settings.WidgetBackgroundColour,
                WidgetAppearanceOptions.BackgroundColour,
                _settings.WidgetBackgroundOpacityPercent)
            : Brushes.Transparent;

        WidgetBorder.BorderThickness =
            _settings.ShowWidgetBorder ? new Thickness(1) : new Thickness(0);

        WidgetBorder.BorderBrush = WidgetAppearanceOptions.Brush(
            _settings.WidgetBorderColour,
            WidgetAppearanceOptions.BorderColour);

        WidgetBorder.CornerRadius = new CornerRadius(
            Math.Clamp(_settings.WidgetCornerRadius, 0, 16));

        Resources["GhostButtonCornerRadius"] = new CornerRadius(
            Math.Clamp(_settings.ButtonCornerRadius, 0, 12));

        SettingsButton.Background = WidgetAppearanceOptions.Brush(
            _settings.SettingsButtonColour,
            WidgetAppearanceOptions.SettingsButtonColour,
            _settings.ButtonOpacityPercent);
        SettingsButton.Foreground = WidgetAppearanceOptions.Brush(
            _settings.SettingsIconColour,
            WidgetAppearanceOptions.SettingsIconColour);

        ExitButton.Background = WidgetAppearanceOptions.Brush(
            _settings.ExitButtonColour,
            WidgetAppearanceOptions.ExitButtonColour,
            _settings.ButtonOpacityPercent);
        ExitCross.Stroke = WidgetAppearanceOptions.Brush(
            _settings.ExitIconColour,
            WidgetAppearanceOptions.ExitIconColour);

        DragArea.Foreground = WidgetAppearanceOptions.Brush(
            _settings.TitleColour,
            WidgetAppearanceOptions.TitleColour);
        GhostIconShape.Fill = WidgetAppearanceOptions.Brush(
            _settings.GhostColour,
            WidgetAppearanceOptions.GhostColour);

        GhostIconContainer.Visibility = _settings.ShowGhost
            ? Visibility.Visible : Visibility.Collapsed;
        DragArea.Visibility = _settings.ShowTitle
            ? Visibility.Visible : Visibility.Collapsed;

        // With both hidden, retain a tiny draggable grip so a free-floating
        // widget can always be repositioned while it is unlocked.
        CompactDragHandle.Visibility =
            !_settings.ShowGhost && !_settings.ShowTitle
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetButtonAppearance(
        Button button, string? hex, string defaultColour,
        string? textColour = WidgetAppearanceOptions.AutomaticTextColour)
    {
        button.Background = WidgetAppearanceOptions.Brush(
            hex, defaultColour, _settings.ButtonOpacityPercent);

        button.Foreground = WidgetAppearanceOptions.TextBrush(
            textColour, hex, defaultColour);
    }
}
