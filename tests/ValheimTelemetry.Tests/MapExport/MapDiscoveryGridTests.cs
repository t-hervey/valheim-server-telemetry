using FluentAssertions;
using ValheimTelemetry.MapExport;
using Xunit;

namespace ValheimTelemetry.Tests.MapExport;

public sealed class MapDiscoveryGridTests
{
    [Fact]
    public void Explore_NewWorldPosition_MarksCenterAndReturnsTrue()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(100);

        // Act
        bool result = sut.Explore(0f, 0f, 100f);

        // Assert
        result.Should().BeTrue();
        sut.Pixels[50 * 100 + 50].Should().Be(1);
    }

    [Fact]
    public void Explore_PreviouslyExploredPosition_ReturnsFalse()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(100);
        sut.Explore(0f, 0f, 100f);

        // Act
        bool result = sut.Explore(0f, 0f, 100f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Explore_PositiveWorldZ_MapsTowardImageTop()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(100);

        // Act
        sut.Explore(0f, 10000f, 1f);

        // Assert
        sut.Pixels[2 * 100 + 50].Should().Be(1);
    }

    [Fact]
    public void MergeNative_ExploredPixel_MapsIntoOutputGrid()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(100);
        var native = new byte[16];
        native[3 * 4 + 2] = 1;

        // Act
        bool result = sut.MergeNative(native, 4, 1000f);

        // Assert
        result.Should().BeTrue();
        sut.Pixels[45 * 100 + 50].Should().Be(1);
    }

    [Fact]
    public void MergeNative_InvalidDimensions_DoesNotChangeGrid()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(10);

        // Act
        bool result = sut.MergeNative(new byte[3], 2, 1f);

        // Assert
        result.Should().BeFalse();
        sut.Pixels.Should().OnlyContain(value => value == 0);
    }

    [Fact]
    public void Load_MatchingBuffer_RestoresDiscovery()
    {
        // Arrange
        var sut = new MapDiscoveryGrid(2);
        byte[] persisted = { 0, 1, 1, 0 };

        // Act
        sut.Load(persisted);

        // Assert
        sut.Pixels.Should().Equal(persisted);
    }
}
