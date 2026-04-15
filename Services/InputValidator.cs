using System;
using System.IO;
using System.Text.RegularExpressions;

namespace realsnag_media_downloader.Services;

public static partial class InputValidator
{
    [GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^\d{1,2}:\d{2}:\d{2}$")]
    private static partial Regex TimeFormatRegex();

    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return UrlRegex().IsMatch(url.Trim());
    }

    public static bool IsValidTrimTime(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return false;

        if (!TimeFormatRegex().IsMatch(time))
            return false;

        // Validate the actual time components
        var parts = time.Split(':');
        if (parts.Length != 3)
            return false;

        return int.TryParse(parts[0], out var h) && h >= 0 &&
               int.TryParse(parts[1], out var m) && m is >= 0 and < 60 &&
               int.TryParse(parts[2], out var s) && s is >= 0 and < 60;
    }

    public static bool IsDirectoryWritable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            if (!Directory.Exists(path))
                return false;

            // Try creating a temp file to verify write access
            var testFile = Path.Combine(path, $".write_test_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "");
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
