using System.Collections.Generic;
using FluentAssertions;
using ValheimTelemetry.Tracking;
using Xunit;

namespace ValheimTelemetry.Tests.Tracking;

public sealed class PortalTripDetectorTests
{
    [Fact]
    public void Observe_ConnectedPortalPositionJump_ReturnsTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 1f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 101f, 0f, 0f, 10.5f, portals);

        // Assert
        result.Should().NotBeNull();
        result!.From.Id.Should().Be("portal-a");
        result.To.Id.Should().Be("portal-b");
    }

    [Fact]
    public void Observe_FirstPositionAtDestination_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 100f, 0f, 0f, 10f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_JumpToUnrelatedPosition_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 50f, 0f, 50f, 10.5f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_JumpToPortalThatIsNotConnectedToSource_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        portals.Add(Portal("portal-c", "portal-d", 200f));
        portals.Add(Portal("portal-d", "portal-c", 300f));
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 200f, 0f, 0f, 10.5f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_SourceConnectionTargetIsAbsent_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = new List<PortalTripDetector.Portal>
        {
            Portal("portal-a", "missing", 0f)
        };
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 100f, 0f, 0f, 10.5f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_SampleGapExceedsLimit_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 100f, 0f, 0f, 16f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_ConnectedPortalsCloserThanMinimumJump_DoesNotReturnTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = new List<PortalTripDetector.Portal>
        {
            Portal("portal-a", "portal-b", 0f),
            Portal("portal-b", "portal-a", 10f)
        };
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 10f, 0f, 0f, 10.5f, portals);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_ReverseTripDuringCooldown_DoesNotReturnSecondTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);
        PortalTripDetector.Trip? firstTrip = sut.Observe(42L, 100f, 0f, 0f, 10.5f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 0f, 0f, 0f, 11f, portals);

        // Assert
        firstTrip.Should().NotBeNull();
        result.Should().BeNull();
    }

    [Fact]
    public void Observe_ReverseTripAfterCooldown_ReturnsSecondTrip()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);
        sut.Observe(42L, 100f, 0f, 0f, 10.5f, portals);
        sut.Observe(42L, 100f, 0f, 0f, 16f, portals);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 0f, 0f, 0f, 16.5f, portals);

        // Assert
        result.Should().NotBeNull();
        result!.From.Id.Should().Be("portal-b");
        result.To.Id.Should().Be("portal-a");
    }

    [Fact]
    public void Remove_PreviousSourceStateIsDiscarded()
    {
        // Arrange
        var sut = new PortalTripDetector();
        IList<PortalTripDetector.Portal> portals = ConnectedPortals();
        sut.Observe(42L, 0f, 0f, 0f, 10f, portals);
        sut.Remove(42L);

        // Act
        PortalTripDetector.Trip? result = sut.Observe(42L, 100f, 0f, 0f, 10.5f, portals);

        // Assert
        result.Should().BeNull();
    }

    private static IList<PortalTripDetector.Portal> ConnectedPortals()
    {
        return new List<PortalTripDetector.Portal>
        {
            Portal("portal-a", "portal-b", 0f),
            Portal("portal-b", "portal-a", 100f)
        };
    }

    private static PortalTripDetector.Portal Portal(string id, string targetId, float x)
    {
        return new PortalTripDetector.Portal
        {
            Id = id,
            TargetId = targetId,
            Type = "portal_wood",
            Tag = "test",
            X = x,
            Y = 0f,
            Z = 0f
        };
    }
}
