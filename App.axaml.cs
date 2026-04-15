using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using realsnag_media_downloader.Services;
using realsnag_media_downloader.Views;

namespace realsnag_media_downloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Configure DI container before anything else
        ServiceLocator.Configure();

        var settings = ServiceLocator.GetRequired<ISettingsService>();
        var localization = ServiceLocator.GetRequired<ILocalizationService>();

        settings.ApplyLanguage();
        settings.ApplyTheme(this);
        localization.CurrentLanguage = settings.Language;

        settings.ThemeChanged += (_, _) =>
        {
            RequestedThemeVariant = settings.IsDarkTheme
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        };

        settings.LanguageChanged += (_, e) =>
        {
            localization.CurrentLanguage = e.Language;
        };
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var toolManager = ServiceLocator.GetRequired<IToolManager>();
            var settings = ServiceLocator.GetRequired<ISettingsService>();

            if (!toolManager.IsYtDlpInstalled())
            {
                var setupWindow = new SetupWindow(toolManager, ServiceLocator.GetRequired<ILocalizationService>());
                desktop.MainWindow = setupWindow;
                setupWindow.Show();
                await setupWindow.RunSetupAsync();
            }

            if (settings.AutoUpdateYtDlp && toolManager.IsYtDlpInstalled())
            {
                _ = toolManager.UpdateYtDlpAsync();
            }

            desktop.MainWindow = new MainWindow();
            desktop.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
