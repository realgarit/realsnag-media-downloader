using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace realsnag_media_downloader.Services;

public class ToolManager
{
    private static readonly Lazy<ToolManager> _instance = new(() => new ToolManager());
    public static ToolManager Instance => _instance.Value;

    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "realsnag-media-downloader" }
        }
    };
    private string? _cachedFfmpegPath;

    public string BinDirectory { get; }

    public ToolManager()
    {
        BinDirectory = Path.Combine(GetAppDataDir(), "bin");
        Directory.CreateDirectory(BinDirectory);
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
        // Windows
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "realsnag-media-downloader");
    }

    public string YtDlpPath
    {
        get
        {
            var name = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

            // Check bundled location next to app binary first (CI/CD distributed)
            var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", name);
            if (File.Exists(bundledPath)) return bundledPath;

            // Fall back to app data directory (auto-downloaded)
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
            await process.WaitForExitAsync();
            return version?.Trim() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public async Task<string?> GetLatestYtDlpVersionAsync()
    {
        try
        {
            var json = await _httpClient.GetFromJsonAsync<JsonElement>(
                "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest",
                new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            return json.GetProperty("tag_name").GetString();
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Checking latest yt-dlp version...");

        // Get the download URL for the current platform
        var assetName = GetYtDlpAssetName();
        var json = await _httpClient.GetFromJsonAsync<JsonElement>(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", ct);

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
            throw new Exception($"Could not find yt-dlp binary '{assetName}' in latest release.");

        progress?.Report($"Downloading {assetName}...");

        var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(YtDlpPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long downloaded = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            downloaded += bytesRead;
            if (totalBytes > 0)
                progress?.Report($"Downloading... {downloaded * 100 / totalBytes}%");
        }

        // Set executable permission on Unix
        if (!OperatingSystem.IsWindows())
        {
            var chmod = Process.Start("chmod", $"+x \"{YtDlpPath}\"");
            if (chmod != null) await chmod.WaitForExitAsync(ct);
        }

        progress?.Report("yt-dlp installed successfully.");
    }

    public async Task UpdateYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var currentVersion = await GetInstalledYtDlpVersionAsync();
        var latestVersion = await GetLatestYtDlpVersionAsync();

        if (latestVersion != null && currentVersion == latestVersion)
        {
            progress?.Report($"Already up to date ({currentVersion}).");
            return;
        }

        await DownloadYtDlpAsync(progress, ct);
    }

    public string GetFfmpegPath()
    {
        if (_cachedFfmpegPath != null) return _cachedFfmpegPath;

        // Try system PATH first
        if (TryFindExecutable("ffmpeg"))
        {
            _cachedFfmpegPath = "ffmpeg";
            return _cachedFfmpegPath;
        }

        // Try common locations
        string[] commonPaths;
        if (OperatingSystem.IsMacOS())
        {
            commonPaths =
            [
                "/opt/homebrew/bin/ffmpeg",
                "/usr/local/bin/ffmpeg"
            ];
        }
        else if (OperatingSystem.IsLinux())
        {
            commonPaths =
            [
                "/usr/bin/ffmpeg",
                "/usr/local/bin/ffmpeg"
            ];
        }
        else // Windows
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
                _cachedFfmpegPath = path;
                return _cachedFfmpegPath;
            }
        }

        // Fallback — hope it's in PATH
        _cachedFfmpegPath = "ffmpeg";
        return _cachedFfmpegPath;
    }

    public bool IsFfmpegAvailable()
    {
        var path = GetFfmpegPath();
        return TryFindExecutable(path);
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
            var psi = new ProcessStartInfo
            {
                FileName = name,
                Arguments = OperatingSystem.IsWindows() && name == "ffmpeg" ? "-version" : "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (name == "ffmpeg") psi.Arguments = "-version";

            using var process = Process.Start(psi);
            if (process == null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
