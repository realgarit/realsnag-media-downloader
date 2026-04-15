using System.Collections.Generic;

namespace realsnag_media_downloader.Models;

public record MediaInfo(
    string Title,
    string Duration,
    string? ThumbnailUrl,
    List<QualityOption> Qualities);

public record QualityOption(string Label, string FormatArg)
{
    public override string ToString() => Label;
}

public record DownloadOptions(
    string Url,
    string OutputDir,
    string Format,
    string? QualityFormatArg,
    string? TrimStart,
    string? TrimEnd);

public record DownloadProgress(double Percentage, string Line);
