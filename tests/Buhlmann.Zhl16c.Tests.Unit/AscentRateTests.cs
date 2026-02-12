using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class AscentRateTests
{
    private readonly AscentDescentSettings _settings = new()
    {
        DescentRateMmSec = 5 * 1000 / 60,
        AscentRate75MmSec = 6 * 1000 / 60,
        AscentRate50MmSec = 5 * 1000 / 60,
        AscentRateStopsMmSec = 4 * 1000 / 60,
        AscentRateLast6mMmSec = 1 * 1000 / 60
    };

    [Fact]
    public void GetAscentRate_ShouldReturn75_WhenCurrentDepthIsGreaterThan75PercentOfDepth()
    {
        // Arrange

        // Act
        var response = AscentRate.GetAscentRate(20 * 1000, 25 * 1000, _settings);

        // Assert
        Assert.Equal(_settings.AscentRate75MmSec, response);
    }

    [Fact]
    public void GetAscentRate_ShouldReturn50_WhenCurrentDepthIsGreaterThan50PercentOfDepth()
    {
        // Arrange

        // Act
        var response = AscentRate.GetAscentRate(15 * 1000, 25 * 1000,
            _settings);

        // Assert
        Assert.Equal(_settings.AscentRate50MmSec, response);
    }

    [Fact]
    public void GetAscentRate_ShouldReturnStop_WhenCurrentDepthIsGreaterThan50PercentOfDepth()
    {
        // Arrange

        // Act
        var response = AscentRate.GetAscentRate(7 * 1000, 25 * 1000, _settings);

        // Assert
        Assert.Equal(_settings.AscentRateStopsMmSec, response);
    }

    [Fact]
    public void GetAscentRate_ShouldReturnLast6m_WhenCurrentDepthIsLessThan6m()
    {
        // Arrange

        // Act
        var response = AscentRate.GetAscentRate(3 * 1000, 25 * 1000, _settings);

        // Assert
        Assert.Equal(_settings.AscentRateLast6mMmSec, response);
    }
}