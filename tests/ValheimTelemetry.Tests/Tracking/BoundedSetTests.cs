using System;
using FluentAssertions;
using ValheimTelemetry.Tracking;
using Xunit;

namespace ValheimTelemetry.Tests.Tracking;

public sealed class BoundedSetTests
{
    [Fact]
    public void Add_NewValue_ReturnsTrueAndRetainsValue()
    {
        // Arrange
        var sut = new BoundedSet<string>(1);

        // Act
        bool result = sut.Add("first");

        // Assert
        result.Should().BeTrue();
        sut.Contains("first").Should().BeTrue();
    }

    [Fact]
    public void Add_DuplicateValue_ReturnsFalse()
    {
        // Arrange
        var sut = new BoundedSet<string>(1);
        sut.Add("first");

        // Act
        bool result = sut.Add("first");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Add_OverCapacity_EvictsOldestValue()
    {
        // Arrange
        var sut = new BoundedSet<string>(2);
        sut.Add("first");
        sut.Add("second");

        // Act
        sut.Add("third");

        // Assert
        sut.Contains("first").Should().BeFalse();
        sut.Contains("second").Should().BeTrue();
        sut.Contains("third").Should().BeTrue();
    }

    [Fact]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int invalidCapacity = 0;

        // Act
        Action act = () => _ = new BoundedSet<string>(invalidCapacity);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }
}
