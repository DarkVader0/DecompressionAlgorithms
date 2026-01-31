using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class GasConsumptionTests
{
    private readonly DiveContext _seaWaterDiveContext;

    public GasConsumptionTests()
    {
        _seaWaterDiveContext = new DiveContext(GasConstants.StandardPressureMbar, WaterType.Salt);
    }

    [Fact]
    public void CalculateAtDepth_ShouldReturnZeroConsumption_WhenDurationIsZero()
    {
        // Arrange
        var depthMm = 20000u;
        var durationSeconds = 0u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.Equal(0u, gasUsed);
    }

    [Fact]
    public void CalculateAtDepth_ShouldReturnSac_WhenOnSurfaceForOneMinute()
    {
        // Arrange
        var depthMm = 0u;
        var durationSeconds = 60u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.Equal(sacMlMin, gasUsed);
    }

    [Fact]
    public void CalculateAtDepth_ShouldReturnTwiceTheSac_WhenAtTenMetersForOneMinute()
    {
        // Arrange
        var depthMm = 10000u;
        var durationSeconds = 60u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.InRange(gasUsed, sacMlMin * 2 - 100, sacMlMin * 2 + 100);
    }

    [Fact]
    public void CalculateAtDepth_ShouldBeThreeTimesTheSac_WhenAtTwentyMetersForOneMinute()
    {
        // Arrange
        var depthMm = 20000u;
        var durationSeconds = 60u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.InRange(gasUsed, sacMlMin * 3 - 100, sacMlMin * 3 + 100);
    }

    [Fact]
    public void CalculateAtDepth_ShouldBeTwoTimesTheSac_WhenAtSurfaceForTwoMinutes()
    {
        // Arrange
        var depthMm = 0u;
        var durationSeconds = 120u;
        var sacMlMin = 15000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.Equal(sacMlMin * 2, gasUsed);
    }

    [Fact]
    public void CalculateAtDepth_ShouldBeFourTimesTheSac_WhenAtTenMetersForTwoMinutes()
    {
        // Arrange
        var depthMm = 10000u;
        var durationSeconds = 120u;
        var sacMlMin = 15000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateAtDepth(depthMm, durationSeconds, sacMlMin, _seaWaterDiveContext);

        // Assert
        Assert.InRange(gasUsed, sacMlMin * 4 - 100, sacMlMin * 4 + 100);
    }

    [Fact]
    public void CalculateForTransition_ShouldReturnZeroConsumption_WhenDurationIsZero()
    {
        // Arrange
        var startDepthMm = 0u;
        var endDepthMm = 20000u;
        var durationSeconds = 0u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateForTransition(startDepthMm, endDepthMm, durationSeconds, sacMlMin,
                _seaWaterDiveContext);

        // Assert
        Assert.Equal(0u, gasUsed);
    }

    [Fact]
    public void CalculateForTransition_ShouldReturnSac_WhenOnSurfaceAndTransitionIsZero()
    {
        // Arrange
        var startDepthMm = 0u;
        var endDepthMm = 0u;
        var durationSeconds = 60u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateForTransition(startDepthMm, endDepthMm, durationSeconds, sacMlMin,
                _seaWaterDiveContext);

        // Assert
        Assert.Equal(sacMlMin, gasUsed);
    }

    [Fact]
    public void CalculateForTransition_ShouldBeTwiceTheSac_WhenTransitioningBetweenSurfaceAndTwentyMetersOverOneMinute()
    {
        // Arrange
        var startDepthMm = 0u;
        var endDepthMm = 20000u;
        var durationSeconds = 60u;
        var sacMlMin = 12000u;

        // Act
        var gasUsed =
            GasConsumption.CalculateForTransition(startDepthMm, endDepthMm, durationSeconds, sacMlMin,
                _seaWaterDiveContext);

        // Assert
        Assert.InRange(gasUsed, sacMlMin * 2 - 100, sacMlMin * 2 + 100);
    }

    [Fact]
    public void TransitionDuration_ShouldBeZero_WhenStartAndEndDepthAreTheSame()
    {
        // Arrange
        var startDepthMm = 15000u;
        var endDepthMm = 15000u;
        var rateMmMin = 10000u;

        // Act
        var durationSeconds =
            GasConsumption.TransitionDuration(startDepthMm, endDepthMm, rateMmMin);

        // Assert
        Assert.Equal(0u, durationSeconds);
    }

    [Fact]
    public void TransitionDuration_ShouldBeSixtySeconds_WhenChangingDepthByTenMetersAtTenMetersPerMinute()
    {
        // Arrange
        var startDepthMm = 0u;
        var endDepthMm = 10000u;
        var rateMmMin = 10000u;

        // Act
        var durationSeconds =
            GasConsumption.TransitionDuration(startDepthMm, endDepthMm, rateMmMin);

        // Assert
        Assert.Equal(60u, durationSeconds);
    }

    [Fact]
    public void TransitionDuration_ShouldBeThirtySeconds_WhenChangingDepthByFiveMetersAtTenMetersPerMinute()
    {
        // Arrange
        var startDepthMm = 10000u;
        var endDepthMm = 5000u;
        var rateMmMin = 10000u;

        // Act
        var durationSeconds =
            GasConsumption.TransitionDuration(startDepthMm, endDepthMm, rateMmMin);

        // Assert
        Assert.Equal(30u, durationSeconds);
    }

    [Fact]
    public void RemainingPressureMbar_ShouldReturnZero_WhenGasUsedExceedsTotalGas()
    {
        // Arrange
        var startPressureMbar = 200000u;
        var gasUsedMl = 5000000u;
        var cylinderVolumeMl = 10000u;

        // Act
        var remainingPressureMbar =
            GasConsumption.RemainingPressureMbar(startPressureMbar, gasUsedMl, cylinderVolumeMl);

        // Assert
        Assert.Equal(0u, remainingPressureMbar);
    }

    [Fact]
    public void RemainingPressureMbar_ShouldReturn100Bar_When100BarIsUsedOutOf200BarCylinder()
    {
        // Arrange
        var startPressureMbar = 200000u;
        var gasUsedMl = 1000000u;
        var cylinderVolumeMl = 10000u;

        // Act
        var remainingPressureMbar =
            GasConsumption.RemainingPressureMbar(startPressureMbar, gasUsedMl, cylinderVolumeMl);

        // Assert
        Assert.Equal(100000u, remainingPressureMbar);
    }
}