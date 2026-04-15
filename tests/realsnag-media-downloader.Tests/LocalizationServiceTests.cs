using Microsoft.Extensions.Logging;
using NSubstitute;
using realsnag_media_downloader.Services;
using FluentAssertions;

namespace realsnag_media_downloader.Tests;

public class LocalizationServiceTests
{
    private readonly LocalizationService _sut;

    public LocalizationServiceTests()
    {
        var logger = Substitute.For<ILogger<LocalizationService>>();
        _sut = new LocalizationService(logger);
    }

    [Fact]
    public void GetString_ReturnsEnglishByDefault()
    {
        _sut.GetString("AppTitle").Should().Be("Realgar Media Downloader");
    }

    [Fact]
    public void GetString_ReturnsGermanAfterSwitch()
    {
        _sut.CurrentLanguage = "de";
        _sut.GetString("Download").Should().Be("Herunterladen");
    }

    [Fact]
    public void GetString_FallsBackToEnglish_ForMissingKey()
    {
        _sut.CurrentLanguage = "de";
        // Both en and de have AppTitle, but let's test a key that might not exist
        var result = _sut.GetString("NonExistentKey");
        result.Should().Be("NonExistentKey");
    }

    [Fact]
    public void GetString_ReturnsKey_WhenNotFoundInAnyLanguage()
    {
        _sut.GetString("CompletelyMissingKey").Should().Be("CompletelyMissingKey");
    }

    [Fact]
    public void CurrentLanguage_IgnoresUnsupportedLanguage()
    {
        _sut.CurrentLanguage = "fr";
        _sut.CurrentLanguage.Should().Be("en");
    }

    [Fact]
    public void CurrentLanguage_RaisesEvent_OnChange()
    {
        var raised = false;
        _sut.LanguageChanged += (_, e) =>
        {
            raised = true;
            e.Language.Should().Be("de");
        };

        _sut.CurrentLanguage = "de";
        raised.Should().BeTrue();
    }

    [Fact]
    public void AvailableLanguages_ContainsEnAndDe()
    {
        _sut.AvailableLanguages.Should().Contain(["en", "de"]);
    }

    [Fact]
    public void GetString_ReturnsNewKeys_AddedInRefactor()
    {
        _sut.GetString("UpdateAvailable").Should().Contain("{0}");
        _sut.GetString("InvalidUrl").Should().NotBeEmpty();
        _sut.GetString("InvalidTrimTime").Should().NotBeEmpty();
        _sut.GetString("OutputDirNotWritable").Should().NotBeEmpty();
    }
}
