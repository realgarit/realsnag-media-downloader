using System.Threading;
using System.Threading.Tasks;
using realsnag_media_downloader.Models;

namespace realsnag_media_downloader.Services;

public interface IAppUpdateService
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);
}
