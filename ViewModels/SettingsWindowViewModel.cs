using System;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using realsnag_media_downloader.Services;

namespace realsnag_media_downloader.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;
    private readonly IToolManager _toolManager;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private bool _isEnglish;
    [ObservableProperty] private bool _autoUpdateYtDlp;
    [ObservableProperty] private string _ytDlpVersionText = "Version: checking...";
    [ObservableProperty] private string _ffmpegStatusText = "Status: checking...";
    [ObservableProperty] private bool _isUpdatingYtDlp;

    public SettingsWindowViewModel(
        ISettingsService settings,
        ILocalizationService localization,
        IToolManager toolManager)
    {
        _settings = settings;
        _localization = localization;
        _toolManager = toolManager;

        _isDarkTheme = settings.IsDarkTheme;
        _isEnglish = localization.CurrentLanguage == "en";
        _autoUpdateYtDlp = settings.AutoUpdateYtDlp;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _settings.IsDarkTheme = value;
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        }
    }

    partial void OnIsEnglishChanged(bool value)
    {
        _localization.CurrentLanguage = value ? "en" : "de";
    }

    partial void OnAutoUpdateYtDlpChanged(bool value)
    {
        _settings.AutoUpdateYtDlp = value;
    }

    public async Task LoadToolInfoAsync()
    {
        try
        {
            var version = await _toolManager.GetInstalledYtDlpVersionAsync();
            YtDlpVersionText = $"Version: {version}";
        }
        catch
        {
            YtDlpVersionText = "Version: unknown";
        }

        var ffmpegPath = _toolManager.GetFfmpegPath();
        var found = _toolManager.IsFfmpegAvailable();
        FfmpegStatusText = found ? $"Status: Found ({ffmpegPath})" : "Status: Not found";
    }

    [RelayCommand]
    private async Task UpdateYtDlpAsync()
    {
        IsUpdatingYtDlp = true;
        try
        {
            var progress = new Progress<string>(msg => YtDlpVersionText = msg);
            await _toolManager.UpdateYtDlpAsync(progress);
            var version = await _toolManager.GetInstalledYtDlpVersionAsync();
            YtDlpVersionText = $"Version: {version}";
        }
        catch (Exception ex)
        {
            YtDlpVersionText = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingYtDlp = false;
        }
    }
}
