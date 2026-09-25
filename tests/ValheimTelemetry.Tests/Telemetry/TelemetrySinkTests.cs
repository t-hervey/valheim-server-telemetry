using System;
using FluentAssertions;
using Moq;
using ValheimTelemetry.Telemetry;
using Xunit;

namespace ValheimTelemetry.Tests.Telemetry;

public sealed class TelemetrySinkTests
{
    private static readonly DateTime FixedUtcTime = new DateTime(2026, 9, 25, 2, 14, 17, 421, DateTimeKind.Utc);

    [Fact]
    public void Emit_PopulateCallback_LogsPrefixedStructuredEvent()
    {
        // Arrange
        var log = new Mock<ITelemetryLog>(MockBehavior.Strict);
        string? capturedLine = null;
        log.Setup(candidate => candidate.Info(It.IsAny<string>()))
            .Callback<string>(line => capturedLine = line);
        var sut = CreateSink(log.Object);

        // Act
        sut.Emit("mob_killed", telemetryEvent => telemetryEvent.Add("mob_type", "Neck"));

        // Assert
        capturedLine.Should().Be("TEST_PREFIX {\"schema_version\":1,\"event\":\"mob_killed\",\"timestamp\":\"2026-09-25T02:14:17.421Z\",\"world\":\"TestWorld\",\"mob_type\":\"Neck\"}");
        log.Verify(candidate => candidate.Info(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Emit_TelemetryEvent_CopiesPropertiesAfterCommonEnvelope()
    {
        // Arrange
        var log = new Mock<ITelemetryLog>(MockBehavior.Strict);
        string? capturedLine = null;
        log.Setup(candidate => candidate.Info(It.IsAny<string>()))
            .Callback<string>(line => capturedLine = line);
        var sut = CreateSink(log.Object);
        var telemetryEvent = new TelemetryEvent()
            .Add("category", "active_mob")
            .Add("count", 2);

        // Act
        sut.Emit("entity_count", telemetryEvent);

        // Assert
        capturedLine.Should().Be("TEST_PREFIX {\"schema_version\":1,\"event\":\"entity_count\",\"timestamp\":\"2026-09-25T02:14:17.421Z\",\"world\":\"TestWorld\",\"category\":\"active_mob\",\"count\":2}");
        log.Verify(candidate => candidate.Info(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Emit_PopulateCallbackThrows_LogsFailureWithoutWritingEvent()
    {
        // Arrange
        var log = new Mock<ITelemetryLog>(MockBehavior.Strict);
        string? capturedInfo = null;
        string? capturedError = null;
        log.Setup(candidate => candidate.Info(It.IsAny<string>()))
            .Callback<string>(line => capturedInfo = line);
        log.Setup(candidate => candidate.Error(It.IsAny<string>()))
            .Callback<string>(line => capturedError = line);
        var sut = CreateSink(log.Object);

        // Act
        sut.Emit("broken", _ => throw new InvalidOperationException("test failure"));

        // Assert
        capturedInfo.Should().BeNull();
        capturedError.Should().StartWith("ValheimTelemetry sink failed safely:")
            .And.Contain("test failure");
        log.Verify(candidate => candidate.Info(It.IsAny<string>()), Times.Never);
        log.Verify(candidate => candidate.Error(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Emit_NullPopulateCallback_LogsCommonEnvelopeOnly()
    {
        // Arrange
        var log = new Mock<ITelemetryLog>(MockBehavior.Strict);
        string? capturedLine = null;
        log.Setup(candidate => candidate.Info(It.IsAny<string>()))
            .Callback<string>(line => capturedLine = line);
        var sut = CreateSink(log.Object);

        // Act
        sut.Emit("heartbeat", (Action<TelemetryEvent>?)null);

        // Assert
        capturedLine.Should().Be("TEST_PREFIX {\"schema_version\":1,\"event\":\"heartbeat\",\"timestamp\":\"2026-09-25T02:14:17.421Z\",\"world\":\"TestWorld\"}");
        log.Verify(candidate => candidate.Info(It.IsAny<string>()), Times.Once);
    }

    private static TelemetrySink CreateSink(ITelemetryLog log)
    {
        return new TelemetrySink(log, "TEST_PREFIX", () => FixedUtcTime, () => "TestWorld");
    }
}
