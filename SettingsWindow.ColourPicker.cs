using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace GhostClip;

// The swatch buttons use Windows' standard colour picker. Choosing a colour
// updates the adjacent #RRGGBB field; existing live validation/persistence
// performs the actual widget update. Cancel leaves everything unchanged.
public partial class SettingsWindow
{
    private void ColourSwatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not string inputName ||
            FindName(inputName) is not TextBox hexInput)
        {
            return;
        }

        using Forms.ColorDialog dialog = new()
        {
            AllowFullOpen = true,
            FullOpen = true,
            AnyColor = true
        };

        if (WidgetAppearanceOptions.TryParseColour(
            hexInput.Text.Trim(), out System.Windows.Media.Color current))
        {
            dialog.Color = System.Drawing.Color.FromArgb(
                current.R, current.G, current.B);
        }
        else if (string.Equals(
                     hexInput.Text.Trim(),
                     WidgetAppearanceOptions.AutomaticTextColour,
                     StringComparison.OrdinalIgnoreCase) &&
                 FindName(inputName.Replace("TextBox", "Preview")) is Border preview &&
                 preview.Background is System.Windows.Media.SolidColorBrush brush)
        {
            // Auto has no fixed hex value. Start the picker with the
            // contrasting colour currently shown in its live swatch.
            System.Windows.Media.Color displayed = brush.Color;
            dialog.Color = System.Drawing.Color.FromArgb(
                displayed.R, displayed.G, displayed.B);
        }

        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (dialog.ShowDialog(new WindowOwner(hwnd)) == Forms.DialogResult.OK)
        {
            System.Drawing.Color selected = dialog.Color;
            hexInput.Text =
                $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
        }
    }

    private sealed class WindowOwner(IntPtr handle) : Forms.IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
