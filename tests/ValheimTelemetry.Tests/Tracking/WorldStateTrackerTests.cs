using System.Collections.Generic;
using FluentAssertions;
using Moq;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Tracking;
using Xunit;

namespace ValheimTelemetry.Tests.Tracking;

public sealed class WorldStateTrackerTests
{
    [Fact]
    public void KeySet_NewValuelessKey_EmitsAddedEvent()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        string? capturedName = null;
        TelemetryEvent? capturedEvent = null;
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((name, telemetryEvent) =>
            {
                capturedName = name;
                capturedEvent = telemetryEvent;
            });
        var sut = new WorldStateTracker(true, sink.Object);

        // Act
        sut.KeySet("Defeated_Eikthyr", false, null!);

        // Assert
        capturedName.Should().Be("world_key_changed");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Properties.Should().Equal(
            new KeyValuePair<string, object>("action", "added"),
            new KeyValuePair<string, object>("key", "defeated_eikthyr"),
            new KeyValuePair<string, object>("value", null!),
            new KeyValuePair<string, object>("previous_value", null!));
        sink.Verify(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()), Times.Once);
    }

    [Fact]
    public void KeySet_ExistingKeyWithDifferentValue_EmitsUpdatedEvent()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        TelemetryEvent? capturedEvent = null;
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((_, telemetryEvent) => capturedEvent = telemetryEvent);
        var sut = new WorldStateTracker(true, sink.Object);

        // Act
        sut.KeySet("PlayerDamage 150", true, "100");

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Properties.Should().Equal(
            new KeyValuePair<string, object>("action", "updated"),
            new KeyValuePair<string, object>("key", "playerdamage"),
            new KeyValuePair<string, object>("value", "150"),
            new KeyValuePair<string, object>("previous_value", "100"));
        sink.Verify(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()), Times.Once);
    }

    [Fact]
    public void KeySet_ExistingKeyWithSameValue_DoesNotEmit()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        var capturedEvents = new List<TelemetryEvent>();
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((_, telemetryEvent) => capturedEvents.Add(telemetryEvent));
        var sut = new WorldStateTracker(true, sink.Object);

        // Act
        sut.KeySet("PlayerDamage VALUE", true, "value");

        // Assert
        capturedEvents.Should().BeEmpty();
    }

    [Fact]
    public void KeyRemoved_ExistingValuedKey_EmitsRemovedEvent()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        TelemetryEvent? capturedEvent = null;
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((_, telemetryEvent) => capturedEvent = telemetryEvent);
        var sut = new WorldStateTracker(true, sink.Object);

        // Act
        sut.KeyRemoved("PlayerDamage ignored", "150");

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Properties.Should().Equal(
            new KeyValuePair<string, object>("action", "removed"),
            new KeyValuePair<string, object>("key", "playerdamage"),
            new KeyValuePair<string, object>("value", null!),
            new KeyValuePair<string, object>("previous_value", "150"));
        sink.Verify(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()), Times.Once);
    }

    [Fact]
    public void KeySet_DisabledTracker_DoesNotEmit()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        var capturedEvents = new List<TelemetryEvent>();
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((_, telemetryEvent) => capturedEvents.Add(telemetryEvent));
        var sut = new WorldStateTracker(false, sink.Object);

        // Act
        sut.KeySet("defeated_eikthyr", false, null!);

        // Assert
        capturedEvents.Should().BeEmpty();
    }

    [Fact]
    public void KeySet_EmptyKey_DoesNotEmit()
    {
        // Arrange
        var sink = new Mock<ITelemetrySink>(MockBehavior.Strict);
        var capturedEvents = new List<TelemetryEvent>();
        sink.Setup(candidate => candidate.Emit(It.IsAny<string>(), It.IsAny<TelemetryEvent>()))
            .Callback<string, TelemetryEvent>((_, telemetryEvent) => capturedEvents.Add(telemetryEvent));
        var sut = new WorldStateTracker(true, sink.Object);

        // Act
        sut.KeySet(string.Empty, false, null!);

        // Assert
        capturedEvents.Should().BeEmpty();
    }
}
