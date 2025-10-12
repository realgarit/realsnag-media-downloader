using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace realsnag_media_downloader.Services;

public class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private bool _isDarkTheme = true;
    private string _language = "de";

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(value));
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
}

public class ThemeChangedEventArgs : EventArgs
{
    public bool IsDarkTheme { get; }

    public ThemeChangedEventArgs(bool isDarkTheme)
    {
        IsDarkTheme = isDarkTheme;
    }
}

public class LanguageChangedEventArgs : EventArgs
{
    public string Language { get; }

    public LanguageChangedEventArgs(string language)
    {
        Language = language;
    }
}
