using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace realsnag_media_downloader.Services;

public interface IToolManager
{
    string BinDirectory { get; }
    string YtDlpPath { get; }
    bool IsYtDlpInstalled();
    Task<string> GetInstalledYtDlpVersionAsync();
    Task<string?> GetLatestYtDlpVersionAsync();
    Task DownloadYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default);
    Task UpdateYtDlpAsync(IProgress<string>? progress = null, CancellationToken ct = default);
    string GetFfmpegPath();
    bool IsFfmpegAvailable();
    void ApplyEnvironment(ProcessStartInfo psi);
}
