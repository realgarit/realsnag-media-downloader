using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using realsnag_media_downloader;
using realsnag_media_downloader.Services;

namespace realsnag_media_downloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Initialize services
        SettingsService.Instance.ApplyLanguage();
        SettingsService.Instance.ApplyTheme(this);
        
        // Subscribe to theme changes
        SettingsService.Instance.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        ApplyTheme(this);
    }

    private void ApplyTheme(Application application)
    {
        if (SettingsService.Instance.IsDarkTheme)
        {
            application.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        }
        else
        {
            application.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and
        // FluentValidation
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}