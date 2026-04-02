using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using realsnag_media_downloader.Services;
using realsnag_media_downloader.ViewModels;
using realsnag_media_downloader.Views;
using System;

namespace realsnag_media_downloader;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainWindowViewModel();
        DataContext = _vm;

        var settingsButton = this.FindControl<Button>("SettingsButton");
        if (settingsButton != null)
            settingsButton.Click += OnSettingsClick;

        var browseButton = this.FindControl<Button>("BrowseButton");
        if (browseButton != null)
            browseButton.Click += OnBrowseClick;

        LocalizationService.Instance.LanguageChanged += (_, _) => UpdateTitle();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        Title = LocalizationService.Instance.GetString("AppTitle");
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
