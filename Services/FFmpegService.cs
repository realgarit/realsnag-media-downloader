using System;
using System.Diagnostics;
using System.IO;

namespace realsnag_media_downloader.Services;

public static class FFmpegService
{
    private static string? _cachedPath;

    /// <summary>
    /// Find FFmpeg using simple system commands (KISS principle)
    /// </summary>
    public static string FindFFmpeg()
    {
        // Return cached path if already found
        if (_cachedPath != null)
            return _cachedPath;

        // Strategy 1: Try using 'which' command (works on Linux/macOS)
        var whichPath = TryCommand("which ffmpeg");
        if (!string.IsNullOrEmpty(whichPath) && File.Exists(whichPath))
        {
            _cachedPath = whichPath;
            return _cachedPath;
        }

        // Strategy 2: Try 'where' command (Windows)
        var wherePath = TryCommand("where ffmpeg")?.Split('\n')[0]?.Trim();
        if (!string.IsNullOrEmpty(wherePath) && File.Exists(wherePath))
        {
            _cachedPath = wherePath;
            return _cachedPath;
        }

        // Strategy 3: Check bundled tools (for portable apps)
        var bundledPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Tools", "ffmpeg", 
            RuntimeIndicator.IsWindows ? "ffmpeg.exe" : "ffmpeg");
        
        if (File.Exists(bundledPath))
        {
            _cachedPath = bundledPath;
            return _cachedPath;
        }

        // Strategy 4: Common installation paths (Windows)
        if (RuntimeIndicator.IsWindows)
        {
            var commonPaths = new[]
            {
                @"C:\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ffmpeg", "bin", "ffmpeg.exe"),
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _cachedPath = path;
                    return _cachedPath;
                }
            }
        }

        // Strategy 5: Just return "ffmpeg" and hope it's in PATH
        _cachedPath = "ffmpeg";
        return _cachedPath;
    }

    private static string? TryCommand(string command)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = RuntimeIndicator.IsWindows ? "cmd.exe" : "/bin/bash",
                Arguments = RuntimeIndicator.IsWindows ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;
            
            process.WaitForExit(3000);
            if (process.ExitCode == 0)
            {
                return process.StandardOutput.ReadToEnd().Trim();
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    /// <summary>
    /// Clear the cached path (useful for testing)
    /// </summary>
    public static void ClearCache() => _cachedPath = null;
}

public static class RuntimeIndicator
{
    public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
    public static bool IsMac => Environment.OSVersion.Platform == PlatformID.MacOSX;
    public static bool IsLinux => Environment.OSVersion.Platform == PlatformID.Unix;
}
