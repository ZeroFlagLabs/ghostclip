using System;
using System.Globalization;
using System.Windows.Media;

namespace GhostClip;

// All appearance defaults in one place. Preferences are only colours and
// geometry; they never contain screen captures or clipboard content.
internal static class WidgetAppearanceOptions
{
    public const string BackgroundColour = "#20242C";
    public const string BorderColour = "#454545";
    public const string PrivateOnColour = "#256A4A";
    public const string ReadyColour = "#2E455B";
    public const string CountdownColour = "#774E26";
    public const string PrivateOffColour = "#353535";
    public const string StatusOffColour = "#303030";
    public const string SettingsButtonColour = "#303030";
    public const string SettingsIconColour = "#FFFFFF";
    public const string ExitButtonColour = "#443035";
    public const string ExitIconColour = "#FFFFFF";
    public const string TitleColour = "#FFFFFF";
    public const string GhostColour = "#FFFFFF";

    // Automatic status text tracks the chosen button background so the
    // existing readable black/white contrast is preserved by default.
    public const string AutomaticTextColour = "Auto";

    public static string ValidatedTextColour(string? value)
    {
        if (string.Equals(value, AutomaticTextColour,
            StringComparison.OrdinalIgnoreCase))
        {
            return AutomaticTextColour;
        }

        return ValidatedColour(value, AutomaticTextColour);
    }

    public static Brush TextBrush(
        string? preference, string? backgroundHex, string defaultBackground)
    {
        return TryParseColour(preference, out _)
            ? Brush(preference, "#FFFFFF")
            : ReadableTextBrush(backgroundHex, defaultBackground);
    }

    public const bool ShowTitle = true;
    public const bool ShowGhost = true;

    // The widget-only LayoutTransform scale is independent of DPI and does
    // not apply to Settings or the full-screen screenshot selection overlay.
    public const int DefaultScalePercent = 100;
    public const int MinimumScalePercent = 70;
    public const int MaximumScalePercent = 150;

    public const double BackgroundOpacity = 90;
    public const double WidgetCorners = 8;
    public const double ButtonOpacity = 100;
    public const double ButtonCorners = 6;

    public static bool TryParseColour(string? text, out Color colour)
    {
        colour = default;
        if (text is null || text.Length != 7 || text[0] != '#')
        {
            return false;
        }

        // NumberStyles.HexNumber permits surrounding whitespace. Reject it
        // so only the advertised six hex digits are ever accepted.
        for (int i = 1; i < text.Length; i++)
        {
            if (!Uri.IsHexDigit(text[i]))
            {
                return false;
            }
        }

        if (!uint.TryParse(
                text.AsSpan(1),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out uint rgb) ||
            rgb > 0xFFFFFF)
        {
            return false;
        }

        colour = Color.FromRgb(
            (byte)(rgb >> 16),
            (byte)(rgb >> 8),
            (byte)rgb);
        return true;
    }

    public static string ValidatedColour(string? value, string fallback)
    {
        return TryParseColour(value, out _)
            ? value!.ToUpperInvariant()
            : fallback;
    }

    public static SolidColorBrush Brush(string? hex, string fallback, double opacity = 100)
    {
        Color colour = TryParseColour(hex, out Color parsed)
            ? parsed
            : (Color)ColorConverter.ConvertFromString(fallback)!;

        return new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(Math.Clamp(opacity, 0, 100) * 255 / 100),
            colour.R,
            colour.G,
            colour.B));
    }

    public static System.Windows.Media.Brush ReadableTextBrush(string? hex, string fallback)
    {
        Color colour = TryParseColour(hex, out Color parsed)
            ? parsed
            : (Color)ColorConverter.ConvertFromString(fallback)!;

        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        double luminance =
            0.2126 * Linear(colour.R) +
            0.7152 * Linear(colour.G) +
            0.0722 * Linear(colour.B);

        return luminance > 0.179 ? Brushes.Black : Brushes.White;
    }
}
