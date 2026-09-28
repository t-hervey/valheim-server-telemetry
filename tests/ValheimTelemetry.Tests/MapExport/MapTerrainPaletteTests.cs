using FluentAssertions;
using ValheimTelemetry.MapExport;
using Xunit;

namespace ValheimTelemetry.Tests.MapExport;

public sealed class MapTerrainPaletteTests
{
    [Fact]
    public void Select_NonOceanTerrainBelowWaterLevel_ReturnsLighterInlandWater()
    {
        // Arrange
        var biomeColor = new MapPixelColor(83, 145, 70);

        // Act
        MapPixelColor result = MapTerrainPalette.Select(false, 29.99f, biomeColor);

        // Assert
        result.Should().BeEquivalentTo(MapTerrainPalette.InlandWater);
        result.Red.Should().BeGreaterThan(MapTerrainPalette.Ocean.Red);
        result.Green.Should().BeGreaterThan(MapTerrainPalette.Ocean.Green);
        result.Blue.Should().BeGreaterThan(MapTerrainPalette.Ocean.Blue);
    }

    [Fact]
    public void Select_NonOceanTerrainAtWaterLevel_ReturnsBiomeColor()
    {
        // Arrange
        var biomeColor = new MapPixelColor(83, 145, 70);

        // Act
        MapPixelColor result = MapTerrainPalette.Select(false, MapTerrainPalette.WaterLevel, biomeColor);

        // Assert
        result.Should().BeEquivalentTo(biomeColor);
    }

    [Fact]
    public void Select_OceanBiomeAboveWaterLevel_ReturnsOceanColor()
    {
        // Arrange
        var biomeColor = new MapPixelColor(83, 145, 70);

        // Act
        MapPixelColor result = MapTerrainPalette.Select(true, 100f, biomeColor);

        // Assert
        result.Should().BeEquivalentTo(MapTerrainPalette.Ocean);
    }
}
