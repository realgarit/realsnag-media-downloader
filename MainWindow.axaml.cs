using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using realsnag_media_downloader.Services;
using System.Reflection;

namespace realsnag_media_downloader;

public partial class MainWindow : Window
{
    private Process? _currentProcess;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        var downloadButton = this.FindControl<Button>("DownloadButton");
        var clearLogsButton = this.FindControl<Button>("ClearLogsButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        var settingsButton = this.FindControl<Button>("SettingsButton");

        if (downloadButton != null)
        {
            downloadButton.Click += OnDownloadButtonClick;
        }

        if (clearLogsButton != null)
        {
            clearLogsButton.Click += (sender, e) =>
            {
                var logsBox = this.FindControl<TextBox>("LogsTextBox");
                if (logsBox != null)
                {
                    logsBox.Text = string.Empty;
                }
            };
        }

        if (cancelButton != null)
        {
            cancelButton.Click += OnCancelButtonClick;
        }

        if (settingsButton != null)
        {
            settingsButton.Click += OnSettingsButtonClick;
        }

        // Subscribe to language changes
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        SettingsService.Instance.ThemeChanged += OnThemeChanged;
        
        // Initialize UI text with current language
        UpdateUIText();
        
        // Set version number
        SetVersionNumber();
    }

    private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
    {
        // Create a modern settings window that adapts to theme
        var settingsWindow = new Window
        {
            Title = "Einstellungen",
            Width = 350,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var mainPanel = new StackPanel 
        { 
            Margin = new Thickness(25), 
            Spacing = 20
        };

        // Theme section
        var themeGroup = new StackPanel { Spacing = 10 };
        var themeLabel = new TextBlock 
        { 
            Text = "Design", 
            FontWeight = FontWeight.Bold,
            FontSize = 16
        };
        
        var themePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15 };
        
        var darkRadio = new RadioButton 
        { 
            Content = "Dunkel", 
            IsChecked = SettingsService.Instance.IsDarkTheme
        };
        var lightRadio = new RadioButton 
        { 
            Content = "Hell", 
            IsChecked = !SettingsService.Instance.IsDarkTheme
        };
        
        darkRadio.IsCheckedChanged += (s, args) => 
        { 
            if (darkRadio.IsChecked == true) 
            {
                SettingsService.Instance.IsDarkTheme = true;
                // Apply theme immediately
                if (Application.Current != null)
                {
                    Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                }
            }
        };
        
        lightRadio.IsCheckedChanged += (s, args) => 
        { 
            if (lightRadio.IsChecked == true) 
            {
                SettingsService.Instance.IsDarkTheme = false;
                // Apply theme immediately
                if (Application.Current != null)
                {
                    Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                }
            }
        };
        
        themePanel.Children.Add(darkRadio);
        themePanel.Children.Add(lightRadio);
        themeGroup.Children.Add(themeLabel);
        themeGroup.Children.Add(themePanel);

        // Language section
        var langGroup = new StackPanel { Spacing = 10 };
        var langLabel = new TextBlock 
        { 
            Text = "Sprache", 
            FontWeight = FontWeight.Bold,
            FontSize = 16
        };
        
        var langPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15 };
        
        var germanRadio = new RadioButton 
        { 
            Content = "Deutsch", 
            IsChecked = LocalizationService.Instance.CurrentLanguage == "de"
        };
        var englishRadio = new RadioButton 
        { 
            Content = "English", 
            IsChecked = LocalizationService.Instance.CurrentLanguage == "en"
        };
        
        germanRadio.IsCheckedChanged += (s, args) => 
        { 
            if (germanRadio.IsChecked == true) 
            {
                LocalizationService.Instance.CurrentLanguage = "de";
                // Update settings dialog text immediately
                settingsWindow.Title = LocalizationService.Instance.GetString("Settings");
                themeLabel.Text = LocalizationService.Instance.GetString("Theme");
                langLabel.Text = LocalizationService.Instance.GetString("Language");
                darkRadio.Content = LocalizationService.Instance.GetString("DarkTheme");
                lightRadio.Content = LocalizationService.Instance.GetString("LightTheme");
                germanRadio.Content = LocalizationService.Instance.GetString("German");
                englishRadio.Content = LocalizationService.Instance.GetString("English");
            }
        };
        
        englishRadio.IsCheckedChanged += (s, args) => 
        { 
            if (englishRadio.IsChecked == true) 
            {
                LocalizationService.Instance.CurrentLanguage = "en";
                // Update settings dialog text immediately
                settingsWindow.Title = LocalizationService.Instance.GetString("Settings");
                themeLabel.Text = LocalizationService.Instance.GetString("Theme");
                langLabel.Text = LocalizationService.Instance.GetString("Language");
                darkRadio.Content = LocalizationService.Instance.GetString("DarkTheme");
                lightRadio.Content = LocalizationService.Instance.GetString("LightTheme");
                germanRadio.Content = LocalizationService.Instance.GetString("German");
                englishRadio.Content = LocalizationService.Instance.GetString("English");
            }
        };
        
