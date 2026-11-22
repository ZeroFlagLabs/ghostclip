using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostClip;

// Button labels default to Auto (contrasting black/white), but each status
// can have its own user-selected text colour. This does not affect the
// separate icon or GhostClip branding colours.
public partial class SettingsWindow
{
    public string PrivateOnTextColour { get; private set; } =
        WidgetAppearanceOptions.AutomaticTextColour;
    public string PrivateOffTextColour { get; private set; } =
        WidgetAppearanceOptions.AutomaticTextColour;
    public string ReadyTextColour { get; private set; } =
        WidgetAppearanceOptions.AutomaticTextColour;
    public string CountdownTextColour { get; private set; } =
        WidgetAppearanceOptions.AutomaticTextColour;
    public string StatusOffTextColour { get; private set; } =
        WidgetAppearanceOptions.AutomaticTextColour;

    private void InitialiseStatusTextColours(
        string privateOn, string privateOff, string ready,
        string countdown, string statusOff)
    {
        PrivateOnTextColour = WidgetAppearanceOptions.ValidatedTextColour(privateOn);
        PrivateOffTextColour = WidgetAppearanceOptions.ValidatedTextColour(privateOff);
        ReadyTextColour = WidgetAppearanceOptions.ValidatedTextColour(ready);
        CountdownTextColour = WidgetAppearanceOptions.ValidatedTextColour(countdown);
        StatusOffTextColour = WidgetAppearanceOptions.ValidatedTextColour(statusOff);

        PrivateOnTextColourTextBox.Text = PrivateOnTextColour;
        PrivateOffTextColourTextBox.Text = PrivateOffTextColour;
        ReadyTextColourTextBox.Text = ReadyTextColour;
        CountdownTextColourTextBox.Text = CountdownTextColour;
        StatusOffTextColourTextBox.Text = StatusOffTextColour;
        RefreshStatusTextColourFields();
    }

    private void SubscribeStatusTextColourChanges()
    {
        PrivateOnTextColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        PrivateOffTextColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        ReadyTextColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        CountdownTextColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        StatusOffTextColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
    }

    private void RefreshStatusTextColourFields()
    {
        PrivateOnTextColour = UpdateTextColourField(
            PrivateOnTextColourTextBox, PrivateOnTextColourPreview,
            PrivateOnTextColour, PrivateOnButtonColour,
            WidgetAppearanceOptions.PrivateOnColour);

        PrivateOffTextColour = UpdateTextColourField(
            PrivateOffTextColourTextBox, PrivateOffTextColourPreview,
            PrivateOffTextColour, PrivateOffButtonColour,
            WidgetAppearanceOptions.PrivateOffColour);

        ReadyTextColour = UpdateTextColourField(
            ReadyTextColourTextBox, ReadyTextColourPreview,
            ReadyTextColour, ReadyButtonColour,
            WidgetAppearanceOptions.ReadyColour);

        CountdownTextColour = UpdateTextColourField(
            CountdownTextColourTextBox, CountdownTextColourPreview,
            CountdownTextColour, CountdownButtonColour,
            WidgetAppearanceOptions.CountdownColour);

        StatusOffTextColour = UpdateTextColourField(
            StatusOffTextColourTextBox, StatusOffTextColourPreview,
            StatusOffTextColour, StatusOffButtonColour,
            WidgetAppearanceOptions.StatusOffColour);
    }

    private static string UpdateTextColourField(
        TextBox input, Border sample, string previousValue,
        string backgroundColour, string defaultBackground)
    {
        if (string.Equals(input.Text.Trim(),
            WidgetAppearanceOptions.AutomaticTextColour,
            StringComparison.OrdinalIgnoreCase))
        {
            input.BorderBrush = Brushes.Gray;
            input.ToolTip = "Auto follows the button background; or choose #RRGGBB";
            sample.Background = WidgetAppearanceOptions.ReadableTextBrush(
                backgroundColour, defaultBackground);
            return WidgetAppearanceOptions.AutomaticTextColour;
        }

        if (WidgetAppearanceOptions.TryParseColour(
            input.Text.Trim(), out Color selected))
        {
            sample.Background = new SolidColorBrush(selected);
            input.BorderBrush = Brushes.Gray;
            input.ToolTip = "Hex text colour (#RRGGBB), or type Auto";
            return input.Text.Trim().ToUpperInvariant();
        }

        // Partial edits and invalid hex must not replace the previous saved
        // value. In Auto mode, retain the actual contrast preview.
        input.BorderBrush = Brushes.IndianRed;
        input.ToolTip = "Enter Auto or a six-digit hex colour (#RRGGBB)";
        sample.Background = string.Equals(
            previousValue, WidgetAppearanceOptions.AutomaticTextColour,
            StringComparison.OrdinalIgnoreCase)
            ? WidgetAppearanceOptions.ReadableTextBrush(
                backgroundColour, defaultBackground)
            : WidgetAppearanceOptions.Brush(previousValue, "#FFFFFF");
        return previousValue;
    }

    private void ResetStatusTextColourControls()
    {
        // Called under _synchronising by the existing appearance reset.
        PrivateOnTextColourTextBox.Text = WidgetAppearanceOptions.AutomaticTextColour;
        PrivateOffTextColourTextBox.Text = WidgetAppearanceOptions.AutomaticTextColour;
        ReadyTextColourTextBox.Text = WidgetAppearanceOptions.AutomaticTextColour;
        CountdownTextColourTextBox.Text = WidgetAppearanceOptions.AutomaticTextColour;
        StatusOffTextColourTextBox.Text = WidgetAppearanceOptions.AutomaticTextColour;
    }
}
