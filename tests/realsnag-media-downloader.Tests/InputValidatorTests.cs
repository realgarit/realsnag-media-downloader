using realsnag_media_downloader.Services;
using FluentAssertions;

namespace realsnag_media_downloader.Tests;

public class InputValidatorTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("http://example.com/video", true)]
    [InlineData("https://vimeo.com/123456", true)]
    [InlineData("https://soundcloud.com/artist/track", true)]
    [InlineData("ftp://example.com/video", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("youtube.com/watch?v=abc", false)]
    public void IsValidUrl_ReturnsExpected(string? url, bool expected)
    {
        InputValidator.IsValidUrl(url).Should().Be(expected);
    }

    [Theory]
    [InlineData("00:00:00", true)]
    [InlineData("01:30:00", true)]
    [InlineData("0:05:30", true)]
    [InlineData("99:59:59", true)]
    [InlineData("00:60:00", false)]
    [InlineData("00:00:60", false)]
    [InlineData("abc", false)]
    [InlineData("1:2:3", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("00:00", false)]
    [InlineData("100:00:00", false)]
    public void IsValidTrimTime_ReturnsExpected(string? time, bool expected)
    {
        InputValidator.IsValidTrimTime(time).Should().Be(expected);
    }

    [Fact]
    public void IsDirectoryWritable_ReturnsFalse_ForNullPath()
    {
        InputValidator.IsDirectoryWritable(null).Should().BeFalse();
    }

    [Fact]
    public void IsDirectoryWritable_ReturnsFalse_ForNonExistentPath()
    {
        InputValidator.IsDirectoryWritable("/nonexistent/path/that/does/not/exist").Should().BeFalse();
    }

    [Fact]
    public void IsDirectoryWritable_ReturnsTrue_ForTempDirectory()
    {
        var tempDir = Path.GetTempPath();
        InputValidator.IsDirectoryWritable(tempDir).Should().BeTrue();
    }
}
