using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class WaitUntilTests
{
    private const double StandardPressureBar = 1.013;

    private static readonly DiveContext DefaultContext =
        new(1013, WaterType.Salt);

    private static readonly AscentDescentSettings DefaultAscentSettings = new()
    {
        AscentRate75MmSec = 9 * 1000 / 60,
        AscentRate50MmSec = 9 * 1000 / 60,
        AscentRateStopsMmSec = 9 * 1000 / 60,
        AscentRateLast6mMmSec = 3 * 1000 / 60
    };

    [Fact]
    public void FindClearTime_ShouldReturnTimeout_WhenMinExceeds48Hours()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);

        // Act
        var result = WaitUntil.FindClearTime(
            ref state, 0, 48 * 3600, 61, 60,
            30000, 0, 30000, air, DiveMode.OC, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.Equal(50 * 3600, result);
    }

    [Fact]
    public void FindClearTime_ShouldReturnMultipleOfStepSize_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);
        state.AddSegment(DefaultContext.DepthToBar(40000), air, 1500, DiveMode.OC, 0);

        // Act
        var result = WaitUntil.FindClearTime(
            ref state, 0, 0, 61, 60,
            9000, 6000, 40000, air, DiveMode.OC, 0,
            0.85, 0.95, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.Equal(0, result % 60);
    }

    [Fact]
    public void FindClearTime_ShouldReturnPositiveTime_WhenDecoIsRequired()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);
        state.AddSegment(DefaultContext.DepthToBar(50000), air, 1800, DiveMode.OC, 0);

        // Act
        var result = WaitUntil.FindClearTime(
            ref state, 0, 0, 61, 60,
            9000, 6000, 50000, air, DiveMode.OC, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void FindClearTime_ShouldNotModifyDecoState_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);
        state.AddSegment(DefaultContext.DepthToBar(40000), air, 1200, DiveMode.OC, 0);
        var saved = state.Clone();

        // Act
        WaitUntil.FindClearTime(
            ref state, 0, 0, 61, 60,
            9000, 6000, 40000, air, DiveMode.OC, 0,
            0.85, 0.95, DefaultAscentSettings, DefaultContext);

        // Assert
        unsafe
        {
            for (var i = 0; i < 16; i++)
            {
                Assert.Equal(saved.TissueN2Sat[i], state.TissueN2Sat[i]);
                Assert.Equal(saved.TissueHeSat[i], state.TissueHeSat[i]);
            }
        }
    }

    [Fact]
    public void FindClearTime_ShouldReturnLongerTime_WhenDiveIsDeeper()
    {
        // Arrange
        var air = new GasMix(210, 0);

        var stateShallow = new DecoState();
        stateShallow.Clear(StandardPressureBar);
        stateShallow.AddSegment(DefaultContext.DepthToBar(30000), air, 1200, DiveMode.OC, 0);

        var stateDeep = new DecoState();
        stateDeep.Clear(StandardPressureBar);
        stateDeep.AddSegment(DefaultContext.DepthToBar(50000), air, 1200, DiveMode.OC, 0);

        // Act
        var resultShallow = WaitUntil.FindClearTime(
            ref stateShallow, 0, 0, 61, 60,
            6000, 3000, 30000, air, DiveMode.OC, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        var resultDeep = WaitUntil.FindClearTime(
            ref stateDeep, 0, 0, 61, 60,
            6000, 3000, 50000, air, DiveMode.OC, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(resultDeep >= resultShallow);
    }

    [Fact]
    public void FindClearTime_ShouldReturnShorterTime_WhenGfIsHigher()
    {
        // Arrange
        var air = new GasMix(210, 0);

        var stateConservative = new DecoState();
        stateConservative.Clear(StandardPressureBar);
        stateConservative.AddSegment(DefaultContext.DepthToBar(40000), air, 1500, DiveMode.OC, 0);

        var stateAggressive = new DecoState();
        stateAggressive.Clear(StandardPressureBar);
        stateAggressive.AddSegment(DefaultContext.DepthToBar(40000), air, 1500, DiveMode.OC, 0);

        // Act
        var resultConservative = WaitUntil.FindClearTime(
            ref stateConservative, 0, 0, 61, 60,
            9000, 6000, 40000, air, DiveMode.OC, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        var resultAggressive = WaitUntil.FindClearTime(
            ref stateAggressive, 0, 0, 61, 60,
            9000, 6000, 40000, air, DiveMode.OC, 0,
            0.85, 0.95, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(resultAggressive <= resultConservative);
    }

    [Fact]
    public void FindClearTime_ShouldReturn5Minutes_WhenDiving45mFor31MinOnAirAndAscendingOnNx25()
    {
        // Arrange
        var nx25 = new GasMix(250, 0);
        var state = DecoState.CreateAtSurface();

        state.AddSegment(DefaultContext.DepthToBar(22500), nx25, 9 * 60, DiveMode.OC, 0);
        state.AddSegment(DefaultContext.DepthToBar(45000), nx25, 31 * 60, DiveMode.OC, 0);
        state.AddSegment(DefaultContext.DepthToBar(30000), nx25, 6 * 60, DiveMode.OC, 0);

        // First ceiling call to establish GfLowPressureThisDive
        var ceilingMm = state.CeilingMm(0.50, 0.70, DefaultContext);
        Assert.True(ceilingMm >= 15000 && ceilingMm <= 15100,
            $"Expected ceiling around 15m, got {ceilingMm}mm");

        // Act — how long to wait at 15m breathing NX25 before clearing to ascend to 12m?
        var clock = 46 * 60; // 46 min into dive (9 + 31 + 6)
        var result = WaitUntil.FindClearTime(
            ref state, clock, clock, 60 * 2 + 1, 60,
            15000, 12000, 45000, nx25, DiveMode.OC, 0,
            0.50, 0.70, DefaultAscentSettings, DefaultContext);

        var waitTimeSec = result - clock;

        Assert.Equal(300, waitTimeSec);
    }
}