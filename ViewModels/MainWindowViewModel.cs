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
using Microsoft.Extensions.Logging;
using realsnag_media_downloader.Models;
using realsnag_media_downloader.Services;

namespace realsnag_media_downloader.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IYtDlpService _ytDlp;
    private readonly IToolManager _toolManager;
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;
    private readonly IAppUpdateService _updateService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private static readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _metadataCts;
    private System.Timers.Timer? _debounceTimer;
    private DateTime _lastLogUpdate = DateTime.MinValue;
    private string _logBuffer = string.Empty;
    private const int MaxLogLength = 50_000;

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
    [ObservableProperty] private string _outputDirectory;
    [ObservableProperty] private bool _trimEnabled;
    [ObservableProperty] private string _trimStart = "00:00:00";
    [ObservableProperty] private string _trimEnd = "00:00:00";
    [ObservableProperty] private QualityOption? _selectedQuality;
    [ObservableProperty] private string _mediaDuration = string.Empty;
    [ObservableProperty] private string? _updateBannerText;

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

    public MainWindowViewModel(
        IYtDlpService ytDlp,
        IToolManager toolManager,
        ISettingsService settings,
        ILocalizationService localization,
        IAppUpdateService updateService,
        ILogger<MainWindowViewModel> logger)
    {
        _ytDlp = ytDlp;
        _toolManager = toolManager;
        _settings = settings;
        _localization = localization;
        _updateService = updateService;
        _logger = logger;
        _outputDirectory = settings.OutputDirectory;
        SelectedQuality = Qualities[0];

        _ = CheckForAppUpdateAsync();
    }

    private async Task CheckForAppUpdateAsync()
    {
        var info = await _updateService.CheckForUpdateAsync();
        if (info is { IsUpdateAvailable: true })
        {
            UpdateBannerText = string.Format(_localization.GetString("UpdateAvailable"), info.LatestVersion);
            _logger.LogInformation("App update available: {Version}", info.LatestVersion);
        }
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

        if (!InputValidator.IsValidUrl(Url))
        {
            MetadataText = _localization.GetString("InvalidUrl");
            return;
        }

        if (!_toolManager.IsYtDlpInstalled()) return;

        _metadataCts?.Cancel();
        _metadataCts = new CancellationTokenSource();

        IsFetchingMetadata = true;
        MetadataText = _localization.GetString("FetchingInfo");

        try
        {
            var info = await _ytDlp.FetchMetadataAsync(Url, _metadataCts.Token);

            MetadataText = $"{info.Title}\n{info.Duration}";
            MediaDuration = info.Duration;

            if (!string.IsNullOrWhiteSpace(info.ThumbnailUrl))
            {
                try
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(info.ThumbnailUrl, _metadataCts.Token);
                    using var ms = new MemoryStream(imageBytes);
                    ThumbnailSource = new Bitmap(ms);
                }
                catch (HttpRequestException)
                {
                    ThumbnailSource = null;
                }
            }

            Qualities.Clear();
            foreach (var q in info.Qualities)
                Qualities.Add(q);
            SelectedQuality = Qualities[0];
        }
        catch (OperationCanceledException) { }
        catch (TimeoutException ex)
        {
            MetadataText = ex.Message;
            _logger.LogWarning(ex, "Metadata fetch timed out");
        }
        catch (InvalidOperationException ex)
        {
            MetadataText = $"{_localization.GetString("ErrorFetchingMetadata")} {ex.Message}";
            _logger.LogError(ex, "Failed to fetch metadata");
        }
        finally
        {
            IsFetchingMetadata = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (!InputValidator.IsValidUrl(Url))
        {
            AppendLog(_localization.GetString("ErrorValidLink"));
            return;
        }

        if (!_toolManager.IsYtDlpInstalled())
        {
            AppendLog("yt-dlp is not installed. Please restart the app.");
            return;
        }

        if (TrimEnabled)
        {
            if (!InputValidator.IsValidTrimTime(TrimStart) || !InputValidator.IsValidTrimTime(TrimEnd))
            {
                AppendLog(_localization.GetString("InvalidTrimTime"));
                return;
            }
        }

        if (!InputValidator.IsDirectoryWritable(OutputDirectory))
        {
            AppendLog(_localization.GetString("OutputDirNotWritable"));
            return;
        }

        IsDownloading = true;
        IsProgressVisible = true;
        ProgressValue = 0;
        StatusText = _localization.GetString("Downloading");

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
            StatusText = _localization.GetString("Complete");
            _logger.LogInformation("Download completed: {Url}", Url);
        }
        catch (OperationCanceledException)
        {
            StatusText = _localization.GetString("DownloadCancelled");
        }
        catch (InvalidOperationException ex)
        {
            AppendLog($"{_localization.GetString("ErrorDuringDownload")} {ex.Message}");
            StatusText = _localization.GetString("Error");
            _logger.LogError(ex, "Download failed");
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
            AppendLog(_localization.GetString("DownloadCancelled"));
        }
        else
        {
            AppendLog(_localization.GetString("NoActiveDownload"));
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogText = string.Empty;
        _logBuffer = string.Empty;
    }

    public void SetOutputDirectory(string path)
    {
        OutputDirectory = path;
        _settings.OutputDirectory = path;
    }

    private void AppendLog(string message)
    {
        _logBuffer += message + "\n";

        var now = DateTime.UtcNow;
        if ((now - _lastLogUpdate).TotalMilliseconds < 200) return;
        _lastLogUpdate = now;

        var text = _logBuffer;
        if (text.Length > MaxLogLength)
            text = "... (earlier logs trimmed)\n" + text[^MaxLogLength..];

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LogText = text;
        });
    }
}
