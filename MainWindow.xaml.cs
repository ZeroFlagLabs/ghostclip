using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GhostClip;

public partial class MainWindow : Window
{
    private const int WmClipboardUpdate = 0x031D;
    private const string ExcludeFromClipboardHistoryFormat =
        "ExcludeClipboardContentFromMonitorProcessing";

    private const string CanIncludeInClipboardHistoryFormat =
        "CanIncludeInClipboardHistory";

    private const string CanUploadToCloudClipboardFormat =
        "CanUploadToCloudClipboard";

    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    private const uint SrcCopy = 0x00CC0020;
    private const uint CaptureBlt = 0x40000000;

    private readonly DispatcherTimer _countdownTimer;
    private readonly GitHubReleaseChecker _updateChecker = new();
    private AppSettings _settings;

    private bool _privacyMode;
    private bool _hasActiveItem;
    private bool _positionLocked;
    private bool _changingClipboard;
    private bool _captureInProgress;

    private int _secondsRemaining;
    private DateTimeOffset? _activeItemExpiresAtUtc;

    private DataObject? _activeClipboardData;

    private HwndSource? _windowSource;
    private IntPtr _windowHandle;
    private SettingsWindow? _settingsWindow;

    private static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ZeroFlagLabs",
            "GhostClip");

    private static string SettingsPath =>
        Path.Combine(SettingsDirectory, "settings.json");

    public MainWindow()
    {
        InitializeComponent();

        // Windows sign-in launch must not steal focus from the foreground
        // application. Manual launches retain normal activation behaviour.
        ShowActivated = !WindowsStartupRegistration.IsStartupLaunch();

        _printScreenHookCallback = PrintScreenHook;
        _settings = LoadSettings();
        // Existing installations have no AlwaysOnTop key. The settings model
        // defaults it to true, matching the established v1 widget behaviour.
        Topmost = _settings.AlwaysOnTop;
        ApplyWidgetScale();
        // Apply the saved privacy mode before the native window is initialised,
        // so OnSourceInitialized can register Print Screen immediately.
        _privacyMode = _settings.StartInPrivateMode;
        _positionLocked = _settings.PositionLocked;
        _secondsRemaining = _settings.ClipboardTimeoutSeconds;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _countdownTimer.Tick += CountdownTimer_Tick;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(_windowHandle);

        _windowSource?.AddHook(WindowProc);
        InitialiseDockingNotifications();

        AddClipboardFormatListener(_windowHandle);
        RegisterSessionNotifications();

        // Only claim Print Screen while Private Mode is armed.
        UpdateCaptureHotkeyRegistration();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(_windowHandle);
            UnregisterSessionNotifications();

            ReleaseCaptureHotkeys();
        }

        _windowSource?.RemoveHook(WindowProc);
        StopTaskbarOverlayWatch();

        base.OnClosed(e);
    }

    private IntPtr WindowProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg == WmClipboardUpdate &&
            !_changingClipboard &&
            _hasActiveItem)
        {
            CheckClipboardOwnership();
        }

        // Clear our temporary item when Windows locks, disconnects,
        // suspends or ends the current session.
        HandleSessionMessage(msg, wParam);
        HandleDockingMessage(msg);

        return IntPtr.Zero;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_settings is not null && IsLoaded && IsDocked)
        {
            // SizeToContent responds to title/ghost visibility; keep docked
            // widgets flush to their chosen taskbar edge.
            Dispatcher.BeginInvoke(
                new Action(PositionBesideTaskbar),
                DispatcherPriority.Loaded);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // The Print Screen hook was installed in OnSourceInitialized, using
        // the persisted startup privacy mode. Don't reset it here.
        if (_settings.Left is double left &&
            _settings.Top is double top &&
            IsPositionVisible(left, top))
        {
            Left = left;
            Top = top;
        }
        else
        {
            PositionBesideTaskbar();
            if (!IsDocked)
            {
                SaveCurrentPosition();
            }
        }

        if (IsDocked)
        {
            PositionBesideTaskbar();
        }

        ApplyWidgetAppearance();
        UpdateDisplay();
        StartTaskbarOverlayWatch();

        // Metadata-only check; async and bounded so screenshots remain usable.
        if (_settings.CheckForUpdatesOnLaunch)
        {
            _ = _updateChecker.CheckAsync();
        }

        // If the keyboard hook did not install during source initialisation,
        // retry once after the window has finished loading.
        // This avoids requiring the user to toggle Private Mode manually.
        if (_privacyMode && _printScreenHookHandle == IntPtr.Zero)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (_privacyMode && _printScreenHookHandle == IntPtr.Zero)
                    {
                        UpdateCaptureHotkeyRegistration();
                        UpdateDisplay();
                    }
                }),
                DispatcherPriority.ApplicationIdle);
        }
    }

    private void PrivacyButton_Click(object sender, RoutedEventArgs e)
    {
        _privacyMode = !_privacyMode;
        UpdateCaptureHotkeyRegistration();

        if (!_privacyMode)
        {
            DestroyCurrentItem();
        }

        UpdateDisplay();
    }

    private async void StatusButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_privacyMode || _captureInProgress)
        {
            return;
        }

        // The countdown also acts as a one-click "Clear now" action.
        if (_hasActiveItem)
        {
            DestroyCurrentItem();
            UpdateDisplay();
            return;
        }

        await CapturePrivateScreenshotAsync();
    }

    private async Task CapturePrivateScreenshotAsync()
    {
        _captureInProgress = true;

        try
        {
            // Starting a new capture immediately destroys any previous
            // GhostClip-owned clipboard item.
            if (_hasActiveItem)
            {
                DestroyCurrentItem();
            }

            UpdateDisplay();

            // Hide GhostClip before capturing the desktop so the widget
            // itself is not included in the source image.
            Hide();

            await Task.Delay(150);

            BitmapSource desktop =
                CaptureVirtualDesktop();

            SnippingOverlay overlay =
                new(
                    desktop,
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight);

            bool? result =
                overlay.ShowDialog();

            if (result != true ||
                overlay.SelectedImage is null)
            {
                return;
            }

            SetProtectedClipboardImage(
                overlay.SelectedImage);
        }
        catch (ExternalException)
        {
            FinishCurrentItem();
        }
        finally
        {
            _changingClipboard = false;

            Show();
            Topmost = true;

            _captureInProgress = false;
            UpdateDisplay();
        }
    }

    private void SetProtectedClipboardImage(
        BitmapSource image)
    {
        DataObject dataObject = new();

        dataObject.SetImage(image);

        dataObject.SetData(
            ExcludeFromClipboardHistoryFormat,
            new byte[] { 1 },
            false);

        // The following two documented DWORD flags explicitly opt out
        // of both Windows Clipboard History and cloud synchronisation.
        dataObject.SetData(
            CanIncludeInClipboardHistoryFormat,
            new byte[sizeof(uint)],
            false);

        dataObject.SetData(
            CanUploadToCloudClipboardFormat,
            new byte[sizeof(uint)],
            false);

        _changingClipboard = true;

        try
        {
            Clipboard.SetDataObject(
                dataObject,
                false);
        }
        finally
        {
            _changingClipboard = false;
        }

        _activeClipboardData = dataObject;
        _hasActiveItem = true;

        _secondsRemaining =
            _settings.ClipboardTimeoutSeconds;

        _activeItemExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(
            _settings.ClipboardTimeoutSeconds);

        _countdownTimer.Start();
    }

    private static BitmapSource CaptureVirtualDesktop()
    {
        int left =
            GetSystemMetrics(SmXVirtualScreen);

        int top =
            GetSystemMetrics(SmYVirtualScreen);

        int width =
            GetSystemMetrics(SmCxVirtualScreen);

        int height =
            GetSystemMetrics(SmCyVirtualScreen);

        if (width <= 0 || height <= 0)
        {
            throw new Win32Exception(
                "Windows reported an invalid virtual desktop size.");
        }

        IntPtr screenDc = IntPtr.Zero;
        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            screenDc = GetDC(IntPtr.Zero);

            if (screenDc == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            memoryDc = CreateCompatibleDC(screenDc);

            if (memoryDc == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            bitmap = CreateCompatibleBitmap(
                screenDc,
                width,
                height);

            if (bitmap == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            oldBitmap = SelectObject(
                memoryDc,
                bitmap);

            if (oldBitmap == IntPtr.Zero ||
                oldBitmap == new IntPtr(-1))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            bool copied = BitBlt(
                memoryDc,
                0,
                0,
                width,
                height,
                screenDc,
                left,
                top,
                SrcCopy | CaptureBlt);

            if (!copied)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            BitmapSource image =
                Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            image.Freeze();

            return image;
        }
        finally
        {
            if (memoryDc != IntPtr.Zero &&
                oldBitmap != IntPtr.Zero &&
                oldBitmap != new IntPtr(-1))
            {
                SelectObject(memoryDc, oldBitmap);
            }

            if (bitmap != IntPtr.Zero)
            {
                DeleteObject(bitmap);
            }

            if (memoryDc != IntPtr.Zero)
            {
                DeleteDC(memoryDc);
            }

            if (screenDc != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }
    }

    private void CountdownTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!_hasActiveItem)
        {
            _countdownTimer.Stop();
            return;
        }

        // Account for delayed UI ticks, system sleep and time spent in
        // modal windows. A missed clipboard update should also be noticed.
        CheckClipboardOwnership();
        if (!_hasActiveItem)
        {
            return;
        }

        double secondsLeft = _activeItemExpiresAtUtc is DateTimeOffset expiry
            ? (expiry - DateTimeOffset.UtcNow).TotalSeconds
            : 0;

        _secondsRemaining = Math.Max(0, (int)Math.Ceiling(secondsLeft));

        if (_secondsRemaining <= 0)
        {
            DestroyCurrentItem();
        }

        UpdateDisplay();
    }

    private void CheckClipboardOwnership()
    {
        if (_activeClipboardData is null)
        {
            return;
        }

        try
        {
            if (!Clipboard.IsCurrent(_activeClipboardData))
            {
                FinishCurrentItem();
                UpdateDisplay();
            }
        }
        catch (ExternalException)
        {
            // Another process may temporarily have the clipboard open.
            // Leave our state unchanged and try again later.
        }
    }

    private void DestroyCurrentItem()
    {
        if (_activeClipboardData is null)
        {
            FinishCurrentItem();
            return;
        }

        try
        {
            bool stillOurs =
                Clipboard.IsCurrent(_activeClipboardData);

            if (stillOurs)
            {
                _changingClipboard = true;

                try
                {
                    Clipboard.Clear();
                }
                finally
                {
                    _changingClipboard = false;
                }
            }

            FinishCurrentItem();
        }
        catch (ExternalException)
        {
            // Do not blindly clear the clipboard if ownership cannot
            // be verified. Keep trying on subsequent timer ticks.
            _secondsRemaining = 0;
        }
    }

    private void FinishCurrentItem()
    {
        _countdownTimer.Stop();

        _activeClipboardData = null;
        _hasActiveItem = false;
        _activeItemExpiresAtUtc = null;

        _secondsRemaining =
            _settings.ClipboardTimeoutSeconds;
    }

    private void ExitButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Closing uses Window_Closing to clear the current GhostClip
        // clipboard item only if we still own it.
        Close();
    }

    private void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Settings is modeless: GhostClip can still be dragged or used while
        // the panel is open. A second click focuses the same Settings window.
        if (_settingsWindow?.IsVisible == true)
        {
            _settingsWindow.Activate();
            return;
        }

        SettingsWindow settingsWindow =
            new(
                _settings.ClipboardTimeoutSeconds,
                _positionLocked,
                _settings.StartInPrivateMode,
                _settings.AlwaysOnTop,
                _settings.WidgetScalePercent,
                _settings.DockPosition ?? "Right",
                _settings.ShowWidgetBackground,
                _settings.ShowWidgetBorder,
                _settings.WidgetBackgroundOpacityPercent,
                _settings.WidgetBackgroundColour,
                _settings.WidgetBorderColour,
                _settings.WidgetCornerRadius,
                _settings.PrivateOnButtonColour,
                _settings.ReadyButtonColour,
                _settings.CountdownButtonColour,
                _settings.ButtonOpacityPercent,
                _settings.ButtonCornerRadius,
                _settings.ShowGhost,
                _settings.ShowTitle,
                _settings.GhostColour,
                _settings.TitleColour,
                _settings.PrivateOffButtonColour,
                _settings.StatusOffButtonColour,
                _settings.SettingsButtonColour,
                _settings.SettingsIconColour,
                _settings.ExitButtonColour,
                _settings.ExitIconColour,
                _settings.PrivateOnTextColour,
                _settings.PrivateOffTextColour,
                _settings.ReadyTextColour,
                _settings.CountdownTextColour,
                _settings.StatusOffTextColour,
                _settings.CheckForUpdatesOnLaunch,
                _updateChecker)
            {
                Owner = this
            };

        settingsWindow.PreferencesChanged += SettingsWindow_PreferencesChanged;
        settingsWindow.ResetPositionRequested += ResetWidgetToSafePosition;
        settingsWindow.Closed += (_, _) =>
        {
            settingsWindow.PreferencesChanged -= SettingsWindow_PreferencesChanged;
            settingsWindow.ResetPositionRequested -= ResetWidgetToSafePosition;
            if (ReferenceEquals(_settingsWindow, settingsWindow))
            {
                _settingsWindow = null;
            }
        };

        _settingsWindow = settingsWindow;
        settingsWindow.Show();
    }

    private void SettingsWindow_PreferencesChanged(SettingsWindow settingsWindow)
    {
        bool wasDocked = IsDocked;
        bool willDock = settingsWindow.DockPosition is "Left" or "Right";

        // When docking from a floating position, retain those coordinates.
        if (!wasDocked && willDock)
        {
            _settings.Left = Left;
            _settings.Top = Top;
        }

        _settings.DockPosition = settingsWindow.DockPosition;
        _settings.AlwaysOnTop = settingsWindow.AlwaysOnTop;
        ApplyAlwaysOnTopPreference();
        _settings.WidgetScalePercent = settingsWindow.WidgetScalePercent;
        ApplyWidgetScale();

        // Undocking retains the CURRENT docked position instead of jumping
        // to an old floating position. Dragging is immediately available.
        if (wasDocked && !willDock)
        {
            _settings.Left = Left;
            _settings.Top = Top;
        }

        _settings.ShowWidgetBackground = settingsWindow.ShowWidgetBackground;
        _settings.ShowWidgetBorder = settingsWindow.ShowWidgetBorder;
        _settings.WidgetBackgroundOpacityPercent =
            settingsWindow.WidgetBackgroundOpacityPercent;
        _settings.WidgetBackgroundColour = settingsWindow.WidgetBackgroundColour;
        _settings.WidgetBorderColour = settingsWindow.WidgetBorderColour;
        _settings.WidgetCornerRadius = settingsWindow.WidgetCornerRadius;
        _settings.PrivateOnButtonColour = settingsWindow.PrivateOnButtonColour;
        _settings.ReadyButtonColour = settingsWindow.ReadyButtonColour;
        _settings.CountdownButtonColour = settingsWindow.CountdownButtonColour;
        _settings.ButtonOpacityPercent = settingsWindow.ButtonOpacityPercent;
        _settings.ButtonCornerRadius = settingsWindow.ButtonCornerRadius;
        _settings.ShowGhost = settingsWindow.ShowGhost;
        _settings.ShowTitle = settingsWindow.ShowTitle;
        _settings.GhostColour = settingsWindow.GhostColour;
        _settings.TitleColour = settingsWindow.TitleColour;
        _settings.PrivateOffButtonColour = settingsWindow.PrivateOffButtonColour;
        _settings.StatusOffButtonColour = settingsWindow.StatusOffButtonColour;
        _settings.SettingsButtonColour = settingsWindow.SettingsButtonColour;
        _settings.SettingsIconColour = settingsWindow.SettingsIconColour;
        _settings.ExitButtonColour = settingsWindow.ExitButtonColour;
        _settings.ExitIconColour = settingsWindow.ExitIconColour;
        _settings.PrivateOnTextColour = settingsWindow.PrivateOnTextColour;
        _settings.PrivateOffTextColour = settingsWindow.PrivateOffTextColour;
        _settings.ReadyTextColour = settingsWindow.ReadyTextColour;
        _settings.CountdownTextColour = settingsWindow.CountdownTextColour;
        _settings.StatusOffTextColour = settingsWindow.StatusOffTextColour;
        _settings.CheckForUpdatesOnLaunch = settingsWindow.CheckForUpdatesOnLaunch;
        _settings.ClipboardTimeoutSeconds =
            settingsWindow.ClipboardTimeoutSeconds;
        _settings.PositionLocked = settingsWindow.PositionLocked;
        _settings.StartInPrivateMode = settingsWindow.StartInPrivateMode;
        _positionLocked = _settings.PositionLocked;

        if (_hasActiveItem)
        {
            // Shortening the timeout can shorten an active item's lifetime.
            // Increasing it never extends the current item.
            DateTimeOffset maximumExpiry = DateTimeOffset.UtcNow.AddSeconds(
                _settings.ClipboardTimeoutSeconds);

            if (_activeItemExpiresAtUtc is null ||
                _activeItemExpiresAtUtc > maximumExpiry)
            {
                _activeItemExpiresAtUtc = maximumExpiry;
            }

            _secondsRemaining = Math.Max(
                0,
                (int)Math.Ceiling(
                    (_activeItemExpiresAtUtc.Value - DateTimeOffset.UtcNow)
                    .TotalSeconds));
        }
        else
        {
            _secondsRemaining = _settings.ClipboardTimeoutSeconds;
        }

        if (willDock)
        {
            PositionBesideTaskbar();
        }

        ApplyWidgetAppearance();
        SaveSettings();
        UpdateDisplay();
        KeepWidgetAboveTaskbar();
    }

    private void DragArea_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_positionLocked || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        // Dragging an unlocked docked widget switches it to Free-floating.
        // Synchronise the still-open modeless Settings window immediately.
        if (IsDocked)
        {
            _settings.DockPosition = "Floating";
            _settings.Left = Left;
            _settings.Top = Top;
            _settingsWindow?.ReflectFreeFloatingAfterDrag();
            SaveSettings();
            UpdateDisplay();
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // A mouse release during DragMove needs no recovery.
        }
        finally
        {
            SaveCurrentPosition();
            KeepWidgetAboveTaskbar();
        }
    }

    private void UpdateDisplay()
    {
        PrivacyButton.Content = _privacyMode
            ? "PRIVATE ON"
            : "PRIVATE OFF";

        SetButtonAppearance(
            PrivacyButton,
            _privacyMode ? _settings.PrivateOnButtonColour : _settings.PrivateOffButtonColour,
            _privacyMode ? WidgetAppearanceOptions.PrivateOnColour : WidgetAppearanceOptions.PrivateOffColour,
            _privacyMode ? _settings.PrivateOnTextColour : _settings.PrivateOffTextColour);

        if (!_privacyMode)
        {
            StatusButton.Content = "OFF";
            SetButtonAppearance(
                StatusButton,
                _settings.StatusOffButtonColour,
                WidgetAppearanceOptions.StatusOffColour,
                _settings.StatusOffTextColour);
        }
        else if (_captureInProgress)
        {
            StatusButton.Content = "CAPTURE";
            SetButtonAppearance(
                StatusButton, _settings.CountdownButtonColour,
                WidgetAppearanceOptions.CountdownColour,
                _settings.CountdownTextColour);
        }
        else if (_hasActiveItem)
        {
            StatusButton.Content = _secondsRemaining > 0
                ? $"{_secondsRemaining}s"
                : "CLEAR...";

            SetButtonAppearance(
                StatusButton, _settings.CountdownButtonColour,
                WidgetAppearanceOptions.CountdownColour,
                _settings.CountdownTextColour);
        }
        else if (_printScreenRegistrationError is not null)
        {
            StatusButton.Content = "KEY BUSY";
            SetButtonAppearance(StatusButton, "#8E2D2D", "#8E2D2D");
        }
        else
        {
            StatusButton.Content = "READY";
            SetButtonAppearance(
                StatusButton, _settings.ReadyButtonColour,
                WidgetAppearanceOptions.ReadyColour,
                _settings.ReadyTextColour);
        }

        StatusButton.ToolTip = _hasActiveItem
            ? "Clear this temporary clipboard item now"
            : _printScreenRegistrationError is not null
                ? $"Print Screen is unavailable (Windows error {_printScreenRegistrationError}). Click here to capture."
                : "Capture a private snip (or press Print Screen while Private Mode is on)";

        DragArea.Cursor = _positionLocked ? Cursors.Arrow : Cursors.SizeAll;

        DragArea.ToolTip = _positionLocked
            ? "Unlock the widget in Settings to reposition it"
            : IsDocked
                ? "Drag to move and switch automatically to Free-floating"
                : "Drag to reposition GhostClip";

        // The border colour is now a user appearance preference. Lock
        // state remains visible through the drag cursor and tooltip.
    }



    private bool IsPositionVisible(
        double left,
        double top)
    {
        double right =
            left + ActualWidth;

        double bottom =
            top + ActualHeight;

        return right >
                   SystemParameters.VirtualScreenLeft &&
               left <
                   SystemParameters.VirtualScreenLeft +
                   SystemParameters.VirtualScreenWidth &&
               bottom >
                   SystemParameters.VirtualScreenTop &&
               top <
                   SystemParameters.VirtualScreenTop +
                   SystemParameters.VirtualScreenHeight;
    }

    private void SaveCurrentPosition()
    {
        // Docking is derived from the Windows work area. Preserve the last
        // manually chosen position for when docking is turned off again.
        if (IsDocked)
        {
            return;
        }

        _settings.Left = Left;
        _settings.Top = Top;

        SaveSettings();
    }

    private void Window_Closing(
        object? sender,
        CancelEventArgs e)
    {
        DestroyCurrentItem();
        SaveCurrentPosition();
    }


    private static AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings { DockPosition = "Right" };
            }

            string json =
                File.ReadAllText(SettingsPath);

            AppSettings? settings =
                JsonSerializer.Deserialize<AppSettings>(
                    json);

            if (settings is null)
            {
                return new AppSettings { DockPosition = "Right" };
            }

            settings.ClipboardTimeoutSeconds =
                Math.Clamp(
                    settings.ClipboardTimeoutSeconds,
                    5,
                    300);

            settings.WidgetBackgroundOpacityPercent = Math.Clamp(
                settings.WidgetBackgroundOpacityPercent, 20, 100);
            settings.ButtonOpacityPercent = Math.Clamp(
                settings.ButtonOpacityPercent, 60, 100);
            settings.WidgetCornerRadius = Math.Clamp(
                settings.WidgetCornerRadius, 0, 16);
            settings.WidgetScalePercent = Math.Clamp(
                settings.WidgetScalePercent,
                WidgetAppearanceOptions.MinimumScalePercent,
                WidgetAppearanceOptions.MaximumScalePercent);
            settings.ButtonCornerRadius = Math.Clamp(
                settings.ButtonCornerRadius, 0, 12);

            settings.WidgetBackgroundColour = WidgetAppearanceOptions.ValidatedColour(
                settings.WidgetBackgroundColour, WidgetAppearanceOptions.BackgroundColour);
            settings.WidgetBorderColour = WidgetAppearanceOptions.ValidatedColour(
                settings.WidgetBorderColour, WidgetAppearanceOptions.BorderColour);
            settings.PrivateOnButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.PrivateOnButtonColour, WidgetAppearanceOptions.PrivateOnColour);
            settings.ReadyButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.ReadyButtonColour, WidgetAppearanceOptions.ReadyColour);
            settings.CountdownButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.CountdownButtonColour, WidgetAppearanceOptions.CountdownColour);
            settings.GhostColour = WidgetAppearanceOptions.ValidatedColour(
                settings.GhostColour, WidgetAppearanceOptions.GhostColour);
            settings.TitleColour = WidgetAppearanceOptions.ValidatedColour(
                settings.TitleColour, WidgetAppearanceOptions.TitleColour);
            settings.PrivateOffButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.PrivateOffButtonColour, WidgetAppearanceOptions.PrivateOffColour);
            settings.StatusOffButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.StatusOffButtonColour, WidgetAppearanceOptions.StatusOffColour);
            settings.SettingsButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.SettingsButtonColour, WidgetAppearanceOptions.SettingsButtonColour);
            settings.SettingsIconColour = WidgetAppearanceOptions.ValidatedColour(
                settings.SettingsIconColour, WidgetAppearanceOptions.SettingsIconColour);
            settings.ExitButtonColour = WidgetAppearanceOptions.ValidatedColour(
                settings.ExitButtonColour, WidgetAppearanceOptions.ExitButtonColour);
            settings.ExitIconColour = WidgetAppearanceOptions.ValidatedColour(
                settings.ExitIconColour, WidgetAppearanceOptions.ExitIconColour);
            settings.PrivateOnTextColour = WidgetAppearanceOptions.ValidatedTextColour(
                settings.PrivateOnTextColour);
            settings.PrivateOffTextColour = WidgetAppearanceOptions.ValidatedTextColour(
                settings.PrivateOffTextColour);
            settings.ReadyTextColour = WidgetAppearanceOptions.ValidatedTextColour(
                settings.ReadyTextColour);
            settings.CountdownTextColour = WidgetAppearanceOptions.ValidatedTextColour(
                settings.CountdownTextColour);
            settings.StatusOffTextColour = WidgetAppearanceOptions.ValidatedTextColour(
                settings.StatusOffTextColour);

            settings.DockPosition = settings.DockPosition switch
            {
                "Left" or "Right" or "Floating" => settings.DockPosition,
                _ => settings.DockBesideTaskbar == false ? "Floating" : "Right"
            };
            settings.DockBesideTaskbar = null;

            return settings;
        }
        catch
        {
            return new AppSettings { DockPosition = "Right" };
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(
                SettingsDirectory);

            string json =
                JsonSerializer.Serialize(
                    _settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                SettingsPath,
                json);
        }
        catch
        {
            // Settings persistence must never prevent
            // GhostClip from running.
        }
    }

    private sealed class AppSettings
    {
        public double? Left { get; set; }

        public double? Top { get; set; }

        public bool PositionLocked { get; set; } = true;

        // Missing in older settings.json files: preserve historical topmost behaviour.
        public bool AlwaysOnTop { get; set; } = true;

        // Missing in existing settings.json: the original widget is 100%.
        public int WidgetScalePercent { get; set; } =
            WidgetAppearanceOptions.DefaultScalePercent;

        // A nullable legacy field allows migration from the previous
        // DockBesideTaskbar setting. Null fields are omitted from JSON.
        [System.Text.Json.Serialization.JsonIgnore(
            Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? DockBesideTaskbar { get; set; }

        // "Left", "Right" or "Floating"; null only before migration.
        public string? DockPosition { get; set; }

        public bool ShowWidgetBackground { get; set; } = true;

        public bool ShowWidgetBorder { get; set; } = true;

        public double WidgetBackgroundOpacityPercent { get; set; } = 90;
        public string WidgetBackgroundColour { get; set; } = WidgetAppearanceOptions.BackgroundColour;
        public string WidgetBorderColour { get; set; } = WidgetAppearanceOptions.BorderColour;
        public double WidgetCornerRadius { get; set; } = WidgetAppearanceOptions.WidgetCorners;
        public string PrivateOnButtonColour { get; set; } = WidgetAppearanceOptions.PrivateOnColour;
        public string ReadyButtonColour { get; set; } = WidgetAppearanceOptions.ReadyColour;
        public string CountdownButtonColour { get; set; } = WidgetAppearanceOptions.CountdownColour;
        public double ButtonOpacityPercent { get; set; } = WidgetAppearanceOptions.ButtonOpacity;
        public double ButtonCornerRadius { get; set; } = WidgetAppearanceOptions.ButtonCorners;
        public bool ShowGhost { get; set; } = WidgetAppearanceOptions.ShowGhost;
        public bool ShowTitle { get; set; } = WidgetAppearanceOptions.ShowTitle;
        public string GhostColour { get; set; } = WidgetAppearanceOptions.GhostColour;
        public string TitleColour { get; set; } = WidgetAppearanceOptions.TitleColour;
        public string PrivateOffButtonColour { get; set; } = WidgetAppearanceOptions.PrivateOffColour;
        public string StatusOffButtonColour { get; set; } = WidgetAppearanceOptions.StatusOffColour;
        public string SettingsButtonColour { get; set; } = WidgetAppearanceOptions.SettingsButtonColour;
        public string SettingsIconColour { get; set; } = WidgetAppearanceOptions.SettingsIconColour;
        public string ExitButtonColour { get; set; } = WidgetAppearanceOptions.ExitButtonColour;
        public string ExitIconColour { get; set; } = WidgetAppearanceOptions.ExitIconColour;
        public string PrivateOnTextColour { get; set; } = WidgetAppearanceOptions.AutomaticTextColour;
        public string PrivateOffTextColour { get; set; } = WidgetAppearanceOptions.AutomaticTextColour;
        public string ReadyTextColour { get; set; } = WidgetAppearanceOptions.AutomaticTextColour;
        public string CountdownTextColour { get; set; } = WidgetAppearanceOptions.AutomaticTextColour;
        public string StatusOffTextColour { get; set; } = WidgetAppearanceOptions.AutomaticTextColour;
        public bool CheckForUpdatesOnLaunch { get; set; } = true;

        public bool StartInPrivateMode { get; set; }

        public int ClipboardTimeoutSeconds { get; set; } = 30;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(
        IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(
        IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(
        IntPtr hWnd,
        IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(
        int nIndex);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(
        IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(
        IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(
        IntPtr hdc,
        int nWidth,
        int nHeight);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(
        IntPtr hdc,
        IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(
        IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(
        IntPtr hdcDest,
        int xDest,
        int yDest,
        int width,
        int height,
        IntPtr hdcSrc,
        int xSrc,
        int ySrc,
        uint rasterOperation);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
