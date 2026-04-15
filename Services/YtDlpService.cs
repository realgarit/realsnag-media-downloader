using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using realsnag_media_downloader.Models;

namespace realsnag_media_downloader.Services;

public partial class YtDlpService : IYtDlpService
{
    private readonly IToolManager _toolManager;
    private readonly ILogger<YtDlpService> _logger;
    private Process? _currentProcess;
    private CancellationTokenSource? _cts;

    [GeneratedRegex(@"(\d+(\.\d+)?)%")]
    private static partial Regex ProgressRegex();

    public YtDlpService(IToolManager toolManager, ILogger<YtDlpService> logger)
    {
        _toolManager = toolManager;
        _logger = logger;
    }

    public async Task<MediaInfo> FetchMetadataAsync(string url, CancellationToken ct = default)
    {
        var ytdlp = _toolManager.YtDlpPath;
        _logger.LogDebug("Fetching metadata for {Url} using {YtDlpPath}", url, ytdlp);

        var psi = new ProcessStartInfo
        {
            FileName = ytdlp,
            Arguments = $"--dump-json --no-download \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _toolManager.ApplyEnvironment(psi);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start yt-dlp process.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        string output;
        try
        {
            output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Metadata fetch timed out for {Url}", url);
            KillProcess(process);
            throw new TimeoutException("Metadata fetch timed out after 30 seconds.");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            _logger.LogWarning("No metadata received from yt-dlp for {Url}", url);
            throw new InvalidOperationException("No metadata received from yt-dlp.");
        }

        try
        {
            var json = JsonDocument.Parse(output);
            var root = json.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Unknown" : "Unknown";
            var duration = root.TryGetProperty("duration_string", out var d) ? d.GetString() ?? "Unknown" : "Unknown";
            var thumbnail = root.TryGetProperty("thumbnail", out var th) ? th.GetString() : null;

            var qualities = new List<QualityOption>
            {
                new("Best Available", "bestvideo+bestaudio/best")
            };

            if (root.TryGetProperty("formats", out var formats))
            {
                var seen = new HashSet<string>();
                var formatList = new List<(int Height, string Label, string FormatArg)>();

                foreach (var fmt in formats.EnumerateArray())
                {
                    if (!fmt.TryGetProperty("height", out var h) || h.ValueKind != JsonValueKind.Number)
                        continue;

                    var height = h.GetInt32();
                    if (height <= 0 || !seen.Add(height.ToString())) continue;

                    var label = height switch
                    {
                        >= 2160 => "4K (2160p)",
                        >= 1440 => "1440p",
                        >= 1080 => "1080p",
                        >= 720 => "720p",
                        >= 480 => "480p",
                        >= 360 => "360p",
                        _ => $"{height}p"
                    };

                    formatList.Add((height, label, $"bestvideo[height<={height}]+bestaudio/best[height<={height}]"));
                }

                foreach (var (_, label, formatArg) in formatList.OrderByDescending(f => f.Height))
                {
                    qualities.Add(new QualityOption(label, formatArg));
                }
            }

            qualities.Add(new QualityOption("Audio Only", "bestaudio"));

            _logger.LogInformation("Fetched metadata: {Title} ({Duration})", title, duration);
            return new MediaInfo(title, duration, thumbnail, qualities);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse yt-dlp JSON output");
            throw new InvalidOperationException("Failed to parse media metadata.", ex);
        }
    }

    public async Task RunDownloadAsync(
        DownloadOptions opts,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        var ytdlp = _toolManager.YtDlpPath;
        var ffmpeg = _toolManager.GetFfmpegPath();
        var outputTemplate = Path.Combine(opts.OutputDir, "%(title)s.%(ext)s");

        _logger.LogInformation("Starting download: {Url} -> {OutputDir} (format={Format})", opts.Url, opts.OutputDir, opts.Format);

        var args = new List<string>
        {
            $"--ffmpeg-location \"{ffmpeg}\"",
            "--newline",
            $"-o \"{outputTemplate}\""
        };

        if (opts.Format == "mp3")
        {
            args.Add(opts.QualityFormatArg != null
                ? $"-f {opts.QualityFormatArg}"
                : "-f bestaudio");
            args.Add("--extract-audio");
            args.Add("--audio-format mp3");
            args.Add("--audio-quality 0");
        }
        else
        {
            var formatArg = opts.QualityFormatArg ?? "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]";
            args.Add($"-f \"{formatArg}\"");
        }

        if (!string.IsNullOrWhiteSpace(opts.TrimStart) && !string.IsNullOrWhiteSpace(opts.TrimEnd))
        {
            args.Add($"--download-sections \"*{opts.TrimStart}-{opts.TrimEnd}\"");
            args.Add("--force-keyframes-at-cuts");
        }

        args.Add($"\"{opts.Url}\"");

        var arguments = string.Join(" ", args);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var downloadPsi = new ProcessStartInfo
        {
            FileName = ytdlp,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _toolManager.ApplyEnvironment(downloadPsi);
        _currentProcess = new Process { StartInfo = downloadPsi };

        try
        {
            _currentProcess.Start();

            var readOutput = ReadLinesAsync(_currentProcess.StandardOutput, progress, _cts.Token);
            var readError = ReadLinesAsync(_currentProcess.StandardError, progress, _cts.Token, isError: true);

            await Task.WhenAll(readOutput, readError);
            await _currentProcess.WaitForExitAsync(_cts.Token);

            if (_currentProcess.ExitCode != 0)
            {
                _logger.LogError("yt-dlp exited with code {ExitCode}", _currentProcess.ExitCode);
                throw new InvalidOperationException($"yt-dlp exited with code {_currentProcess.ExitCode}");
            }

            _logger.LogInformation("Download completed successfully");
        }
        finally
        {
            _currentProcess = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Cancel()
    {
        try
        {
            _cts?.Cancel();
            if (_currentProcess is { HasExited: false })
            {
                _logger.LogInformation("Cancelling download, killing yt-dlp process");
                _currentProcess.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct,
        bool isError = false)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            var prefix = isError ? $"ERROR: {line}" : line;
            var match = ProgressRegex().Match(line);
            var pct = match.Success && double.TryParse(match.Groups[1].Value, out var v) ? v : 0;

            progress?.Report(new DownloadProgress(pct, prefix));
        }
    }
}
