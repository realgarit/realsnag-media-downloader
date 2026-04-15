using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace realsnag_media_downloader.Services;

public class ToolManager : IToolManager
{
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "realsnag-media-downloader" }
        },
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly ILogger<ToolManager> _logger;
    private string? _cachedFfmpegPath;

    public string BinDirectory { get; }

    public ToolManager(ILogger<ToolManager> logger)
    {
        _logger = logger;
        BinDirectory = Path.Combine(GetAppDataDir(), "bin");
        Directory.CreateDirectory(BinDirectory);
    }

    /// <summary>
    /// Returns a PATH string that includes common tool locations.
    /// macOS GUI apps don't inherit the shell PATH, so Homebrew etc. aren't found.
    /// </summary>
    public static string GetEnrichedPath()
    {
        var current = Environment.GetEnvironmentVariable("PATH") ?? "";
        var extras = new List<string>();

        if (OperatingSystem.IsMacOS())
        {
            extras.AddRange(["/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin"]);
        }
        else if (OperatingSystem.IsLinux())
        {
            extras.AddRange(["/usr/local/bin", "/usr/bin", "/bin"]);
        }

        foreach (var p in extras)
        {
            if (!current.Contains(p))
                current = p + Path.PathSeparator + current;
        }

        return current;
    }

    public void ApplyEnvironment(ProcessStartInfo psi)
    {
        psi.Environment["PATH"] = GetEnrichedPath();
    }

    private static string GetAppDataDir()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "realsnag-media-downloader");
        if (OperatingSystem.IsLinux())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "realsnag-media-downloader");
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "realsnag-media-downloader");
    }

    public string YtDlpPath
    {
        get
        {
            var name = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

            var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", name);
            if (File.Exists(bundledPath)) return bundledPath;

            return Path.Combine(BinDirectory, name);
        }
    }

    public bool IsYtDlpInstalled() => File.Exists(YtDlpPath);

    public async Task<string> GetInstalledYtDlpVersionAsync()
    {
        if (!IsYtDlpInstalled()) return "Not installed";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return "Unknown";
            var version = await process.StandardOutput.ReadLineAsync();
            if (!process.WaitForExit(5000))
            {
                _logger.LogWarning("yt-dlp version check timed out");
                return "Unknown";
            }
            return version?.Trim() ?? "Unknown";
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to check yt-dlp version");
            return "Unknown";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start yt-dlp for version check");
            return "Unknown";
        }
    }

    public async Task<string?> GetLatestYtDlpVersionAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var json = await _httpClient.GetFromJsonAsync<JsonElement>(
                "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", cts.Token);
            return json.GetProperty("tag_name").GetString();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to check latest yt-dlp version (network error)");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Latest yt-dlp version check timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse yt-dlp release JSON");
            return null;
        }
    }

    public async Task DownloadYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Checking latest yt-dlp version...");
        _logger.LogInformation("Downloading yt-dlp...");

        var assetName = GetYtDlpAssetName();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(2));

        var json = await _httpClient.GetFromJsonAsync<JsonElement>(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", cts.Token);

        string? downloadUrl = null;
        foreach (var asset in json.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() == assetName)
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (downloadUrl == null)
            throw new InvalidOperationException($"Could not find yt-dlp binary '{assetName}' in latest release.");

        progress?.Report($"Downloading {assetName}...");

        var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        await using var fileStream = new FileStream(YtDlpPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long downloaded = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cts.Token)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
            downloaded += bytesRead;
            if (totalBytes > 0)
                progress?.Report($"Downloading... {downloaded * 100 / totalBytes}%");
        }

        if (!OperatingSystem.IsWindows())
        {
            var chmod = Process.Start("chmod", $"+x \"{YtDlpPath}\"");
            if (chmod != null) await chmod.WaitForExitAsync(cts.Token);
        }

        _logger.LogInformation("yt-dlp installed successfully at {Path}", YtDlpPath);
        progress?.Report("yt-dlp installed successfully.");
    }

    public async Task UpdateYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var currentVersion = await GetInstalledYtDlpVersionAsync();
        var latestVersion = await GetLatestYtDlpVersionAsync();

        if (latestVersion != null && currentVersion == latestVersion)
        {
            _logger.LogDebug("yt-dlp already up to date ({Version})", currentVersion);
            progress?.Report($"Already up to date ({currentVersion}).");
            return;
        }

        await DownloadYtDlpAsync(progress, ct);
    }

    public string GetFfmpegPath()
    {
        if (_cachedFfmpegPath != null) return _cachedFfmpegPath;

        string[] commonPaths;
        if (OperatingSystem.IsMacOS())
        {
            commonPaths = ["/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg"];
        }
        else if (OperatingSystem.IsLinux())
        {
            commonPaths = ["/usr/bin/ffmpeg", "/usr/local/bin/ffmpeg"];
        }
        else
        {
            commonPaths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
                @"C:\ffmpeg\bin\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ffmpeg", "bin", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "ffmpeg", "current", "bin", "ffmpeg.exe"),
                @"C:\ProgramData\chocolatey\bin\ffmpeg.exe"
            ];
        }

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found ffmpeg at {Path}", path);
                _cachedFfmpegPath = path;
                return _cachedFfmpegPath;
            }
        }

        var resolved = ResolveExecutablePath("ffmpeg");
        if (resolved != null)
        {
            _logger.LogDebug("Resolved ffmpeg via PATH at {Path}", resolved);
            _cachedFfmpegPath = resolved;
            return _cachedFfmpegPath;
        }

        _logger.LogWarning("ffmpeg not found, falling back to bare command name");
        _cachedFfmpegPath = "ffmpeg";
        return _cachedFfmpegPath;
    }

    public bool IsFfmpegAvailable()
    {
        var path = GetFfmpegPath();
        return TryFindExecutable(path);
    }

    private static string? ResolveExecutablePath(string name)
    {
        try
        {
            var cmd = OperatingSystem.IsWindows() ? "where" : "which";
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = name,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;
            var output = process.StandardOutput.ReadLine()?.Trim();
            process.WaitForExit(3000);
            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && File.Exists(output))
                return output;
        }
        catch (IOException) { }
        catch (InvalidOperationException) { }
        return null;
    }

    private static string GetYtDlpAssetName()
    {
        if (OperatingSystem.IsWindows()) return "yt-dlp.exe";
        if (OperatingSystem.IsMacOS()) return "yt-dlp_macos";
        return "yt-dlp_linux";
    }

    private static bool TryFindExecutable(string name)
    {
        try
        {
            var isFfmpeg = name.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase);
            var psi = new ProcessStartInfo
            {
                FileName = name,
                Arguments = isFfmpeg ? "-version" : "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch (IOException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
}
