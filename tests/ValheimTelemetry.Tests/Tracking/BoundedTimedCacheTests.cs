using System;
using FluentAssertions;
using ValheimTelemetry.Tracking;
using Xunit;

namespace ValheimTelemetry.Tests.Tracking;

public sealed class BoundedTimedCacheTests
{
    [Fact]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int invalidCapacity = 0;

        // Act
        Action act = () => _ = new BoundedTimedCache<string>(invalidCapacity, 1f, () => 0f);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    [Fact]
    public void Constructor_NegativeRetention_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const float invalidRetention = -1f;

        // Act
        Action act = () => _ = new BoundedTimedCache<string>(1, invalidRetention, () => 0f);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("retentionSeconds");
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        // Arrange
        Func<float> nullClock = null!;

        // Act
        Action act = () => _ = new BoundedTimedCache<string>(1, 1f, nullClock);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clock");
    }

    [Fact]
    public void Constructor_ZeroRetention_CreatesCache()
    {
        // Arrange
        const float zeroRetention = 0f;

        // Act
        var result = new BoundedTimedCache<string>(1, zeroRetention, () => 0f);

        // Assert
        result.Should().NotBeNull();
    }

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
    public void ContainsRecent_EntryExactlyAtMaximumAge_ReturnsTrue()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 30f, () => now);
        sut.Touch("target");
        now = 13f;

        // Act
        bool result = sut.ContainsRecent("target", 3f);

        // Assert
        result.Should().BeTrue();
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
    public void ContainsRecent_EntryExactlyAtRetention_ReturnsTrue()
    {
        // Arrange
        float now = 10f;
        var sut = new BoundedTimedCache<string>(2, 5f, () => now);
        sut.Touch("target");
        now = 15f;

        // Act
        bool result = sut.ContainsRecent("target", 5f);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Touch_AtCapacity_RetainsAllEntries()
    {
        // Arrange
        var sut = new BoundedTimedCache<string>(2, 30f, () => 10f);
        sut.Touch("first");

        // Act
        sut.Touch("second");

        // Assert
        sut.Count.Should().Be(2);
    }

    [Fact]
    public void Touch_OverCapacity_TrimsImmediately()
    {
        // Arrange
        var sut = new BoundedTimedCache<string>(2, 30f, () => 10f);
        sut.Touch("first");
        sut.Touch("second");

        // Act
        sut.Touch("third");

        // Assert
        sut.Count.Should().Be(2);
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
