using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace GhostClip;

public partial class SettingsWindow
{
    // The visible title strip is custom WPF WindowChrome with GhostClip's
    // vector icon. Suppress the underlying Windows caption control flags,
    // which WindowChrome can still set despite WindowStyle=None. Keep the
    // resizable frame and custom draggable caption functionality.
    private const int GwlStyle = -16;
    private const int WsSystemMenu = 0x00080000;
    private const int WsMinimiseBox = 0x00020000;
    private const int WsMaximiseBox = 0x00010000;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    private void RemoveTitleBarControlButtons()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int style = GetWindowLong(hwnd, GwlStyle);
        int withoutButtons =
            style & ~(WsSystemMenu | WsMinimiseBox | WsMaximiseBox);

        if (style == withoutButtons)
        {
            return;
        }

        // The window has already been created (SourceInitialized), so
        // change only its native caption buttons and refresh the frame.
        _ = SetWindowLong(hwnd, GwlStyle, withoutButtons);

        if (!SetWindowPos(
                hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoZOrder |
                SwpNoActivate | SwpFrameChanged))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(
        IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
