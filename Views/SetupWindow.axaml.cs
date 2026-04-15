using Avalonia.Controls;
using realsnag_media_downloader.Services;
using System;
using System.Threading.Tasks;

namespace realsnag_media_downloader.Views;

public partial class SetupWindow : Window
{
    private readonly IToolManager _toolManager;
    private readonly ILocalizationService _localization;

    public bool SetupSucceeded { get; private set; }

    public SetupWindow(IToolManager toolManager, ILocalizationService localization)
    {
        _toolManager = toolManager;
        _localization = localization;
        InitializeComponent();
    }

    public async Task RunSetupAsync()
    {
        var statusLabel = this.FindControl<TextBlock>("StatusLabel")!;
        var errorLabel = this.FindControl<TextBlock>("ErrorLabel")!;
        var progressBar = this.FindControl<ProgressBar>("SetupProgress")!;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                statusLabel.Text = msg;
            });

            await _toolManager.DownloadYtDlpAsync(progress);

            statusLabel.Text = _localization.GetString("SetupComplete");
            progressBar.IsIndeterminate = false;
            progressBar.Value = 100;
            SetupSucceeded = true;

            await Task.Delay(800);
            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = _localization.GetString("SetupFailed");
            errorLabel.Text = ex.Message;
            errorLabel.IsVisible = true;
            progressBar.IsIndeterminate = false;

            await Task.Delay(5000);
            Close();
        }
    }
}
