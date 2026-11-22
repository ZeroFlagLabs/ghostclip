using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace GhostClip;

// Explorer's taskbar is itself topmost. When the user enables Always on top,
// an overlapping GhostClip window can be reordered behind the taskbar after
// Explorer interactions. Reassert topmost only if both overlap AND Explorer
// is actually ahead. When Always on top is OFF, the timer is stopped and
// no recovery occurs. Never activate GhostClip or inspect clipboard content.
public partial class MainWindow
{
    private const uint GwHwndPrev = 3;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;

    private static readonly IntPtr HwndTopmost = new(-1);
    private DispatcherTimer? _taskbarOverlayTimer;

    // The timer and the Win32 Z-order recovery are strictly opt-in:
    // disabling Always on top must not be undone by Explorer interactions.
    private void ApplyAlwaysOnTopPreference()
    {
        Topmost = _settings.AlwaysOnTop;

        if (_settings.AlwaysOnTop)
        {
            // Loaded starts this once at startup. The method is also safe
            // when toggled later while Settings remains open.
            if (IsLoaded)
            {
                StartTaskbarOverlayWatch();
            }
        }
        else
        {
            StopTaskbarOverlayWatch();
        }
    }

    private void StartTaskbarOverlayWatch()
    {
        if (!_settings.AlwaysOnTop)
        {
            StopTaskbarOverlayWatch();
            return;
        }

        _taskbarOverlayTimer ??= new DispatcherTimer(
            DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _taskbarOverlayTimer.Tick -= TaskbarOverlayTimer_Tick;
        _taskbarOverlayTimer.Tick += TaskbarOverlayTimer_Tick;
        _taskbarOverlayTimer.Start();
        KeepWidgetAboveTaskbar();
    }

    private void StopTaskbarOverlayWatch()
    {
        _taskbarOverlayTimer?.Stop();
    }

    private void TaskbarOverlayTimer_Tick(object? sender, EventArgs e)
    {
        KeepWidgetAboveTaskbar();
    }

    private void KeepWidgetAboveTaskbar()
    {
        if (!_settings.AlwaysOnTop || _windowHandle == IntPtr.Zero ||
            !IsVisible || _captureInProgress ||
            !GetWindowRect(_windowHandle, out RECT widgetBounds))
        {
            return;
        }

        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);

        if (taskbar == IntPtr.Zero ||
            !GetWindowRect(taskbar, out RECT taskbarBounds) ||
            !Overlaps(widgetBounds, taskbarBounds))
        {
            return;
        }

        // Inspect the Z-order without changing it if GhostClip is already
        // in front. The window can still be dragged over the taskbar.
        IntPtr above = GetWindow(_windowHandle, GwHwndPrev);

        for (int i = 0; above != IntPtr.Zero && i < 256; i++)
        {
            if (above == taskbar)
            {
                _ = SetWindowPos(
                    _windowHandle,
                    HwndTopmost,
                    0, 0, 0, 0,
                    SwpNoMove | SwpNoSize | SwpNoActivate | SwpNoOwnerZOrder);
                return;
            }

            above = GetWindow(above, GwHwndPrev);
        }
    }

    private static bool Overlaps(RECT first, RECT second)
    {
        return first.Left < second.Right &&
               first.Right > second.Left &&
               first.Top < second.Bottom &&
               first.Bottom > second.Top;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(
        string className, string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(
        IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(
        IntPtr hwnd, uint command);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);
}
