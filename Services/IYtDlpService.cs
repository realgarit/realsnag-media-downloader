using System;
using System.Threading;
using System.Threading.Tasks;
using realsnag_media_downloader.Models;

namespace realsnag_media_downloader.Services;

public interface IYtDlpService
{
    Task<MediaInfo> FetchMetadataAsync(string url, CancellationToken ct = default);
    Task RunDownloadAsync(DownloadOptions opts, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default);
    void Cancel();
}
