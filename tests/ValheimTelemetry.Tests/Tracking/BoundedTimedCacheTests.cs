using FluentAssertions;
using ValheimTelemetry.Tracking;
using Xunit;

namespace ValheimTelemetry.Tests.Tracking;

public sealed class BoundedTimedCacheTests
{
    [Fact]
    public void ContainsRecent_EntryWithinMaximumAge_ReturnsTrue()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 30f, () => now);
        sut.Touch("target");
        now = 12f;

        // Act
        bool result = sut.ContainsRecent("target", 3f);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsRecent_EntryOlderThanMaximumAge_ReturnsFalse()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 30f, () => now);
        sut.Touch("target");
        now = 14f;

        // Act
        bool result = sut.ContainsRecent("target", 3f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsRecent_EntryBeyondRetention_RemovesAndReturnsFalse()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 5f, () => now);
        sut.Touch("target");
        now = 16f;

        // Act
        bool result = sut.ContainsRecent("target", 10f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsRecent_OldestEntryOverCapacity_ReturnsFalse()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 30f, () => now);
        sut.Touch("first");
        sut.Touch("second");
        sut.Touch("third");

        // Act
        bool result = sut.ContainsRecent("first", 1f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsRecent_RefreshedEntrySurvivesOlderQueuedTimestamp()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(3, 5f, () => now);
        sut.Touch("target");
        now = 14f;
        sut.Touch("target");
        now = 16f;

        // Act
        bool result = sut.ContainsRecent("target", 3f);

        // Assert
        result.Should().BeTrue();
    }
}
