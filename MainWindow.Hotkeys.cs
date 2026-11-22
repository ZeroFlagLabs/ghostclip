using System;
using System.Runtime.InteropServices;

namespace GhostClip;

// Only the Print Screen key is examined by this hook. No other key data is
// recorded, logged or retained. The hook exists only while Private Mode is ON.
public partial class MainWindow
{
    private const uint VkPrintScreen = 0x2C;

    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private const long MinimumPrintScreenIntervalMs = 350;

    private readonly LowLevelKeyboardProc _printScreenHookCallback;
    private IntPtr _printScreenHookHandle;
    private bool _printScreenKeyHeld;
    private long _lastPrintScreenTriggerTick = -1000;

    private int? _printScreenRegistrationError;

    private void UpdateCaptureHotkeyRegistration()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        ReleaseCaptureHotkeys();

        if (!_privacyMode)
        {
            return;
        }

        // The Windows Snipping Tool owns a default Print Screen hotkey.
        // RegisterHotKey can override it only while our window is foreground.
        // A low-level hook is needed for GhostClip's background-widget design.
        _printScreenHookHandle = SetWindowsHookEx(
            WhKeyboardLl,
            _printScreenHookCallback,
            GetModuleHandle(null),
            0);

        if (_printScreenHookHandle == IntPtr.Zero)
        {
            _printScreenRegistrationError = Marshal.GetLastWin32Error();
        }

        // There is deliberately no global fallback shortcut. Print Screen
        // is GhostClip's only keyboard capture command.
    }

    private void ReleaseCaptureHotkeys()
    {
        if (_printScreenHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_printScreenHookHandle);
            _printScreenHookHandle = IntPtr.Zero;
        }

        _printScreenKeyHeld = false;
        _printScreenRegistrationError = null;
    }

    private IntPtr PrintScreenHook(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0 || !_privacyMode)
        {
            return CallNextHookEx(_printScreenHookHandle, code, wParam, lParam);
        }

        // Read only the virtual key code. We never store or log other keys.
        KeyboardHookData key = Marshal.PtrToStructure<KeyboardHookData>(lParam);

        if (key.VirtualKeyCode != VkPrintScreen)
        {
            return CallNextHookEx(_printScreenHookHandle, code, wParam, lParam);
        }

        int message = wParam.ToInt32();
        bool keyDown = message is WmKeyDown or WmSysKeyDown;
        bool keyUp = message is WmKeyUp or WmSysKeyUp;

        if (!keyDown && !keyUp)
        {
            return CallNextHookEx(_printScreenHookHandle, code, wParam, lParam);
        }

        // Some keyboards emit only a key-up; others can miss key-up
        // entirely. Debounce repeat key-downs instead of requiring a
        // previous key-up before any future screenshot can be captured.
        bool shouldTrigger = keyDown || (keyUp && !_printScreenKeyHeld);

        if (keyDown)
        {
            _printScreenKeyHeld = true;
        }
        else
        {
            _printScreenKeyHeld = false;
        }

        long now = Environment.TickCount64;

        if (shouldTrigger && now - _lastPrintScreenTriggerTick >= MinimumPrintScreenIntervalMs)
        {
            _lastPrintScreenTriggerTick = now;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_privacyMode && !_captureInProgress)
                {
                    _ = CapturePrivateScreenshotAsync();
                }
            }));
        }

        // Suppress Windows' ordinary screenshot handling only in Private Mode.
        return new IntPtr(1);
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookData
    {
        public uint VirtualKeyCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int hookType,
        LowLevelKeyboardProc callback,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
