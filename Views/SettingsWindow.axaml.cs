using Avalonia.Controls;
using realsnag_media_downloader.Services;
using realsnag_media_downloader.ViewModels;

namespace realsnag_media_downloader.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var vm = new SettingsWindowViewModel(
            ServiceLocator.GetRequired<ISettingsService>(),
            ServiceLocator.GetRequired<ILocalizationService>(),
            ServiceLocator.GetRequired<IToolManager>());
        DataContext = vm;

        Opened += async (_, _) => await vm.LoadToolInfoAsync();
    }
}
