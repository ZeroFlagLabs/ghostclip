using System.Reflection;

namespace GhostClip;

// The public release repository supplies metadata for the update checker.
// No clipboard content or screenshots are ever sent by the update check.
internal static class GhostClipAppInfo
{
    public const string Publisher = "ZeroFlagLabs";
    public static string Version =>
        typeof(GhostClipAppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "0.0.0";
    public const string Repository = "ZeroFlagLabs/ghostclip";
    public const string PublisherUrl = "https://github.com/ZeroFlagLabs";
    public const string LatestReleaseApi =
        "https://api.github.com/repos/" + Repository + "/releases/latest";

    public const string AboutDescription =
        "A lightweight private screenshot and temporary clipboard utility for Windows. " +
        "GhostClip never uploads clipboard contents when checking for updates.";
}
