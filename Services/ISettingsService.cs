using System;

namespace realsnag_media_downloader.Services;

public interface ISettingsService
{
    bool IsDarkTheme { get; set; }
    string Language { get; set; }
    string OutputDirectory { get; set; }
    bool AutoUpdateYtDlp { get; set; }

    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    void ApplyTheme(Avalonia.Application application);
    void ApplyLanguage();
    void Save();
}
