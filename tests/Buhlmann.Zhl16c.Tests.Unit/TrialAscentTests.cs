using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class TrialAscentTests
{
    private const double StandardPressureBar = 1.013;

    private static readonly DiveContext DefaultContext =
        new(1013, WaterType.Salt);

    private static readonly AscentDescentSettings DefaultAscentSettings = new()
    {
        AscentRate75MmSec = 5 * 1000 / 60,
        AscentRate50MmSec = 5 * 1000 / 60,
        AscentRateStopsMmSec = 5 * 1000 / 60,
        AscentRateLast6mMmSec = 1 * 1000 / 60
    };

    private static DecoState CreateSaturatedState(double pressureBar,
        GasMix gasMix,
        int durationSec)
    {
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        state.AddSegment(pressureBar, gasMix, durationSec, DiveMode.OC, 0);
        return state;
    }

    [Fact]
    public void IsClearToAscend_ShouldReturnTrue_WhenShallowDiveWithNoDecoObligation()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state = CreateSaturatedState(2.0, air, 600); // 10m for 10min

        // Act
        var result = TrialAscent.IsClearToAscend(
            ref state, 10000, 0, 10000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsClearToAscend_ShouldReturnFalse_WhenDeepDiveTriesToAscendDirectlyToSurface()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state = CreateSaturatedState(7.0, air, 1800); // 60m for 30min

        // Act
        var result = TrialAscent.IsClearToAscend(
            ref state, 60000, 0, 60000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsClearToAscend_ShouldReturnTrue_WhenAscendingOnlyOneStop()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state = CreateSaturatedState(4.0, air, 60 * 15);

        state.AddSegment(2.8, air, (int)(4.8 * 60), DiveMode.OC, 0);

        // Act — try ascending from 6m to 3m (one stop)
        var result = TrialAscent.IsClearToAscend(
            ref state, 6000, 3000, 30000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public unsafe void IsClearToAscend_ShouldNotModifyDecoState_WhenCalled()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state = CreateSaturatedState(5.0, air, 1500); // 40m for 25min
        var before = state.Clone();

        // Act
        TrialAscent.IsClearToAscend(
            ref state, 40000, 0, 40000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(before.TissueN2Sat[i], state.TissueN2Sat[i]);
            Assert.Equal(before.TissueHeSat[i], state.TissueHeSat[i]);
        }

        Assert.Equal(before.GfLowPressureThisDive, state.GfLowPressureThisDive);
    }

    [Fact]
    public void IsClearToAscend_ShouldBeMorePermissive_WhenGfIsHigher()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state1 = CreateSaturatedState(5.0, air, 1500);
        var state2 = CreateSaturatedState(5.0, air, 1500);

        // Act
        var conservativeResult = TrialAscent.IsClearToAscend(
            ref state1, 40000, 0, 40000, air, DiveMode.OC, 0, 0,
            0.20, 0.50, DefaultAscentSettings, DefaultContext);

        var aggressiveResult = TrialAscent.IsClearToAscend(
            ref state2, 40000, 0, 40000, air, DiveMode.OC, 0, 0,
            0.90, 0.95, DefaultAscentSettings, DefaultContext);

        // Assert — aggressive should be at least as permissive
        if (!conservativeResult)
        {
            // If conservative fails, aggressive might still pass (that's fine)
            // but if conservative passes, aggressive must also pass
        }
        else
        {
            Assert.True(aggressiveResult);
        }
    }

    [Fact]
    public void IsClearToAscend_ShouldHandleWaitTime_WhenWaitTimeIsProvided()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var stateNoWait = CreateSaturatedState(5.0, air, 1500);
        var stateWithWait = CreateSaturatedState(5.0, air, 1500);

        // Act
        var resultNoWait = TrialAscent.IsClearToAscend(
            ref stateNoWait, 12000, 9000, 40000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        var resultWithWait = TrialAscent.IsClearToAscend(
            ref stateWithWait, 12000, 9000, 40000, air, DiveMode.OC, 0, 600,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert — waiting at a stop should help off-gas, so at least as likely to clear
        if (resultNoWait)
        {
            Assert.True(resultWithWait);
        }
    }

    [Fact]
    public void IsClearToAscend_ShouldHandleTrimix_WhenHePresent()
    {
        // Arrange
        var trimix = new GasMix(180, 450);
        var state = CreateSaturatedState(7.0, trimix, 1200); // 60m for 20min

        // Act
        var result = TrialAscent.IsClearToAscend(
            ref state, 60000, 0, 60000, trimix, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert — deep trimix should definitely require stops
        Assert.False(result);
    }

    [Fact]
    public void IsClearToAscend_ShouldReturnTrue_WhenAlreadyAtStopLevel()
    {
        // Arrange
        var air = new GasMix(210, 0);
        var state = CreateSaturatedState(4.0, air, 1200);

        // Act — trialDepth == stopLevel, loop doesn't execute
        var result = TrialAscent.IsClearToAscend(
            ref state, 6000, 6000, 30000, air, DiveMode.OC, 0, 0,
            0.35, 0.75, DefaultAscentSettings, DefaultContext);

        // Assert
        Assert.True(result);
    }
}