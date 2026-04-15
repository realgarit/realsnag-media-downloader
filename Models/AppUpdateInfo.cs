namespace realsnag_media_downloader.Models;

public record AppUpdateInfo(string CurrentVersion, string LatestVersion, string ReleaseUrl, bool IsUpdateAvailable);
