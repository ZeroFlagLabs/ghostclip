using System;
using System.Diagnostics;
using System.Windows;

namespace GhostClip;

// A small, non-blocking view of published GitHub Releases. The app itself
// never downloads or executes release assets.
public partial class SettingsWindow
{
    private GitHubReleaseChecker? _releaseChecker;

    public bool CheckForUpdatesOnLaunch { get; private set; } = true;

    private void InitialiseAbout(
        bool checkForUpdatesOnLaunch,
        GitHubReleaseChecker checker)
    {
        CheckForUpdatesOnLaunch = checkForUpdatesOnLaunch;
        _releaseChecker = checker;

        AboutVersionText.Text =
            $"Version: v{GhostClipAppInfo.Version}";
        AboutDescriptionText.Text = GhostClipAppInfo.AboutDescription;
        CheckUpdatesOnLaunchCheckBox.IsChecked = checkForUpdatesOnLaunch;

        CheckUpdatesOnLaunchCheckBox.Checked += (_, _) => ApplyInputChanges();
        CheckUpdatesOnLaunchCheckBox.Unchecked += (_, _) => ApplyInputChanges();

        checker.StateChanged += ReleaseChecker_StateChanged;
        Closed += (_, _) => checker.StateChanged -= ReleaseChecker_StateChanged;
        RefreshReleaseStatus();
    }

    private void ReleaseChecker_StateChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            RefreshReleaseStatus();
        }
        else
        {
            Dispatcher.BeginInvoke(new Action(RefreshReleaseStatus));
        }
    }

    private void RefreshReleaseStatus()
    {
        if (_releaseChecker is null)
        {
            return;
        }

        UpdateStatusText.Text = _releaseChecker.Status;
        CheckNowButton.IsEnabled = !_releaseChecker.IsChecking;
        OpenReleaseButton.IsEnabled =
            _releaseChecker.AvailableReleaseUrl is not null;
    }

    private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_releaseChecker is not null)
        {
            await _releaseChecker.CheckAsync();
        }
    }

    private void OpenReleaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_releaseChecker?.AvailableReleaseUrl is Uri release)
        {
            OpenExternalUrl(release.AbsoluteUri);
        }
    }

    private void PublisherButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalUrl(GhostClipAppInfo.PublisherUrl);
    }

    private static void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // No browser configured, or Windows refused the open request.
            // Never interrupt GhostClip's screenshot functionality.
        }
    }
}
