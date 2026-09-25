using System;
using System.Globalization;
using FluentAssertions;
using ValheimTelemetry.Telemetry;
using Xunit;

namespace ValheimTelemetry.Tests.Telemetry;

public sealed class TelemetrySerializerTests
{
    [Fact]
    public void Serialize_EmptyEvent_ReturnsEmptyJsonObject()
    {
        // Arrange
        var telemetryEvent = new TelemetryEvent();

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{}");
    }

    [Fact]
    public void Serialize_PrimitiveProperties_PreservesOrderAndJsonTypes()
    {
        // Arrange
        var telemetryEvent = new TelemetryEvent()
            .Add("text", "Neck")
            .Add("enabled", true)
            .Add("count", 2)
            .Add("large_count", 4_294_967_296L)
            .Add("ratio", 1.25m)
            .Add("missing", null);

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{\"text\":\"Neck\",\"enabled\":true,\"count\":2,\"large_count\":4294967296,\"ratio\":1.25,\"missing\":null}");
    }

    [Fact]
    public void Serialize_StringWithSpecialCharacters_EscapesJsonControlCharacters()
    {
        // Arrange
        const string value = "quote\" slash\\ back\b form\f line\nreturn\r tab\t unit\u001f";
        var telemetryEvent = new TelemetryEvent().Add("special\nkey", value);

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{\"special\\nkey\":\"quote\\\" slash\\\\ back\\b form\\f line\\nreturn\\r tab\\t unit\\u001f\"}");
    }

    [Fact]
    public void Serialize_FloatingPointNumbers_UsesInvariantCulture()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        var telemetryEvent = new TelemetryEvent()
            .Add("single", 1.5f)
            .Add("double", 2.25d);
        string result;

        // Act
        try
        {
            result = TelemetrySerializer.Serialize(telemetryEvent);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // Assert
        result.Should().Be("{\"single\":1.5,\"double\":2.25}");
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Serialize_NonFiniteSingle_WritesNull(float value)
    {
        // Arrange
        var telemetryEvent = new TelemetryEvent().Add("value", value);

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{\"value\":null}");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Serialize_NonFiniteDouble_WritesNull(double value)
    {
        // Arrange
        var telemetryEvent = new TelemetryEvent().Add("value", value);

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{\"value\":null}");
    }

    [Fact]
    public void Serialize_UnsupportedValue_UsesInvariantStringRepresentation()
    {
        // Arrange
        var telemetryEvent = new TelemetryEvent().Add("day", DayOfWeek.Friday);

        // Act
        string result = TelemetrySerializer.Serialize(telemetryEvent);

        // Assert
        result.Should().Be("{\"day\":\"Friday\"}");
    }
}
