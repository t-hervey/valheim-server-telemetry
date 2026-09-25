using System.Collections.Generic;
using FluentAssertions;
using ValheimTelemetry.Telemetry;
using Xunit;

namespace ValheimTelemetry.Tests.Telemetry;

public sealed class TelemetryEventTests
{
    [Fact]
    public void Add_SingleProperty_AppendsPropertyAndReturnsSameEvent()
    {
        // Arrange
        const string propertyName = "count";
        const int propertyValue = 1;
        var telemetryEvent = new TelemetryEvent();

        // Act
        TelemetryEvent result = telemetryEvent.Add(propertyName, propertyValue);

        // Assert
        result.Should().BeSameAs(telemetryEvent);
        telemetryEvent.Properties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object>(propertyName, propertyValue));
    }
}
