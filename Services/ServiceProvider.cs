using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace realsnag_media_downloader.Services;

/// <summary>
/// Central DI container for the application.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider
        ?? throw new InvalidOperationException("ServiceLocator has not been configured. Call Configure() first.");

    public static void Configure()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new FileLoggerProvider());
        });

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IToolManager, ToolManager>();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddTransient<IYtDlpService, YtDlpService>();

        _provider = services.BuildServiceProvider();
    }

    public static T GetRequired<T>() where T : notnull => Provider.GetRequiredService<T>();
}

/// <summary>
/// Simple file logger that writes structured log entries to a rotating log file.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;

    public FileLoggerProvider()
    {
        if (OperatingSystem.IsMacOS())
            _logDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Logs", "realsnag-media-downloader");
        else if (OperatingSystem.IsLinux())
            _logDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "realsnag-media-downloader", "logs");
        else
            _logDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "realsnag-media-downloader", "logs");

        System.IO.Directory.CreateDirectory(_logDirectory);
        _logFilePath = System.IO.Path.Combine(_logDirectory, "app.log");

        // Rotate log if > 5MB
        try
        {
            if (System.IO.File.Exists(_logFilePath))
            {
                var info = new System.IO.FileInfo(_logFilePath);
                if (info.Length > 5 * 1024 * 1024)
                {
                    var backup = System.IO.Path.Combine(_logDirectory, "app.log.1");
                    System.IO.File.Copy(_logFilePath, backup, overwrite: true);
                    System.IO.File.WriteAllText(_logFilePath, "");
                }
            }
        }
        catch (System.IO.IOException) { }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _logFilePath);
    public void Dispose() { }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _category;
    private readonly string _logFilePath;
    private static readonly object _lock = new();

    public FileLogger(string category, string logFilePath)
    {
        _category = category;
        _logFilePath = logFilePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var shortCategory = _category.Contains('.')
            ? _category[((_category.LastIndexOf('.') + 1))..]
            : _category;
        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{logLevel,-11}] {shortCategory}: {message}";
        if (exception != null)
            line += $"\n  {exception.GetType().Name}: {exception.Message}";

        try
        {
            lock (_lock)
            {
                System.IO.File.AppendAllText(_logFilePath, line + "\n");
            }
        }
        catch (System.IO.IOException) { }
    }
}
