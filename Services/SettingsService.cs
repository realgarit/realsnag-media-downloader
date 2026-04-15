using System;
using System.IO;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Microsoft.Extensions.Logging;
using realsnag_media_downloader.Models;

namespace realsnag_media_downloader.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private bool _isDarkTheme = true;
    private string _language = "en";
    private string _outputDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    private bool _autoUpdateYtDlp = true;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "realsnag-media-downloader");

        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        Load();
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(value));
                Save();
            }
        }
    }

    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(value));
                Save();
            }
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (_outputDirectory != value)
            {
                _outputDirectory = value;
                Save();
            }
        }
    }

    public bool AutoUpdateYtDlp
    {
        get => _autoUpdateYtDlp;
        set
        {
            if (_autoUpdateYtDlp != value)
            {
                _autoUpdateYtDlp = value;
                Save();
            }
        }
    }

    public void ApplyTheme(Application application)
    {
        application.RequestedThemeVariant = IsDarkTheme
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;
    }

    public void ApplyLanguage()
    {
        var culture = new CultureInfo(Language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    _isDarkTheme = settings.IsDarkTheme;
                    _language = settings.Language ?? "en";
                    if (!string.IsNullOrWhiteSpace(settings.OutputDirectory))
                        _outputDirectory = settings.OutputDirectory;
                    _autoUpdateYtDlp = settings.AutoUpdateYtDlp;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse settings file, using defaults");
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read settings file, using defaults");
        }
    }

    public void Save()
    {
        try
        {
            var settings = new SettingsData
            {
                IsDarkTheme = _isDarkTheme,
                Language = _language,
                OutputDirectory = _outputDirectory,
                AutoUpdateYtDlp = _autoUpdateYtDlp
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }
}

public class ThemeChangedEventArgs : EventArgs
{
    public bool IsDarkTheme { get; }
    public ThemeChangedEventArgs(bool isDarkTheme) => IsDarkTheme = isDarkTheme;
}

public class LanguageChangedEventArgs : EventArgs
{
    public string Language { get; }
    public LanguageChangedEventArgs(string language) => Language = language;
}
