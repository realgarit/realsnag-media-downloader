using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using realsnag_media_downloader.Services;

namespace realsnag_media_downloader.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlp = new();
    private static readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _metadataCts;
    private System.Timers.Timer? _debounceTimer;

    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isFetchingMetadata;
    [ObservableProperty] private Bitmap? _thumbnailSource;
    [ObservableProperty] private string _metadataText = string.Empty;
    [ObservableProperty] private string _logText = string.Empty;
    [ObservableProperty] private bool _isMp4 = true;
    [ObservableProperty] private string _outputDirectory = SettingsService.Instance.OutputDirectory;
    [ObservableProperty] private bool _trimEnabled;
    [ObservableProperty] private string _trimStart = "00:00:00";
    [ObservableProperty] private string _trimEnd = "00:00:00";
    [ObservableProperty] private QualityOption? _selectedQuality;
    [ObservableProperty] private string _mediaDuration = string.Empty;

    public ObservableCollection<QualityOption> Qualities { get; } = new()
    {
        new QualityOption("Best Available", "bestvideo+bestaudio/best")
    };

    public string VersionString
    {
        get
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v2.1.0";
        }
    }

    public MainWindowViewModel()
    {
        SelectedQuality = Qualities[0];
    }

    partial void OnUrlChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        if (string.IsNullOrWhiteSpace(value)) return;

        _debounceTimer = new System.Timers.Timer(800);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => FetchMetadataCommand.ExecuteAsync(null));
        };
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FetchMetadataAsync()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        if (!ToolManager.Instance.IsYtDlpInstalled()) return;

        _metadataCts?.Cancel();
        _metadataCts = new CancellationTokenSource();

        IsFetchingMetadata = true;
        MetadataText = LocalizationService.Instance.GetString("FetchingInfo");

        try
        {
            var info = await _ytDlp.FetchMetadataAsync(Url, _metadataCts.Token);

            MetadataText = $"{info.Title}\n{info.Duration}";
            MediaDuration = info.Duration;

            // Load thumbnail
            if (!string.IsNullOrWhiteSpace(info.ThumbnailUrl))
            {
                try
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(info.ThumbnailUrl, _metadataCts.Token);
                    using var ms = new MemoryStream(imageBytes);
                    ThumbnailSource = new Bitmap(ms);
                }
                catch
                {
                    ThumbnailSource = null;
                }
            }

            // Update qualities
            Qualities.Clear();
            foreach (var q in info.Qualities)
                Qualities.Add(q);
            SelectedQuality = Qualities[0];
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            MetadataText = $"{LocalizationService.Instance.GetString("ErrorFetchingMetadata")} {ex.Message}";
        }
        finally
        {
            IsFetchingMetadata = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            AppendLog(LocalizationService.Instance.GetString("ErrorValidLink"));
            return;
        }

        if (!ToolManager.Instance.IsYtDlpInstalled())
        {
            AppendLog("yt-dlp is not installed. Please restart the app.");
            return;
        }

        IsDownloading = true;
        IsProgressVisible = true;
        ProgressValue = 0;
        StatusText = LocalizationService.Instance.GetString("Downloading");

        var format = IsMp4 ? "mp4" : "mp3";
        var qualityArg = SelectedQuality?.FormatArg;

        var opts = new DownloadOptions(
            Url,
            OutputDirectory,
            format,
            qualityArg,
            TrimEnabled ? TrimStart : null,
            TrimEnabled ? TrimEnd : null);

        var progress = new Progress<DownloadProgress>(p =>
        {
            if (p.Percentage > 0) ProgressValue = p.Percentage;
            AppendLog(p.Line);
        });

        try
        {
            await _ytDlp.RunDownloadAsync(opts, progress);
            StatusText = LocalizationService.Instance.GetString("Complete");
        }
        catch (OperationCanceledException)
        {
            StatusText = LocalizationService.Instance.GetString("DownloadCancelled");
        }
        catch (Exception ex)
        {
            AppendLog($"{LocalizationService.Instance.GetString("ErrorDuringDownload")} {ex.Message}");
            StatusText = LocalizationService.Instance.GetString("Error");
        }
        finally
        {
            IsDownloading = false;
            IsProgressVisible = false;
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        if (IsDownloading)
        {
            _ytDlp.Cancel();
            AppendLog(LocalizationService.Instance.GetString("DownloadCancelled"));
        }
        else
        {
            AppendLog(LocalizationService.Instance.GetString("NoActiveDownload"));
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogText = string.Empty;
    }

    public void SetOutputDirectory(string path)
    {
        OutputDirectory = path;
        SettingsService.Instance.OutputDirectory = path;
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LogText += message + "\n";
        });
    }
}
