using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using realsnag_media_downloader.Services;
using realsnag_media_downloader.ViewModels;
using realsnag_media_downloader.Views;

namespace realsnag_media_downloader;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private readonly ILocalizationService _localization;

    public MainWindow()
    {
        InitializeComponent();

        _localization = ServiceLocator.GetRequired<ILocalizationService>();
        _vm = new MainWindowViewModel(
            ServiceLocator.GetRequired<IYtDlpService>(),
            ServiceLocator.GetRequired<IToolManager>(),
            ServiceLocator.GetRequired<ISettingsService>(),
            _localization,
            ServiceLocator.GetRequired<IAppUpdateService>(),
            ServiceLocator.GetRequired<ILogger<MainWindowViewModel>>());
        DataContext = _vm;

        var settingsButton = this.FindControl<Button>("SettingsButton");
        if (settingsButton != null)
            settingsButton.Click += OnSettingsClick;

        var browseButton = this.FindControl<Button>("BrowseButton");
        if (browseButton != null)
            browseButton.Click += OnBrowseClick;

        _localization.LanguageChanged += (_, _) => UpdateTitle();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        Title = _localization.GetString("AppTitle");
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Download Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            _vm.SetOutputDirectory(path);
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        settingsWindow.ShowDialog(this);
    }
}
