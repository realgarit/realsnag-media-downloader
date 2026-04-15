namespace realsnag_media_downloader.Models;

public class SettingsData
{
    public bool IsDarkTheme { get; set; } = true;
    public string? Language { get; set; }
    public string? OutputDirectory { get; set; }
    public bool AutoUpdateYtDlp { get; set; } = true;
}
