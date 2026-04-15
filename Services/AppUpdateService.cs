using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using realsnag_media_downloader.Models;

namespace realsnag_media_downloader.Services;

public class AppUpdateService : IAppUpdateService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/realgarit/realsnag-media-downloader/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/realgarit/realsnag-media-downloader/releases/latest";

    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "realsnag-media-downloader" }
        },
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly ILogger<AppUpdateService> _logger;

    public AppUpdateService(ILogger<AppUpdateService> logger)
    {
        _logger = logger;
    }

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var json = await _httpClient.GetFromJsonAsync<JsonElement>(ReleasesApiUrl, cts.Token);
            var tagName = json.GetProperty("tag_name").GetString();

            if (string.IsNullOrWhiteSpace(tagName))
                return null;

            var latestVersion = tagName.TrimStart('v');
            var currentVersion = GetCurrentVersion();

            var isUpdateAvailable = IsNewer(latestVersion, currentVersion);

            _logger.LogDebug("Update check: current={Current}, latest={Latest}, updateAvailable={Available}",
                currentVersion, latestVersion, isUpdateAvailable);

            return new AppUpdateInfo(currentVersion, latestVersion, ReleasesPageUrl, isUpdateAvailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Failed to check for app updates (network error)");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("App update check timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse app release JSON");
            return null;
        }
    }

    private static string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    private static bool IsNewer(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
            return latestVer > currentVer;
        return false;
    }
}
