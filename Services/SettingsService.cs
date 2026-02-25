using System;
using System.IO;
using System.Text.Json;
using System.Globalization;
using Avalonia;

namespace realsnag_media_downloader.Services;

public class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private readonly string _settingsPath;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private bool _isDarkTheme = true;
    private string _language = "de";
    private string _outputDirectory = "";
    private bool _isDarkMode = true;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "realsnag-media-downloader");
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        
        Load();
    }

    // Properties
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

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                Save();
            }
        }
    }

    public void ApplyTheme(Application application)
    {
        if (IsDarkTheme)
        {
            application.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        }
        else
        {
            application.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        }
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
                    _language = settings.Language ?? "de";
                    _outputDirectory = settings.OutputDirectory ?? "";
                    _isDarkMode = settings.IsDarkMode;
                }
            }
        }
        catch
        {
            // Use defaults on error
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
                IsDarkMode = _isDarkMode
            };
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private class SettingsData
    {
        public bool IsDarkTheme { get; set; } = true;
        public string? Language { get; set; }
        public string? OutputDirectory { get; set; }
        public bool IsDarkMode { get; set; } = true;
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
