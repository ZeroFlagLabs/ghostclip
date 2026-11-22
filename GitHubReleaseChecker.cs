using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GhostClip;

// Asynchronous, check-only GitHub Releases metadata. No screenshot or
// clipboard access, downloads or automatic installation; errors are bounded
// and never interrupt the capture workflow.
internal sealed class GitHubReleaseChecker
{
    private static readonly HttpClient HttpClient = new();

    public event EventHandler? StateChanged;
    public string Status { get; private set; } = "Not checked yet.";
    public Uri? AvailableReleaseUrl { get; private set; }
    public bool IsChecking { get; private set; }

    public async Task CheckAsync()
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;
        AvailableReleaseUrl = null;
        Status = "Checking GitHub Releases...";
        StateChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            using CancellationTokenSource timeout =
                new(TimeSpan.FromSeconds(7));
            using HttpRequestMessage request =
                new(HttpMethod.Get, GhostClipAppInfo.LatestReleaseApi);

            request.Headers.UserAgent.ParseAdd("ZeroFlagLabs-GhostClip");
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using HttpResponseMessage response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Status = "No published GhostClip GitHub release found yet.";
                return;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using JsonDocument json = await JsonDocument.ParseAsync(
                stream, cancellationToken: timeout.Token);

            JsonElement release = json.RootElement;
            if (release.ValueKind != JsonValueKind.Object ||
                IsTrue(release, "draft") || IsTrue(release, "prerelease"))
            {
                Status = "No newer stable release found.";
                return;
            }

            string tag = GetString(release, "tag_name");
            if (!TryParseVersion(tag, out Version? latest) ||
                !TryParseVersion(GhostClipAppInfo.Version, out Version? current))
            {
                Status = "GitHub release version could not be read.";
                return;
            }

            if (latest!.CompareTo(current) <= 0)
            {
                Status = "Up to date.";
                return;
            }

            string releaseUrl = GetString(release, "html_url");
            if (!Uri.TryCreate(releaseUrl, UriKind.Absolute, out Uri? url) ||
                url.Scheme != Uri.UriSchemeHttps ||
                !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                !url.AbsolutePath.StartsWith(
                    "/" + GhostClipAppInfo.Repository + "/releases/tag/",
                    StringComparison.OrdinalIgnoreCase))
            {
                Status = "A newer version was found, but the release link is invalid.";
                return;
            }

            AvailableReleaseUrl = url;
            Status = $"{tag} is available. Open GitHub to download it.";
        }
        catch (OperationCanceledException)
        {
            Status = "The update check timed out. Try again later.";
        }
        catch (HttpRequestException)
        {
            Status = "Unable to check updates. Check your connection.";
        }
        catch (JsonException)
        {
            Status = "Unable to read GitHub release information.";
        }
        catch (Exception)
        {
            // Checking for updates must not interrupt screenshot capture.
            Status = "Unable to check updates. Try again later.";
        }
        finally
        {
            IsChecking = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsTrue(JsonElement node, string name) =>
        node.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind == JsonValueKind.True;

    private static string GetString(JsonElement node, string name) =>
        node.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static bool TryParseVersion(string value, out Version? parsed)
    {
        parsed = null;
        string raw = value.StartsWith('v') ? value[1..] : value;
        string[] parts = raw.Split('.');
        return parts.Length == 3 &&
               Version.TryParse(raw, out parsed) &&
               parsed.Build >= 0;
    }
}
