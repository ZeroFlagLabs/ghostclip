using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GhostClip;

// GhostClip is a normal WPF window near the Windows taskbar. It does not
// modify Explorer or reserve screen space.
public partial class MainWindow
{
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const uint MonitorDefaultToNearest = 2;
    private uint _taskbarCreatedMessage;

    private bool IsDocked =>
        _settings.DockPosition is "Left" or "Right";

    private void InitialiseDockingNotifications()
    {
        _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
    }

    private void HandleDockingMessage(int message)
    {
        if (!IsDocked)
        {
            return;
        }

        if (message != WmDisplayChange &&
            message != WmSettingChange &&
            (uint)message != _taskbarCreatedMessage)
        {
            return;
        }

        // Give Windows time to publish its new work-area geometry.
        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (IsDocked && !_captureInProgress)
                {
                    PositionBesideTaskbar();
                }
            }),
            DispatcherPriority.Background);
    }

    private void ResetWidgetToSafePosition()
    {
        // Recovery path: the primary display's usable work area excludes
        // taskbar space. Always choose Free-floating, unlocked and visible.
        Rect area = SystemParameters.WorkArea;
        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : Height;

        _settings.DockPosition = "Floating";
        _settings.PositionLocked = false;
        _positionLocked = false;

        Left = area.Left + Math.Max(0, (area.Width - width) / 2);
        Top = Math.Max(area.Top, area.Bottom - height - 12);

        _settings.Left = Left;
        _settings.Top = Top;

        _settingsWindow?.ReflectFreeFloatingAfterDrag();
        SaveSettings();
        UpdateDisplay();
        KeepWidgetAboveTaskbar();
    }

    private void PositionBesideTaskbar()
    {
        Rect workArea;
        Rect monitorArea;

        if (!TryGetMonitorAreas(out monitorArea, out workArea))
        {
            workArea = SystemParameters.WorkArea;
            monitorArea = new Rect(
                0, 0, SystemParameters.PrimaryScreenWidth,
                SystemParameters.PrimaryScreenHeight);
        }

        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : Height;

        // Left and Right refer to the ends of the usable work area, next
        // to the taskbar. The window must not be covered by Explorer.
        double left = _settings.DockPosition == "Left"
            ? workArea.Left + 12
            : workArea.Right - width - 12;
        double top = workArea.Bottom - height - 8;

        double bottomInset = monitorArea.Bottom - workArea.Bottom;
        double topInset = workArea.Top - monitorArea.Top;
        double leftInset = workArea.Left - monitorArea.Left;
        double rightInset = monitorArea.Right - workArea.Right;

        // On installations with the taskbar at the top, align below it.
        if (topInset > 3 &&
            topInset >= Math.Max(bottomInset, Math.Max(leftInset, rightInset)))
        {
            top = workArea.Top + 8;
        }

        Left = Math.Clamp(
            left,
            workArea.Left,
            Math.Max(workArea.Left, workArea.Right - width));
        Top = Math.Clamp(
            top,
            workArea.Top,
            Math.Max(workArea.Top, workArea.Bottom - height));
    }

    private bool TryGetMonitorAreas(out Rect monitorArea, out Rect workArea)
    {
        monitorArea = default;
        workArea = default;

        if (_windowHandle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(
            _windowHandle, MonitorDefaultToNearest);

        MonitorInformation info = new()
        {
            Size = Marshal.SizeOf<MonitorInformation>()
        };

        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        PresentationSource? source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return false;
        }

        Matrix transform = source.CompositionTarget.TransformFromDevice;

        monitorArea = GetRectInDips(info.Monitor, transform);
        workArea = GetRectInDips(info.Work, transform);
        return true;
    }

    private static Rect GetRectInDips(RECT bounds, Matrix transform)
    {
        Point topLeft = transform.Transform(
            new Point(bounds.Left, bounds.Top));
        Point bottomRight = transform.Transform(
            new Point(bounds.Right, bounds.Bottom));

        return new Rect(topLeft, bottomRight);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInformation
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor, ref MonitorInformation info);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);
}
