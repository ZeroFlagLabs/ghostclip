using System;
using System.Runtime.InteropServices;

namespace GhostClip;

// The clipboard remains ephemeral if the user locks Windows or the
// workstation suspends. All work stays in memory; no content is logged.
public partial class MainWindow
{
    private const int WmSessionChange = 0x02B1;
    private const int WmPowerBroadcast = 0x0218;
    private const int WmEndSession = 0x0016;

    private const int WtsConsoleDisconnect = 0x02;
    private const int WtsRemoteDisconnect = 0x04;
    private const int WtsSessionLogoff = 0x06;
    private const int WtsSessionLock = 0x07;

    private const int PbtSuspend = 0x04;
    private const int PbtResumeAutomatic = 0x12;

    private const int NotifyForThisSession = 0;
    private bool _sessionNotificationsRegistered;

    private void RegisterSessionNotifications()
    {
        _sessionNotificationsRegistered = WTSRegisterSessionNotification(
            _windowHandle, NotifyForThisSession);
    }

    private void UnregisterSessionNotifications()
    {
        if (_sessionNotificationsRegistered)
        {
            WTSUnRegisterSessionNotification(_windowHandle);
            _sessionNotificationsRegistered = false;
        }
    }

    private void HandleSessionMessage(int message, IntPtr eventCode)
    {
        if (!_hasActiveItem)
        {
            return;
        }

        int eventValue = eventCode.ToInt32();

        bool shouldClear = message switch
        {
            WmSessionChange =>
                eventValue is WtsConsoleDisconnect
                    or WtsRemoteDisconnect
                    or WtsSessionLogoff
                    or WtsSessionLock,

            WmPowerBroadcast =>
                eventValue is PbtSuspend or PbtResumeAutomatic,

            WmEndSession => eventCode != IntPtr.Zero,
            _ => false
        };

        if (shouldClear)
        {
            DestroyCurrentItem();
            UpdateDisplay();
        }
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSRegisterSessionNotification(
        IntPtr hWnd, int dwFlags);

    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSUnRegisterSessionNotification(
        IntPtr hWnd);
}
