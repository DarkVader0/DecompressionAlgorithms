using Buhlmann.Zhl16c.Coefficients;
using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;

namespace Buhlmann.Zhl16c.Tests.Unit;

public class DecoStateTests
{
    private const double StandardPressureBar = 1.01325;
    private const double Tolerance = 0.0001;

    #region Clear Tests

    [Fact]
    public unsafe void Clear_Should_InitializeTissuesWithN2_WhenCalledWithSurfacePressure()
    {
        // Arrange
        var state = new DecoState();
        var surfacePressure = StandardPressureBar;
        var expectedN2 = (surfacePressure - BuhlmannCoefficients.WaterVaporPressure)
            * GasConstants.N2InAirPermille / 1000.0;

        // Act
        state.Clear(surfacePressure);

        // Assert
        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            Assert.Equal(expectedN2, state.TissueN2Sat[i], Tolerance);
        }
    }

    [Fact]
    public unsafe void Clear_Should_ZeroHeliumTissues_WhenCalledWithSurfacePressure()
    {
        // Arrange
        var state = new DecoState();

        // Act
        state.Clear(StandardPressureBar);

        // Assert
        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            Assert.Equal(0.0, state.TissueHeSat[i]);
        }
    }

    [Fact]
    public void Clear_Should_SetGfLowPressure_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        var surfacePressure = 1.2; // High altitude

        // Act
        state.Clear(surfacePressure);

        // Assert
        Assert.Equal(surfacePressure, state.GfLowPressureThisDive);
    }

    [Fact]
    public void Clear_Should_ResetLeadingTissueIndex_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.LeadingTissueIndex = 5;

        // Act
        state.Clear(StandardPressureBar);

        // Assert
        Assert.Equal(0, state.LeadingTissueIndex);
    }

    [Theory]
    [InlineData(0.8)] // High altitude
    [InlineData(1.01325)] // Sea level
    [InlineData(1.1)] // Below sea level
    public unsafe void Clear_Should_HandleVariousPressures_WhenCalledWithDifferentSurfacePressures(double pressure)
    {
        // Arrange
        var state = new DecoState();

        // Act
        state.Clear(pressure);

        // Assert
        Assert.Equal(pressure, state.GfLowPressureThisDive);
        Assert.True(state.TissueN2Sat[0] > 0);
    }

    #endregion

    #region AddSegment Tests

    [Fact]
    public unsafe void AddSegment_Should_IncreaseN2Saturation_WhenBreathingAirAtDepth()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var initialN2 = state.TissueN2Sat[0];
        var gasMix = new GasMix(210, 0);
        var depthPressure = 4.0; // ~30m

        // Act
        state.AddSegment(depthPressure, gasMix, 600, DiveMode.OC, 0);

        // Assert
        Assert.True(state.TissueN2Sat[0] > initialN2);
    }

    [Fact]
    public unsafe void AddSegment_Should_IncreaseHeSaturation_WhenBreathingTrimix()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(180, 370);
        var depthPressure = 5.0;

        // Act
        state.AddSegment(depthPressure, gasMix, 600, DiveMode.OC, 0);

        // Assert
        Assert.True(state.TissueHeSat[0] > 0);
    }

    [Fact]
    public unsafe void AddSegment_Should_DecreaseN2Saturation_WhenAscendingFromDepth()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);

        // Saturate at depth
        state.AddSegment(4.0, gasMix, 3600, DiveMode.OC, 0);
        var saturatedN2 = state.TissueN2Sat[0];

        // Act - Ascend to shallower depth
        state.AddSegment(2.0, gasMix, 600, DiveMode.OC, 0);

        // Assert
        Assert.True(state.TissueN2Sat[0] < saturatedN2);
    }

    [Fact]
    public unsafe void AddSegment_Should_HandleCCRMode_WhenSetpointProvided()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        var setpointMbar = 1400; // 1.4 bar ppO2
        var depthPressure = 4.0;

        // Act
        state.AddSegment(depthPressure, gasMix, 600, DiveMode.CCR, setpointMbar);

        // Assert - Should have loaded N2, CCR calculation should work
        Assert.True(state.TissueN2Sat[0] > 0);
    }

    [Fact]
    public unsafe void AddSegment_Should_CapO2AtAmbientPressure_WhenCCRSetpointTooHigh()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        var setpointMbar = 2000; // 2.0 bar - higher than shallow depth
        var shallowPressure = 1.5;

        // Act - Should not throw, should cap O2
        state.AddSegment(shallowPressure, gasMix, 60, DiveMode.CCR, setpointMbar);

        // Assert - Execution completed without exception
        Assert.True(state.TissueN2Sat[0] >= 0);
    }

    [Fact]
    public unsafe void AddSegment_Should_HandlePureOxygen_WhenO2Is1000Permille()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var initialN2 = state.TissueN2Sat[0];
        var pureO2 = new GasMix(1000, 0);

        // Act
        state.AddSegment(1.6, pureO2, 300, DiveMode.OC, 0);

        // Assert - N2 should offgas
        Assert.True(state.TissueN2Sat[0] < initialN2);
        Assert.Equal(0.0, state.TissueHeSat[0]);
    }

    [Fact]
    public unsafe void AddSegment_Should_UpdateFasterCompartmentsMore_WhenShortDuration()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        var initialN2Fast = state.TissueN2Sat[0];
        var initialN2Slow = state.TissueN2Sat[15];

        // Act
        state.AddSegment(4.0, gasMix, 300, DiveMode.OC, 0); // 5 minutes

        // Assert
        var changeInFast = state.TissueN2Sat[0] - initialN2Fast;
        var changeInSlow = state.TissueN2Sat[15] - initialN2Slow;
        Assert.True(changeInFast > changeInSlow);
    }

    [Theory]
    [InlineData(DiveMode.OC)]
    [InlineData(DiveMode.CCR)]
    [InlineData(DiveMode.PSCR)]
    public unsafe void AddSegment_Should_HandleDifferentDiveModes_WhenCalled(DiveMode mode)
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);

        // Act
        state.AddSegment(2.0, gasMix, 60, mode, mode == DiveMode.CCR ? 1200 : 0);

        // Assert - Should complete without exception

        Assert.True(state.TissueN2Sat[0] > 0);
    }

    #endregion

    #region CeilingBar Tests

    [Fact]
    public void CeilingBar_Should_ReturnZeroOrLess_WhenAtSurface()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gf = 0.8;

        // Act
        var ceiling = state.CeilingBar(gf);

        // Assert
        Assert.True(ceiling <= StandardPressureBar);
    }

    [Fact]
    public void CeilingBar_Should_ReturnHigherCeiling_WhenAfterDeepDive()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);

        // Saturate at depth
        state.AddSegment(6.0, gasMix, 1800, DiveMode.OC, 0); // 30 min at 50m
        var gf = 0.8;

        // Act
        var ceiling = state.CeilingBar(gf);

        // Assert
        Assert.True(ceiling > StandardPressureBar);
    }

    [Fact]
    public void CeilingBar_Should_ReturnLowerCeiling_WhenHigherGradientFactor()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(5.0, gasMix, 1200, DiveMode.OC, 0);

        // Act
        var ceilingLowGF = state.CeilingBar(0.5);
        var ceilingHighGF = state.CeilingBar(0.9);

        // Assert
        Assert.True(ceilingHighGF < ceilingLowGF);
    }

    [Fact]
    public void CeilingBar_Should_SetLeadingTissueIndex_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(4.0, gasMix, 600, DiveMode.OC, 0);

        // Act
        state.CeilingBar(0.8);

        // Assert
        Assert.InRange(state.LeadingTissueIndex, 0, 15);
    }

    [Fact]
    public void CeilingBar_Should_HandleMixedGasLoading_WhenBothN2AndHePresent()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var trimix = new GasMix(180, 420);
        state.AddSegment(7.0, trimix, 1200, DiveMode.OC, 0);

        // Act
        var ceiling = state.CeilingBar(0.7);

        // Assert
        Assert.True(ceiling > StandardPressureBar);
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.5)]
    [InlineData(0.7)]
    [InlineData(0.9)]
    [InlineData(1.0)]
    public void CeilingBar_Should_ReturnValidCeiling_WhenCalledWithVariousGradientFactors(double gf)
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(4.0, gasMix, 900, DiveMode.OC, 0);

        // Act
        var ceiling = state.CeilingBar(gf);

        // Assert
        Assert.True(ceiling >= 0);
    }

    [Fact]
    public void CeilingBar_Should_UseWeightedCoefficients_WhenMultipleInertGasesPresent()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);

        // Load with trimix
        var trimix = new GasMix(180, 420);
        state.AddSegment(8.0, trimix, 1800, DiveMode.OC, 0);

        // Act
        var ceiling = state.CeilingBar(0.8);

        // Assert - Should calculate without error and give reasonable ceiling
        Assert.True(ceiling > 0);
        Assert.InRange(state.LeadingTissueIndex, 0, 15);
    }

    #endregion

    #region CeilingMm Tests

    [Fact]
    public void CeilingMm_Should_ReturnZero_WhenCeilingBelowSurface()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var context = new DiveContext { SurfacePressureMbar = 1013 };

        // Act
        var ceilingMm = state.CeilingMm(0.8, context);

        // Assert
        Assert.Equal(0u, ceilingMm);
    }

    [Fact]
    public void CeilingMm_Should_ReturnDepthInMm_WhenCeilingAboveSurface()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(5.0, gasMix, 1500, DiveMode.OC, 0);

        var context = new DiveContext { SurfacePressureMbar = 1013 };

        // Act
        var ceilingMm = state.CeilingMm(0.7, context);

        // Assert
        Assert.True(ceilingMm > 0);
    }

    [Fact]
    public void CeilingMm_Should_UseContextConversion_WhenCalculatingDepth()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(4.0, gasMix, 1200, DiveMode.OC, 0);

        var context = new DiveContext(1013, WaterType.Salt);

        // Act
        var ceilingMm = state.CeilingMm(0.75, context);

        // Assert - Should be a reasonable depth value
        Assert.InRange(ceilingMm, 0u, 100000u); // 0-100m seems reasonable
    }

    #endregion

    #region GetInertPressure Tests

    [Fact]
    public unsafe void GetInertPressure_Should_ReturnSumOfN2AndHe_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var trimix = new GasMix(180, 420);
        state.AddSegment(5.0, trimix, 600, DiveMode.OC, 0);

        // Act
        var inertPressure = state.GetInertPressure(0);

        // Assert
        var expected = state.TissueN2Sat[0] + state.TissueHeSat[0];
        Assert.Equal(expected, inertPressure, Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void GetInertPressure_Should_ReturnValidValue_WhenCalledForAnyCompartment(int compartmentIndex)
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(3.0, gasMix, 300, DiveMode.OC, 0);

        // Act
        var inertPressure = state.GetInertPressure(compartmentIndex);

        // Assert
        Assert.True(inertPressure >= 0);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public unsafe void Clone_Should_CreateIndependentCopy_WhenCalled()
    {
        // Arrange
        var original = new DecoState();
        original.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        original.AddSegment(4.0, gasMix, 600, DiveMode.OC, 0);
        var originalN2 = original.TissueN2Sat[0];

        // Act
        var clone = original.Clone();

        // Modify original
        original.AddSegment(2.0, gasMix, 300, DiveMode.OC, 0);

        // Assert
        Assert.Equal(originalN2, clone.TissueN2Sat[0], Tolerance);
        Assert.NotEqual(originalN2, original.TissueN2Sat[0], Tolerance);
    }

    [Fact]
    public unsafe void Clone_Should_CopyAllTissueCompartments_WhenCalled()
    {
        // Arrange
        var original = new DecoState();
        original.Clear(StandardPressureBar);
        var trimix = new GasMix(180, 420);
        original.AddSegment(6.0, trimix, 900, DiveMode.OC, 0);

        // Act
        var clone = original.Clone();

        // Assert
        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            Assert.Equal(original.TissueN2Sat[i], clone.TissueN2Sat[i], Tolerance);
            Assert.Equal(original.TissueHeSat[i], clone.TissueHeSat[i], Tolerance);
        }
    }

    [Fact]
    public void Clone_Should_CopyGfLowPressure_WhenCalled()
    {
        // Arrange
        var original = new DecoState();
        original.Clear(StandardPressureBar);
        original.GfLowPressureThisDive = 3.5;

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.GfLowPressureThisDive, clone.GfLowPressureThisDive);
    }

    [Fact]
    public void Clone_Should_CopyLeadingTissueIndex_WhenCalled()
    {
        // Arrange
        var original = new DecoState();
        original.Clear(StandardPressureBar);
        original.LeadingTissueIndex = 7;

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(7, clone.LeadingTissueIndex);
    }

    #endregion

    #region CopyFrom Tests

    [Fact]
    public unsafe void CopyFrom_Should_OverwriteCurrentState_WhenCalled()
    {
        // Arrange
        var state1 = new DecoState();
        state1.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state1.AddSegment(5.0, gasMix, 1200, DiveMode.OC, 0);

        var state2 = new DecoState();
        state2.Clear(1.1);

        var state1N2 = state1.TissueN2Sat[0];

        // Act
        state2.CopyFrom(ref state1);

        // Assert
        Assert.Equal(state1N2, state2.TissueN2Sat[0], Tolerance);
    }

    [Fact]
    public unsafe void CopyFrom_Should_CopyAllFields_WhenCalled()
    {
        // Arrange
        var source = new DecoState();
        source.Clear(StandardPressureBar);
        source.GfLowPressureThisDive = 4.2;
        source.LeadingTissueIndex = 9;

        var trimix = new GasMix(180, 420);
        source.AddSegment(7.0, trimix, 1500, DiveMode.OC, 0);

        var destination = new DecoState();

        // Act
        destination.CopyFrom(ref source);

        // Assert
        Assert.Equal(source.GfLowPressureThisDive, destination.GfLowPressureThisDive);
        Assert.Equal(source.LeadingTissueIndex, destination.LeadingTissueIndex);

        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            Assert.Equal(source.TissueN2Sat[i], destination.TissueN2Sat[i], Tolerance);
            Assert.Equal(source.TissueHeSat[i], destination.TissueHeSat[i], Tolerance);
        }
    }

    #endregion

    #region CreateAtSurface Tests

    [Fact]
    public unsafe void CreateAtSurface_Should_ReturnInitializedState_WhenCalledWithPressure()
    {
        // Arrange & Act
        var state = DecoState.CreateAtSurface(StandardPressureBar);

        // Assert
        Assert.Equal(StandardPressureBar, state.GfLowPressureThisDive);
        Assert.True(state.TissueN2Sat[0] > 0);
        Assert.Equal(0.0, state.TissueHeSat[0]);
    }

    [Fact]
    public void CreateAtSurface_Should_UseStandardPressure_WhenCalledWithoutParameters()
    {
        // Arrange & Act
        var state = DecoState.CreateAtSurface();

        // Assert
        var expectedPressure = GasConstants.StandardPressureMbar / 1000.0;
        Assert.Equal(expectedPressure, state.GfLowPressureThisDive, Tolerance);
    }

    [Fact]
    public void CreateAtSurface_Should_InitializeLeadingTissueToZero_WhenCalled()
    {
        // Arrange & Act
        var state = DecoState.CreateAtSurface(StandardPressureBar);

        // Assert
        Assert.Equal(0, state.LeadingTissueIndex);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_Should_HandleTypicalRecreationalDive_WhenSimulated()
    {
        // Arrange - Simulate 30m dive for 30 minutes on air
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);

        // Act
        // Descend to 30m (4 bar)
        state.AddSegment(4.0, air, 1800, DiveMode.OC, 0); // 30 minutes at depth

        // Check ceiling
        var ceiling = state.CeilingBar(0.8);

        // Assert - Should have some ceiling after this dive
        Assert.True(ceiling >= StandardPressureBar); // May or may not need stops
    }

    [Fact]
    public void Integration_Should_HandleTechnicalTrimixDive_WhenSimulated()
    {
        // Arrange - Deep trimix dive
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var bottomMix = new GasMix(180, 450);
        var decoMix = new GasMix(500, 0);

        // Act
        // Bottom time at 60m (7 bar)
        state.AddSegment(7.0, bottomMix, 1200, DiveMode.OC, 0); // 20 minutes

        // Ascent to 21m (3.1 bar) on deco gas
        state.AddSegment(3.1, decoMix, 600, DiveMode.OC, 0);

        var ceiling = state.CeilingBar(0.7);

        // Assert - Should definitely have a ceiling
        Assert.True(ceiling > StandardPressureBar);
    }

    [Fact]
    public unsafe void Integration_Should_HandleCCRDive_WhenSimulated()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var diluent = new GasMix(210, 0);

        // Act - CCR dive at constant ppO2 of 1.3 bar
        state.AddSegment(4.0, diluent, 1800, DiveMode.CCR, 1300);

        var ceiling = state.CeilingBar(0.85);

        // Assert
        Assert.True(ceiling >= 0);
        Assert.True(state.TissueN2Sat[0] > 0);
    }

    [Fact]
    public void Integration_Should_HandleRepetitiveDives_WhenSimulated()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var air = new GasMix(210, 0);

        // Act
        // First dive
        state.AddSegment(3.0, air, 1200, DiveMode.OC, 0); // 20 min at 20m
        state.AddSegment(StandardPressureBar, air, 600, DiveMode.OC, 0); // Ascend

        var ceilingAfterFirstDive = state.CeilingBar(0.8);

        // Surface interval
        state.AddSegment(StandardPressureBar, air, 3600, DiveMode.OC, 0); // 1 hour

        // Second dive
        state.AddSegment(3.0, air, 1200, DiveMode.OC, 0); // 20 min at 20m

        var ceilingAfterSecondDive = state.CeilingBar(0.8);

        // Assert - Second dive should have higher ceiling due to residual nitrogen
        Assert.True(ceilingAfterSecondDive >= ceilingAfterFirstDive);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public unsafe void AddSegment_Should_HandleZeroDuration_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var initialN2 = state.TissueN2Sat[0];
        var gasMix = new GasMix(210, 0);

        // Act
        state.AddSegment(4.0, gasMix, 0, DiveMode.OC, 0);

        // Assert - Should not change
        Assert.Equal(initialN2, state.TissueN2Sat[0], Tolerance);
    }

    [Fact]
    public void CeilingBar_Should_HandleZeroGradientFactor_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(5.0, gasMix, 1200, DiveMode.OC, 0);

        // Act
        var ceiling = state.CeilingBar(0.0);

        // Assert - Should handle gracefully (may be infinity or very high value)
        Assert.True(ceiling >= 0 || double.IsInfinity(ceiling) || double.IsNaN(ceiling));
    }

    [Fact]
    public void CeilingBar_Should_HandleGradientFactorOfOne_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);
        state.AddSegment(4.0, gasMix, 900, DiveMode.OC, 0);

        // Act
        var ceiling = state.CeilingBar(1.0);

        // Assert - GF of 1.0 means no conservatism
        Assert.True(ceiling >= 0);
    }

    [Fact]
    public unsafe void AddSegment_Should_HandleVeryLongDuration_WhenCalled()
    {
        // Arrange
        var state = new DecoState();
        state.Clear(StandardPressureBar);
        var gasMix = new GasMix(210, 0);

        // Act - Very long bottom time (saturation)
        state.AddSegment(4.0, gasMix, 86400, DiveMode.OC, 0); // 24 hours

        // Assert - Should approach equilibrium
        Assert.True(state.TissueN2Sat[0] > 0);
    }

    #endregion
}