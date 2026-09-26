using System;
using FluentAssertions;
using ValheimTelemetry.MapExport;
using Xunit;

namespace ValheimTelemetry.Tests.MapExport;

public sealed class PngEncoderTests
{
    [Fact]
    public void EncodeRgb_ValidPixelBuffer_WritesPngSignatureAndDimensions()
    {
        // Arrange
        byte[] rgb = { 255, 0, 0, 0, 255, 0 };

        // Act
        byte[] result = PngEncoder.EncodeRgb(2, 1, rgb);

        // Assert
        result.AsSpan(0, 8).ToArray().Should().Equal(137, 80, 78, 71, 13, 10, 26, 10);
        ReadBigEndian(result, 16).Should().Be(2);
        ReadBigEndian(result, 20).Should().Be(1);
        result.AsSpan(result.Length - 8, 4).ToArray().Should().Equal(73, 69, 78, 68);
    }

    [Fact]
    public void EncodeRgb_MismatchedBuffer_ThrowsArgumentException()
    {
        // Arrange
        byte[] invalid = { 1, 2, 3 };

        // Act
        Action act = () => PngEncoder.EncodeRgb(2, 1, invalid);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("rgb");
    }

    private static uint ReadBigEndian(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }
}
