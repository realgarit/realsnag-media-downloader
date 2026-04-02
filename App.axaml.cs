using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using realsnag_media_downloader.Services;
using realsnag_media_downloader.Views;

namespace realsnag_media_downloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        SettingsService.Instance.ApplyLanguage();
        SettingsService.Instance.ApplyTheme(this);
        SettingsService.Instance.ThemeChanged += (_, _) =>
        {
            RequestedThemeVariant = SettingsService.Instance.IsDarkTheme
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        };
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Check if yt-dlp needs to be set up
            if (!ToolManager.Instance.IsYtDlpInstalled())
            {
                var setupWindow = new SetupWindow();
                desktop.MainWindow = setupWindow;
                setupWindow.Show();
                await setupWindow.RunSetupAsync();
            }

            // Auto-update check in background (fire and forget)
            if (SettingsService.Instance.AutoUpdateYtDlp && ToolManager.Instance.IsYtDlpInstalled())
            {
                _ = ToolManager.Instance.UpdateYtDlpAsync();
            }

            desktop.MainWindow = new MainWindow();
            desktop.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