        langPanel.Children.Add(germanRadio);
        langPanel.Children.Add(englishRadio);
        langGroup.Children.Add(langLabel);
        langGroup.Children.Add(langPanel);

        mainPanel.Children.Add(themeGroup);
        mainPanel.Children.Add(langGroup);

        settingsWindow.Content = mainPanel;
        settingsWindow.ShowDialog(this);
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Update all UI text when language changes
        UpdateUIText();
    }

    private void UpdateUIText()
    {
        var loc = LocalizationService.Instance;
        
        // Update window title
        this.Title = loc.GetString("AppTitle");
        
        // Update main UI elements
        var linkLabel = this.FindControl<TextBlock>("LinkLabel");
        if (linkLabel != null) linkLabel.Text = loc.GetString("EnterLink");
        
        var metadataText = this.FindControl<TextBlock>("MetadataTextBlock");
        if (metadataText != null) metadataText.Text = loc.GetString("MediaMetadata");
        
        var formatLabel = this.FindControl<TextBlock>("FormatLabel");
        if (formatLabel != null) formatLabel.Text = loc.GetString("SelectFormat");
        
        var statusTextLabel = this.FindControl<TextBlock>("StatusTextLabel");
        if (statusTextLabel != null) statusTextLabel.Text = loc.GetString("Status");
        
        var statusLabel = this.FindControl<TextBlock>("StatusLabel");
        if (statusLabel != null && statusLabel.Text == "Bereit") 
        {
            statusLabel.Text = loc.GetString("Idle");
        }
        
        var logsLabel = this.FindControl<TextBlock>("LogsLabel");
        if (logsLabel != null) logsLabel.Text = loc.GetString("Logs");
        
        // Update button text
        var downloadButtonText = this.FindControl<TextBlock>("DownloadButtonText");
        if (downloadButtonText != null) downloadButtonText.Text = loc.GetString("Download");
        
        var cancelButtonText = this.FindControl<TextBlock>("CancelButtonText");
        if (cancelButtonText != null) cancelButtonText.Text = loc.GetString("Cancel");
        
        var clearLogsButtonText = this.FindControl<TextBlock>("ClearLogsButtonText");
        if (clearLogsButtonText != null) clearLogsButtonText.Text = loc.GetString("ClearLogs");
        
        var settingsButtonText = this.FindControl<TextBlock>("SettingsButtonText");
        if (settingsButtonText != null) settingsButtonText.Text = loc.GetString("Settings");
    }

    private void SetVersionNumber()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                var versionString = $"v{version.Major}.{version.Minor}.{version.Build}";
                var versionLabel = this.FindControl<TextBlock>("VersionLabel");
                if (versionLabel != null)
                {
                    versionLabel.Text = versionString;
                }
            }
        }
        catch
        {
            // Fallback to hardcoded version if assembly version fails
            var versionLabel = this.FindControl<TextBlock>("VersionLabel");
            if (versionLabel != null)
            {
                versionLabel.Text = "v1.1.0";
            }
        }
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        // Theme changes are handled automatically by Avalonia
    }

    private async void OnLinkTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        var linkTextBox = this.FindControl<TextBox>("LinkTextBox");
        var thumbnailImage = this.FindControl<Image>("ThumbnailImage");
        var metadataTextBlock = this.FindControl<TextBlock>("MetadataTextBlock");

        if (linkTextBox == null || thumbnailImage == null || metadataTextBlock == null) return;

        var link = linkTextBox.Text;

        if (string.IsNullOrWhiteSpace(link)) return;

        try
        {
            var thumbnailUrl = await FetchMetadata(link, "--get-thumbnail") ?? throw new Exception("Thumbnail URL is null");
            var title = await FetchMetadata(link, "--get-title") ?? "Unknown Title";
            var duration = await FetchMetadata(link, "--get-duration") ?? "Unknown Duration";

            // Load thumbnail image
            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(thumbnailUrl);
                using (var memoryStream = new MemoryStream(imageBytes))
                {
                    var bitmap = new Bitmap(memoryStream);
                    thumbnailImage.Source = bitmap;
                }
            }

            // Display metadata
            metadataTextBlock.Text = $"Title: {title}\nDuration: {duration}";
        }
        catch (Exception ex)
        {
            metadataTextBlock.Text = $"{LocalizationService.Instance.GetString("ErrorFetchingMetadata")} {ex.Message}";
        }
    }

    private async Task<string> FetchMetadata(string url, string mediaDownloaderOption)
    {
        var toolName = GetMediaDownloaderToolName();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = toolName,
            Arguments = $"{mediaDownloaderOption} \"{url}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processStartInfo))
        {
            if (process == null)
                throw new Exception($"Failed to start {toolName} process.");

            string? output = await process.StandardOutput.ReadLineAsync();
            await process.WaitForExitAsync();

            return output ?? throw new Exception($"No output received from {toolName}.");
        }
    }

    private async void OnDownloadButtonClick(object? sender, RoutedEventArgs e)
    {
        var linkTextBox = this.FindControl<TextBox>("LinkTextBox");
        var logsBox = this.FindControl<TextBox>("LogsTextBox");
        var mp4RadioButton = this.FindControl<RadioButton>("Mp4RadioButton");
        var downloadButton = this.FindControl<Button>("DownloadButton");
        var statusLabel = this.FindControl<TextBlock>("StatusLabel");
        var progressBar = this.FindControl<ProgressBar>("ProgressBar");

        if (linkTextBox == null || logsBox == null || mp4RadioButton == null || downloadButton == null || statusLabel == null || progressBar == null)
        {
            Console.WriteLine("Error: One or more controls are missing.");
            return;
        }

        var link = linkTextBox.Text;
        if (string.IsNullOrWhiteSpace(link))
        {
            AppendLog(logsBox, LocalizationService.Instance.GetString("ErrorValidLink"));
            return;
        }

        var isMp4 = mp4RadioButton.IsChecked ?? false;
        var format = isMp4 ? "mp4" : "mp3";
        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
        var outputPath = isMp4 ? $"{downloadsPath}\\%(title)s.%(ext)s" : $"{downloadsPath}\\%(title)s.%(ext)s";

        try
        {
            downloadButton.IsEnabled = false;
            statusLabel.Text = LocalizationService.Instance.GetString("Downloading");
            progressBar.IsVisible = true;
            progressBar.Value = 0;

            await RunMediaDownloader(link, format, outputPath, logsBox, progressBar);

            statusLabel.Text = LocalizationService.Instance.GetString("Complete");
        }
        catch (Exception ex)
        {
            AppendLog(logsBox, $"Error: {ex.Message}");
            statusLabel.Text = LocalizationService.Instance.GetString("Error");
        }
        finally
        {
            downloadButton.IsEnabled = true;
            progressBar.IsVisible = false;
        }
    }

    private async Task RunMediaDownloader(string url, string format, string outputPath, TextBox logsBox, ProgressBar progressBar)
    {
        var toolName = GetMediaDownloaderToolName();
        var ffmpegPath = GetFFmpegPath();

        string mediaDownloaderArguments = format == "mp4"
            ? $"--ffmpeg-location \"{ffmpegPath}\" -f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" \"{url}\" -o \"{outputPath}\" --newline"
            : $"--ffmpeg-location \"{ffmpegPath}\" -f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 \"{url}\" -o \"{outputPath}\" --newline";

        AppendLog(logsBox, $"Executing command: {toolName} {mediaDownloaderArguments}");

        _currentProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = toolName,
                Arguments = mediaDownloaderArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            _currentProcess.Start();

            string? outputLine;
            while ((outputLine = await _currentProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                await Dispatcher.UIThread.InvokeAsync(() => AppendLog(logsBox, outputLine));

                if (outputLine.Contains("%"))
                {
                    var progress = ParseProgressPercentage(outputLine);
                    if (progress.HasValue)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => progressBar.Value = progress.Value);
                    }
                }
            }

            string? errorLine;
            while ((errorLine = await _currentProcess.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => AppendLog(logsBox, "ERROR: " + errorLine));
            }

            await _currentProcess.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            AppendLog(logsBox, LocalizationService.Instance.GetString("DownloadCancelledError"));
        }
        catch (Exception ex)
        {
            AppendLog(logsBox, $"{LocalizationService.Instance.GetString("ErrorDuringDownload")} {ex.Message}");
        }
        finally
        {
            _currentProcess = null;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        var logsBox = this.FindControl<TextBox>("LogsTextBox");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (cancelButton != null)
        {
            cancelButton.IsEnabled = false; // Disable to debounce clicks
        }

        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                _currentProcess.Kill(true); // Try to kill the process forcefully

                if (logsBox != null)
                {
                    AppendLog(logsBox, LocalizationService.Instance.GetString("DownloadCancelled"));
                }
                else
                {
                    Console.WriteLine("LogsTextBox is null. Unable to log cancellation.");
                }
            }
            catch (Exception ex)
            {
                if (logsBox != null)
                {
                    AppendLog(logsBox, $"Error cancelling download: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"Error cancelling download: {ex.Message}");
                }
            }
            finally
            {
                _currentProcess = null; // Reset process

                if (cancelButton != null)
                {
                    cancelButton.IsEnabled = true; // Re-enable for future use
                }
            }
        }
        else
        {
            if (logsBox != null)
            {
                AppendLog(logsBox, LocalizationService.Instance.GetString("NoActiveDownload"));
            }
            else
            {
                Console.WriteLine("LogsTextBox is null. Unable to log no active download.");
            }

            if (cancelButton != null)
            {
                cancelButton.IsEnabled = true; // Ensure button is re-enabled
            }
        }
    }

    private double? ParseProgressPercentage(string logLine)
    {
        var match = System.Text.RegularExpressions.Regex.Match(logLine, @"(\d+(\.\d+)?)%");
        if (match.Success && double.TryParse(match.Groups[1].Value, out var progress))
        {
            return progress;
        }
        return null;
    }

    private void AppendLog(TextBox logsBox, string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            logsBox.Text += message + "\n";
            logsBox.CaretIndex = logsBox.Text.Length;
        });
    }

    private string GetMediaDownloaderToolName()
    {
        // Get the appropriate executable extension based on runtime
        var ext = GetExecutableExtension();

        // First, try to use the bundled yt-dlp
        var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "yt-dlp" + ext);
        if (File.Exists(bundledPath))
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = bundledPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(2000); // Wait max 2 seconds
                        if (process.ExitCode == 0)
                        {
                            return bundledPath;
                        }
                    }
                }
            }
            catch
            {
                // Continue to PATH search
            }
        }

        // Try to find the media downloader tool in PATH
        var possibleNames = new[] { "yt-dlp", "youtube-dl", "media-downloader" };
        
        foreach (var name in possibleNames)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = name,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(2000); // Wait max 2 seconds
                        if (process.ExitCode == 0)
                        {
                            return name;
                        }
                    }
                }
            }
            catch
            {
                // Continue to next tool name
            }
        }

        // Default fallback to bundled tool
        return bundledPath;
    }

    private string GetFFmpegPath()
    {
        // Get the appropriate executable extension based on runtime
        var ext = GetExecutableExtension();

        // First, try to use the bundled ffmpeg
        var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg", "ffmpeg" + ext);
        if (File.Exists(bundledPath))
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = bundledPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(2000);
                        if (process.ExitCode == 0)
                        {
                            return bundledPath;
                        }
                    }
                }
            }
            catch
            {
                // Continue to other methods
            }
        }

        // Try to find ffmpeg using Python imageio_ffmpeg
        try
        {
            var pythonStartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(pythonStartInfo))
            {
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        if (!string.IsNullOrEmpty(output) && File.Exists(output))
                        {
                            return output;
                        }
                    }
                }
            }
        }
        catch
        {
            // Continue to other methods
        }

        // Try to find ffmpeg in PATH
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process != null)
                {
                    process.WaitForExit(2000);
                    if (process.ExitCode == 0)
                    {
                        return "ffmpeg"; // Found in PATH
                    }
                }
            }
        }
        catch
        {
            // Continue to hardcoded paths
        }

        // Try common installation paths
        var possiblePaths = new[]
        {
            // Python imageio_ffmpeg paths
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Programs", "Python", "Python313", "Lib", "site-packages", "imageio_ffmpeg", "binaries", "ffmpeg-win-x86_64-v7.1.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Programs", "Python", "Python312", "Lib", "site-packages", "imageio_ffmpeg", "binaries", "ffmpeg-win-x86_64-v7.1.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Programs", "Python", "Python311", "Lib", "site-packages", "imageio_ffmpeg", "binaries", "ffmpeg-win-x86_64-v7.1.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Programs", "Python", "Python310", "Lib", "site-packages", "imageio_ffmpeg", "binaries", "ffmpeg-win-x86_64-v7.1.exe"),
            
            // Common ffmpeg installation paths
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            
            // Chocolatey installation
            @"C:\ProgramData\chocolatey\bin\ffmpeg.exe",
            
            // Scoop installation
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "ffmpeg", "current", "bin", "ffmpeg.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // If nothing found, return bundled path as fallback
        return bundledPath;
    }

    private string GetExecutableExtension()
    {
        // Return appropriate extension based on the current runtime
        if (OperatingSystem.IsWindows())
            return ".exe";
        else if (OperatingSystem.IsMacOS())
            return ""; // macOS executables don't need extension
        else if (OperatingSystem.IsLinux())
            return ""; // Linux executables don't need extension

        return ".exe"; // Default to Windows
    }
}