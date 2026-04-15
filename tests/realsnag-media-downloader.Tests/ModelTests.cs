using realsnag_media_downloader.Models;
using FluentAssertions;

namespace realsnag_media_downloader.Tests;

public class ModelTests
{
    [Fact]
    public void QualityOption_ToString_ReturnsLabel()
    {
        var option = new QualityOption("1080p", "bestvideo[height<=1080]+bestaudio");
        option.ToString().Should().Be("1080p");
    }

    [Fact]
    public void MediaInfo_CanBeConstructed()
    {
        var qualities = new List<QualityOption>
        {
            new("Best", "best"),
            new("720p", "bestvideo[height<=720]")
        };

        var info = new MediaInfo("Test Video", "10:30", "https://example.com/thumb.jpg", qualities);

        info.Title.Should().Be("Test Video");
        info.Duration.Should().Be("10:30");
        info.ThumbnailUrl.Should().Be("https://example.com/thumb.jpg");
        info.Qualities.Should().HaveCount(2);
    }

    [Fact]
    public void DownloadOptions_CanBeConstructed()
    {
        var opts = new DownloadOptions(
            "https://example.com/video",
            "/tmp/downloads",
            "mp4",
            "bestvideo+bestaudio",
            "00:01:00",
            "00:05:00");

        opts.Url.Should().Be("https://example.com/video");
        opts.Format.Should().Be("mp4");
        opts.TrimStart.Should().Be("00:01:00");
    }

    [Fact]
    public void AppUpdateInfo_IsUpdateAvailable()
    {
        var info = new AppUpdateInfo("2.1.4", "2.2.0", "https://github.com/releases", true);
        info.IsUpdateAvailable.Should().BeTrue();

        var noUpdate = new AppUpdateInfo("2.2.0", "2.2.0", "https://github.com/releases", false);
        noUpdate.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void DownloadProgress_RecordEquality()
    {
        var a = new DownloadProgress(50.0, "Downloading...");
        var b = new DownloadProgress(50.0, "Downloading...");
        a.Should().Be(b);
    }
}
