using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace GhostClip;

public partial class SettingsWindow : Window
{
    public event Action<SettingsWindow>? PreferencesChanged;
    public event Action? ResetPositionRequested;

    public int ClipboardTimeoutSeconds { get; private set; }
    public bool PositionLocked { get; private set; }
    public bool StartInPrivateMode { get; private set; }
    public bool AlwaysOnTop { get; private set; }
    public int WidgetScalePercent { get; private set; }
    public string DockPosition { get; private set; }
    public bool ShowWidgetBackground { get; private set; }
    public bool ShowWidgetBorder { get; private set; }
    public double WidgetBackgroundOpacityPercent { get; private set; }
    public string WidgetBackgroundColour { get; private set; }
    public string WidgetBorderColour { get; private set; }
    public double WidgetCornerRadius { get; private set; }
    public string PrivateOnButtonColour { get; private set; }
    public string ReadyButtonColour { get; private set; }
    public string CountdownButtonColour { get; private set; }
    public double ButtonOpacityPercent { get; private set; }
    public double ButtonCornerRadius { get; private set; }

    private bool _synchronising;

    internal SettingsWindow(
        int clipboardTimeoutSeconds,
        bool positionLocked,
        bool startInPrivateMode,
        bool alwaysOnTop,
        int widgetScalePercent,
        string dockPosition,
        bool showWidgetBackground,
        bool showWidgetBorder,
        double widgetBackgroundOpacityPercent,
        string widgetBackgroundColour,
        string widgetBorderColour,
        double widgetCornerRadius,
        string privateOnButtonColour,
        string readyButtonColour,
        string countdownButtonColour,
        double buttonOpacityPercent,
        double buttonCornerRadius,
        bool showGhost,
        bool showTitle,
        string ghostColour,
        string titleColour,
        string privateOffColour,
        string statusOffColour,
        string settingsButtonColour,
        string settingsIconColour,
        string exitButtonColour,
        string exitIconColour,
        string privateOnTextColour,
        string privateOffTextColour,
        string readyTextColour,
        string countdownTextColour,
        string statusOffTextColour,
        bool checkForUpdatesOnLaunch,
        GitHubReleaseChecker releaseChecker)
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            // WindowChrome draws the ghost title bar; Windows must not
            // retain underlying native caption controls or system menu.
            RemoveTitleBarControlButtons();
            EnableDarkTitleBar();
        };

        ClipboardTimeoutSeconds = clipboardTimeoutSeconds;
        PositionLocked = positionLocked;
        StartInPrivateMode = startInPrivateMode;
        AlwaysOnTop = alwaysOnTop;
        WidgetScalePercent = widgetScalePercent;
        DockPosition = dockPosition;
        ShowWidgetBackground = showWidgetBackground;
        ShowWidgetBorder = showWidgetBorder;
        WidgetBackgroundOpacityPercent = widgetBackgroundOpacityPercent;
        WidgetBackgroundColour = widgetBackgroundColour;
        WidgetBorderColour = widgetBorderColour;
        WidgetCornerRadius = widgetCornerRadius;
        PrivateOnButtonColour = privateOnButtonColour;
        ReadyButtonColour = readyButtonColour;
        CountdownButtonColour = countdownButtonColour;
        ButtonOpacityPercent = buttonOpacityPercent;
        ButtonCornerRadius = buttonCornerRadius;

        // Populate before subscribing: opening Settings must never rewrite
        // the user's saved preferences or move the widget.
        TimeoutTextBox.Text = clipboardTimeoutSeconds.ToString();
        PositionLockedCheckBox.IsChecked = positionLocked;
        StartPrivateCheckBox.IsChecked = startInPrivateMode;
        AlwaysOnTopCheckBox.IsChecked = alwaysOnTop;
        WidgetScaleSlider.Value = widgetScalePercent;
        LaunchWithWindowsCheckBox.IsChecked = WindowsStartupRegistration.IsEnabled();
        DockLeftRadioButton.IsChecked = dockPosition == "Left";
        DockRightRadioButton.IsChecked = dockPosition == "Right";
        FloatingRadioButton.IsChecked = dockPosition == "Floating";
        ShowWidgetBackgroundCheckBox.IsChecked = showWidgetBackground;
        ShowWidgetBorderCheckBox.IsChecked = showWidgetBorder;
        BackgroundOpacitySlider.Value = widgetBackgroundOpacityPercent;
        BackgroundColourTextBox.Text = widgetBackgroundColour;
        BorderColourTextBox.Text = widgetBorderColour;
        WidgetCornerSlider.Value = widgetCornerRadius;
        PrivateColourTextBox.Text = privateOnButtonColour;
        ReadyColourTextBox.Text = readyButtonColour;
        CountdownColourTextBox.Text = countdownButtonColour;
        ButtonOpacitySlider.Value = buttonOpacityPercent;
        ButtonCornerSlider.Value = buttonCornerRadius;
        RefreshColourFields();

        InitialiseExtraAppearance(
            showGhost, showTitle, ghostColour, titleColour,
            privateOffColour, statusOffColour, settingsButtonColour,
            settingsIconColour, exitButtonColour, exitIconColour);
        InitialiseStatusTextColours(
            privateOnTextColour, privateOffTextColour,
            readyTextColour, countdownTextColour, statusOffTextColour);
        InitialiseAbout(checkForUpdatesOnLaunch, releaseChecker);

        StartPrivateCheckBox.Checked += (_, _) => ApplyInputChanges();
        StartPrivateCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        AlwaysOnTopCheckBox.Checked += (_, _) => ApplyInputChanges();
        AlwaysOnTopCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        LaunchWithWindowsCheckBox.Checked += LaunchWithWindowsCheckBox_Changed;
        LaunchWithWindowsCheckBox.Unchecked += LaunchWithWindowsCheckBox_Changed;
        PositionLockedCheckBox.Checked += (_, _) => ApplyInputChanges();
        PositionLockedCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        DockLeftRadioButton.Checked += (_, _) => ApplyInputChanges();
        DockRightRadioButton.Checked += (_, _) => ApplyInputChanges();
        FloatingRadioButton.Checked += (_, _) =>
        {
            if (!_synchronising)
            {
                // Selecting Free-floating means it should be movable straight
                // away. Lock can still be enabled afterwards if desired.
                PositionLockedCheckBox.IsChecked = false;
                ApplyInputChanges();
            }
        };
        ShowWidgetBackgroundCheckBox.Checked += (_, _) => ApplyInputChanges();
        ShowWidgetBackgroundCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        ShowWidgetBorderCheckBox.Checked += (_, _) => ApplyInputChanges();
        ShowWidgetBorderCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        BackgroundOpacitySlider.ValueChanged += (_, _) => ApplyInputChanges();
        WidgetCornerSlider.ValueChanged += (_, _) => ApplyInputChanges();
        ButtonOpacitySlider.ValueChanged += (_, _) => ApplyInputChanges();
        ButtonCornerSlider.ValueChanged += (_, _) => ApplyInputChanges();
        WidgetScaleSlider.ValueChanged += (_, _) => ApplyInputChanges();
        BackgroundColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        BorderColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        PrivateColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        ReadyColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        CountdownColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        TimeoutTextBox.TextChanged += (_, _) => ApplyInputChanges();
        SubscribeExtraAppearanceChanges();
        SubscribeStatusTextColourChanges();
    }

    // Called after a drag changes a docked widget into Free-floating.
    // Sync the still-open Settings radio buttons without moving the window
    // or applying a second, circular settings update.
    public void ReflectFreeFloatingAfterDrag()
    {
        _synchronising = true;
        try
        {
            DockPosition = "Floating";
            FloatingRadioButton.IsChecked = true;
            PositionLocked = false;
            PositionLockedCheckBox.IsChecked = false;
        }
        finally
        {
            _synchronising = false;
        }
    }

    private void LaunchWithWindowsCheckBox_Changed(
        object sender, RoutedEventArgs e)
    {
        if (_synchronising)
        {
            return;
        }

        bool requested = LaunchWithWindowsCheckBox.IsChecked == true;

        if (WindowsStartupRegistration.TrySetEnabled(requested, out string? error))
        {
            return;
        }

        // Reflect the real Windows configuration if registration was denied.
        _synchronising = true;
        try
        {
            LaunchWithWindowsCheckBox.IsChecked =
                WindowsStartupRegistration.IsEnabled();
        }
        finally
        {
            _synchronising = false;
        }

        MessageBox.Show(
            this,
            error ?? "Unable to change the Windows startup setting.",
            "GhostClip",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ApplyInputChanges()
    {
        if (_synchronising)
        {
            return;
        }

        // An incomplete number is never persisted. Other controls remain
        // usable while the user is editing the number.
        bool validTimeout = int.TryParse(
            TimeoutTextBox.Text, out int timeout) &&
            timeout >= 5 && timeout <= 300;

        TimeoutTextBox.BorderBrush = validTimeout
            ? new SolidColorBrush(Color.FromRgb(96, 96, 96))
            : Brushes.IndianRed;

        if (validTimeout)
        {
            ClipboardTimeoutSeconds = timeout;
        }

        PositionLocked = PositionLockedCheckBox.IsChecked == true;
        StartInPrivateMode = StartPrivateCheckBox.IsChecked == true;
        AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        WidgetScalePercent = (int)Math.Clamp(
            Math.Round(WidgetScaleSlider.Value / 5) * 5,
            WidgetAppearanceOptions.MinimumScalePercent,
            WidgetAppearanceOptions.MaximumScalePercent);
        DockPosition = DockLeftRadioButton.IsChecked == true
            ? "Left"
            : DockRightRadioButton.IsChecked == true ? "Right" : "Floating";
        ShowWidgetBackground = ShowWidgetBackgroundCheckBox.IsChecked == true;
        ShowWidgetBorder = ShowWidgetBorderCheckBox.IsChecked == true;
        WidgetBackgroundOpacityPercent = Math.Clamp(
            BackgroundOpacitySlider.Value, 20, 100);
        WidgetCornerRadius = Math.Clamp(WidgetCornerSlider.Value, 0, 16);
        ButtonOpacityPercent = Math.Clamp(ButtonOpacitySlider.Value, 60, 100);
        ButtonCornerRadius = Math.Clamp(ButtonCornerSlider.Value, 0, 12);
        RefreshColourFields();
        ReadExtraAppearanceSettings();
        RefreshStatusTextColourFields();
        CheckForUpdatesOnLaunch =
            CheckUpdatesOnLaunchCheckBox.IsChecked == true;

        PreferencesChanged?.Invoke(this);
    }

    private void RefreshColourFields()
    {
        WidgetBackgroundColour = UpdateColourField(
            BackgroundColourTextBox, BackgroundColourPreview,
            WidgetBackgroundColour);
        WidgetBorderColour = UpdateColourField(
            BorderColourTextBox, BorderColourPreview,
            WidgetBorderColour);
        PrivateOnButtonColour = UpdateColourField(
            PrivateColourTextBox, PrivateColourPreview,
            PrivateOnButtonColour);
        ReadyButtonColour = UpdateColourField(
            ReadyColourTextBox, ReadyColourPreview,
            ReadyButtonColour);
        CountdownButtonColour = UpdateColourField(
            CountdownColourTextBox, CountdownColourPreview,
            CountdownButtonColour);
    }

    private static string UpdateColourField(
        TextBox input, Border sample, string previousValue)
    {
        bool valid = WidgetAppearanceOptions.TryParseColour(
            input.Text.Trim(), out Color colour);

        input.BorderBrush = valid ? Brushes.Gray : Brushes.IndianRed;
        input.ToolTip = valid ? "Hex colour (#RRGGBB)" :
            "Enter a six-digit hex colour, for example #256A4A";

        if (valid)
        {
            sample.Background = new SolidColorBrush(colour);
            return input.Text.Trim().ToUpperInvariant();
        }

        sample.Background = WidgetAppearanceOptions.Brush(
            previousValue, WidgetAppearanceOptions.BackgroundColour);
        return previousValue;
    }

    private void ResetAppearanceButton_Click(object sender, RoutedEventArgs e)
    {
        // Revert only appearance. Privacy mode, timeout and position remain.
        _synchronising = true;
        try
        {
            ShowWidgetBackgroundCheckBox.IsChecked = true;
            ShowWidgetBorderCheckBox.IsChecked = true;
            BackgroundOpacitySlider.Value = WidgetAppearanceOptions.BackgroundOpacity;
            BackgroundColourTextBox.Text = WidgetAppearanceOptions.BackgroundColour;
            BorderColourTextBox.Text = WidgetAppearanceOptions.BorderColour;
            WidgetCornerSlider.Value = WidgetAppearanceOptions.WidgetCorners;
            PrivateColourTextBox.Text = WidgetAppearanceOptions.PrivateOnColour;
            ReadyColourTextBox.Text = WidgetAppearanceOptions.ReadyColour;
            CountdownColourTextBox.Text = WidgetAppearanceOptions.CountdownColour;
            ButtonOpacitySlider.Value = WidgetAppearanceOptions.ButtonOpacity;
            ButtonCornerSlider.Value = WidgetAppearanceOptions.ButtonCorners;
            WidgetScaleSlider.Value =
                WidgetAppearanceOptions.DefaultScalePercent;
            ResetExtraAppearanceControls();
            ResetStatusTextColourControls();
        }
        finally
        {
            _synchronising = false;
        }

        ApplyInputChanges();
    }

    private void EnableDarkTitleBar()
    {
        int enabled = 1;
        _ = DwmSetWindowAttribute(
            new WindowInteropHelper(this).Handle,
            20, // DWMWA_USE_IMMERSIVE_DARK_MODE on Windows 11
            ref enabled,
            sizeof(int));
    }

    private void ResetPositionButton_Click(object sender, RoutedEventArgs e)
    {
        // MainWindow owns the position and persists it; Settings stays open.
        ResetPositionRequested?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Preferences have already been applied and persisted.
        Close();
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int valueSize);
}
