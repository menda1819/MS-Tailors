using FluentAssertions;
using MSTailors.Application.Common.Interfaces;
using MSTailors.Application.Common.Models;
using MSTailors.Application.Health;

namespace MSTailors.Tests.Health;

public class HealthServiceTests
{
    private sealed class StubProbe : IDatabaseHealthProbe
    {
        private readonly DatabaseHealth _result;
        public StubProbe(DatabaseHealth result) => _result = result;

        public Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class StubApplicationInfo : IApplicationInfo
    {
        public string Name => "MSTailors.Api";
        public string Version => "1.2.3";
        public string Environment => "Testing";
    }

    private static HealthService CreateSut(DatabaseHealth probeResult)
        => new(new StubProbe(probeResult), new StubApplicationInfo());

    [Fact]
    public async Task Reports_healthy_when_the_database_responds()
    {
        var sut = CreateSut(new DatabaseHealth(true, "mstailors", 12, null));

        var result = await sut.GetHealthAsync();

        result.Status.Should().Be("Healthy");
        result.Database.Status.Should().Be("Connected");
        result.Database.Name.Should().Be("mstailors");
        result.Database.LatencyMs.Should().Be(12);
        result.Database.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_degraded_when_the_database_is_unreachable()
    {
        var sut = CreateSut(new DatabaseHealth(false, "mstailors", 5000, "TimeoutException"));

        var result = await sut.GetHealthAsync();

        result.Status.Should().Be("Degraded");
        result.Database.Status.Should().Be("Disconnected");
        result.Database.Error.Should().Be("TimeoutException");
    }

    [Fact]
    public async Task Surfaces_build_metadata_from_application_info()
    {
        var sut = CreateSut(new DatabaseHealth(true, "mstailors", 1, null));

        var result = await sut.GetHealthAsync();

        result.Application.Should().Be("MSTailors.Api");
        result.Version.Should().Be("1.2.3");
        result.Environment.Should().Be("Testing");
    }
}
