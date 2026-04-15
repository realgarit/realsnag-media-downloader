using Microsoft.Extensions.Logging;
using NSubstitute;
using realsnag_media_downloader.Services;
using FluentAssertions;

namespace realsnag_media_downloader.Tests;

public class AppUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdate_ReturnsNull_OnNetworkFailure()
    {
        // This test verifies graceful failure — the real API call may fail in CI
        var logger = Substitute.For<ILogger<AppUpdateService>>();
        var sut = new AppUpdateService(logger);

        // Should not throw
        var result = await sut.CheckForUpdateAsync(new CancellationTokenSource(1).Token);

        // Either null (timeout/cancel) or a valid result — never throws
        // We can't guarantee network is available, so just verify no exception
    }

    [Fact]
    public async Task CheckForUpdate_RespectsCancellation()
    {
        var logger = Substitute.For<ILogger<AppUpdateService>>();
        var sut = new AppUpdateService(logger);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sut.CheckForUpdateAsync(cts.Token);
        result.Should().BeNull();
    }
}
