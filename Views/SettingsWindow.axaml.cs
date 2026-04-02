using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using realsnag_media_downloader.Services;
using System;

namespace realsnag_media_downloader.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        InitializeControls();
        LoadToolInfo();
    }

    private void InitializeControls()
    {
        var darkRadio = this.FindControl<RadioButton>("DarkRadio")!;
        var lightRadio = this.FindControl<RadioButton>("LightRadio")!;
        var englishRadio = this.FindControl<RadioButton>("EnglishRadio")!;
        var germanRadio = this.FindControl<RadioButton>("GermanRadio")!;
        var autoUpdate = this.FindControl<CheckBox>("AutoUpdateCheckBox")!;
        var updateBtn = this.FindControl<Button>("UpdateYtDlpButton")!;

        darkRadio.IsChecked = SettingsService.Instance.IsDarkTheme;
        lightRadio.IsChecked = !SettingsService.Instance.IsDarkTheme;
        englishRadio.IsChecked = LocalizationService.Instance.CurrentLanguage == "en";
        germanRadio.IsChecked = LocalizationService.Instance.CurrentLanguage == "de";
        autoUpdate.IsChecked = SettingsService.Instance.AutoUpdateYtDlp;

        darkRadio.IsCheckedChanged += (_, _) =>
        {
            if (darkRadio.IsChecked == true)
            {
                SettingsService.Instance.IsDarkTheme = true;
                Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            }
        };

        lightRadio.IsCheckedChanged += (_, _) =>
        {
            if (lightRadio.IsChecked == true)
            {
                SettingsService.Instance.IsDarkTheme = false;
                Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            }
        };

        englishRadio.IsCheckedChanged += (_, _) =>
        {
            if (englishRadio.IsChecked == true)
                LocalizationService.Instance.CurrentLanguage = "en";
        };

        germanRadio.IsCheckedChanged += (_, _) =>
        {
            if (germanRadio.IsChecked == true)
                LocalizationService.Instance.CurrentLanguage = "de";
        };

        autoUpdate.IsCheckedChanged += (_, _) =>
        {
            SettingsService.Instance.AutoUpdateYtDlp = autoUpdate.IsChecked ?? true;
        };

        updateBtn.Click += OnUpdateYtDlpClick;
    }

    private async void LoadToolInfo()
    {
        var versionLabel = this.FindControl<TextBlock>("YtDlpVersionLabel")!;
        var ffmpegLabel = this.FindControl<TextBlock>("FfmpegStatusLabel")!;

        try
        {
            var version = await ToolManager.Instance.GetInstalledYtDlpVersionAsync();
            versionLabel.Text = $"Version: {version}";
        }
        catch
        {
            versionLabel.Text = "Version: unknown";
        }

        var ffmpegPath = ToolManager.Instance.GetFfmpegPath();
        var found = ToolManager.Instance.IsFfmpegAvailable();
        ffmpegLabel.Text = found ? $"Status: Found ({ffmpegPath})" : "Status: Not found";
    }

    private async void OnUpdateYtDlpClick(object? sender, RoutedEventArgs e)
    {
        var btn = this.FindControl<Button>("UpdateYtDlpButton")!;
        var versionLabel = this.FindControl<TextBlock>("YtDlpVersionLabel")!;

        btn.IsEnabled = false;
        btn.Content = "Updating...";

        try
        {
            var progress = new Progress<string>(msg => versionLabel.Text = msg);
            await ToolManager.Instance.UpdateYtDlpAsync(progress);
            var version = await ToolManager.Instance.GetInstalledYtDlpVersionAsync();
            versionLabel.Text = $"Version: {version}";
        }
        catch (Exception ex)
        {
            versionLabel.Text = $"Update failed: {ex.Message}";
        }
        finally
        {
            btn.IsEnabled = true;
            btn.Content = "Update yt-dlp";
        }
    }
}
