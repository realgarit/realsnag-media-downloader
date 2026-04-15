using Microsoft.Extensions.Logging;
using NSubstitute;
using realsnag_media_downloader.Services;
using FluentAssertions;

namespace realsnag_media_downloader.Tests;

public class SettingsServiceTests
{
    private readonly SettingsService _sut;

    public SettingsServiceTests()
    {
        var logger = Substitute.For<ILogger<SettingsService>>();
        _sut = new SettingsService(logger);
    }

    [Fact]
    public void OutputDirectory_IsNotNullOrEmpty()
    {
        _sut.OutputDirectory.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IsDarkTheme_RaisesThemeChanged_WhenValueChanges()
    {
        var original = _sut.IsDarkTheme;
        var raised = false;

        void Handler(object? s, ThemeChangedEventArgs e) => raised = true;
        _sut.ThemeChanged += Handler;

        _sut.IsDarkTheme = !original;
        raised.Should().BeTrue();

        // Cleanup: unhook before restoring
        _sut.ThemeChanged -= Handler;
        _sut.IsDarkTheme = original;
    }

    [Fact]
    public void IsDarkTheme_DoesNotRaise_WhenSameValue()
    {
        var currentValue = _sut.IsDarkTheme;
        var raised = false;
        _sut.ThemeChanged += (_, _) => raised = true;

        _sut.IsDarkTheme = currentValue;
        raised.Should().BeFalse();
    }

    [Fact]
    public void Language_RaisesLanguageChanged_WhenValueChanges()
    {
        var original = _sut.Language;
        var newLang = original == "en" ? "de" : "en";
        var raised = false;

        void Handler(object? s, LanguageChangedEventArgs e) => raised = true;
        _sut.LanguageChanged += Handler;

        _sut.Language = newLang;
        raised.Should().BeTrue();

        // Cleanup
        _sut.LanguageChanged -= Handler;
        _sut.Language = original;
    }

    [Fact]
    public void Language_DoesNotRaise_WhenSameValue()
    {
        var currentValue = _sut.Language;
        var raised = false;
        _sut.LanguageChanged += (_, _) => raised = true;

        _sut.Language = currentValue;
        raised.Should().BeFalse();
    }

    [Fact]
    public void AutoUpdateYtDlp_CanBeToggled()
    {
        var original = _sut.AutoUpdateYtDlp;
        _sut.AutoUpdateYtDlp = !original;
        _sut.AutoUpdateYtDlp.Should().Be(!original);

        // Restore
        _sut.AutoUpdateYtDlp = original;
    }
}
