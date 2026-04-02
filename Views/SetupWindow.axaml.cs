using Avalonia.Controls;
using realsnag_media_downloader.Services;
using System;
using System.Threading.Tasks;

namespace realsnag_media_downloader.Views;

public partial class SetupWindow : Window
{
    public bool SetupSucceeded { get; private set; }

    public SetupWindow()
    {
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

            await ToolManager.Instance.DownloadYtDlpAsync(progress);

            statusLabel.Text = LocalizationService.Instance.GetString("SetupComplete");
            progressBar.IsIndeterminate = false;
            progressBar.Value = 100;
            SetupSucceeded = true;

            await Task.Delay(800);
            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = LocalizationService.Instance.GetString("SetupFailed");
            errorLabel.Text = ex.Message;
            errorLabel.IsVisible = true;
            progressBar.IsIndeterminate = false;

            // Let user see the error, then close after 5 seconds
            await Task.Delay(5000);
            Close();
        }
    }
}
