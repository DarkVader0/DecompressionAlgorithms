using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;

namespace Buhlmann.Zhl16c.Tests.Unit;

public class GasSelectorTests
{
    private static DiveContext DefaultContext => DiveContext.Default;

    #region BuildGasChangeList Tests

    [Fact]
    public void BuildGasChangeList_ShouldReturnZero_WhenCylindersArrayIsEmpty()
    {
        // Arrange
        var cylinders = ReadOnlySpan<Cylinder>.Empty;
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 60000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(0u, count);
    }

    [Fact]
    public void BuildGasChangeList_ShouldReturnZero_WhenThereIsNoDecoCylinder()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 350, Use = CylinderUse.Bottom },
            new() { O2Permille = 200, HePermille = 210, Use = CylinderUse.Diluent }
        ];
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 60000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(0u, count);
    }

    [Fact]
    public void BuildGasChangeList_ShouldReturnOneChange_WhenThereIsSingleDecoGas()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco }
        ];
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 60000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(1u, count);
        Assert.Equal(1, gasChanges[0].CylinderIndex);
        Assert.Equal(21000u, gasChanges[0].DepthMm);
    }

    [Fact]
    public void BuildGasChangeList_ShouldBeSortedByDescending_WhenThereAreMultipleDecoGases()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 210, HePermille = 350, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
        ];
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 60000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(2u, count);
        Assert.Equal(21000u, gasChanges[0].DepthMm);
        Assert.Equal(2, gasChanges[0].CylinderIndex);
        Assert.Equal(6000u, gasChanges[1].DepthMm);
        Assert.Equal(0, gasChanges[1].CylinderIndex);
    }

    [Fact]
    public void BuildGasChangeList_ShouldBeExcluded_WhenDecoGasModIsAboveMaxDepth()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco }
        ];
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 20000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(0u, count);
    }

    [Fact]
    public void BuildGasChangeList_ShouldBeInCorrectOrder_WhenMultipleDecoGases()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 120, HePermille = 650, Use = CylinderUse.Bottom },
            new() { O2Permille = 210, HePermille = 350, Use = CylinderUse.Deco },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var count = GasSelector.BuildGasChangeList(cylinders, 100000, 1600, DefaultContext, gasChanges);

        // Assert
        Assert.Equal(3u, count);
        Assert.True(gasChanges[0].DepthMm > gasChanges[1].DepthMm);
        Assert.True(gasChanges[1].DepthMm > gasChanges[2].DepthMm);
        Assert.Equal(6000u, gasChanges[2].DepthMm);
    }

    #endregion

    #region FindBottomGas Tests

    [Fact]
    public void FindBottomGas_ShouldReturnIndex_WithBottomCylinder()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 210, HePermille = 350, Use = CylinderUse.Bottom },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBottomGas(cylinders);

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindBottomGas_ShouldReturnZero_WhenNoBottomCylinder()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBottomGas(cylinders);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindBottomGas_ShouldReturnNegative_WhenNoCylindersProvided()
    {
        // Arrange
        var cylinders = ReadOnlySpan<Cylinder>.Empty;

        // Act
        var index = GasSelector.FindBottomGas(cylinders);

        // Assert
        Assert.Equal(-1, index);
    }

    #endregion

    #region FindBestAscentGas Tests

    [Fact]
    public void FindBestAscentGas_ShouldReturnHighestO2_WhenAt3m()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Oxygen }
        ];

        // Act
        var index = GasSelector.FindBestAscentGas(cylinders, 3000, 1600, DefaultContext);

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void FindBestAscentGas_ShouldReturnCorrectGas_WhenAt30m()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBestAscentGas(cylinders, 30000, 1600, DefaultContext);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindBestAscentGas_ShouldReturnNx50_WhenOn21m()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBestAscentGas(cylinders, 21000, 1600, DefaultContext);

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindBestAscentGas_ShouldReturnNegative_WhenThereIsNoSafeGas()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBestAscentGas(cylinders, 60000, 1600, DefaultContext);

        // Assert
        Assert.Equal(-1, index);
    }

    #endregion

    #region FindNextGasChange Tests

    [Fact]
    public void FindNextGasChange_ShouldReturnNextChange_WhenAtDepth()
    {
        // Arrange
        Span<GasSelector.GasChange> gasChanges =
        [
            new(21000, 1),
            new(6000, 2)
        ];

        // Act
        var index = GasSelector.FindNextGasChange(gasChanges, 2, 25000);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindNextGasChange_ShouldReturnChange_WhenOnExactDepth()
    {
        // Arrange
        Span<GasSelector.GasChange> gasChanges =
        [
            new(21000, 1),
            new(6000, 2)
        ];

        // Act
        var index = GasSelector.FindNextGasChange(gasChanges, 2, 21000);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindNextGasChange_ShouldReturnNegative_WhenAboveAllChanges()
    {
        // Arrange
        Span<GasSelector.GasChange> gasChanges =
        [
            new(21000, 1),
            new(6000, 2)
        ];

        // Act
        var index = GasSelector.FindNextGasChange(gasChanges, 2, 5000);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindNextGasChange_ShouldReturnNegative_WhenProvidedWithEmptyList()
    {
        // Arrange
        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[10];

        // Act
        var index = GasSelector.FindNextGasChange(gasChanges, 0, 30000);

        // Assert
        Assert.Equal(-1, index);
    }

    #endregion

    #region IsGasSafeAtDepth Tests

    [Fact]
    public void IsGasSafeAtDepth_ShouldBeTrue_WhenNx50On21m()
    {
        // Arrange
        var mix = new GasMix(500, 0);

        // Act
        var safe = GasSelector.IsGasSafeAtDepth(mix, 21000, 1600, DefaultContext);

        // Assert
        Assert.True(safe);
    }

    [Fact]
    public void IsGasSafeAtDepth_ShouldBeTrue_WhenTx2021On59m()
    {
        // Arrange
        var mix = new GasMix(200, 210);

        // Act
        var safe = GasSelector.IsGasSafeAtDepth(mix, 59200, 1400, DefaultContext);

        // Assert
        Assert.True(safe);
    }

    [Fact]
    public void IsGasSafeAtDepth_ShouldBeFalse_WhenNx50On30m()
    {
        // Arrange
        var mix = new GasMix(500, 0);

        // Act
        var safe = GasSelector.IsGasSafeAtDepth(mix, 30000, 1600, DefaultContext);

        // Assert
        Assert.False(safe);
    }

    #endregion

    #region IsGasBreathable Tests

    [Fact]
    public void IsGasBreathable_ShouldReturnTrue_WhenAirOnSurface()
    {
        // Arrange
        var mix = new GasMix(210, 0);

        // Act
        var breathable = GasSelector.IsGasBreathable(mix, 0, DefaultContext);

        // Assert
        Assert.True(breathable);
    }

    [Fact]
    public void IsGasBreathable_ShouldReturnFalse_WhenHypoxicTrimixOnSurface()
    {
        // Arrange
        var mix = new GasMix(100, 700);

        // Act
        var breathable = GasSelector.IsGasBreathable(mix, 0, DefaultContext);

        // Assert
        Assert.False(breathable);
    }

    [Fact]
    public void IsGasBreathable_ShouldReturnTrue_WhenHypoxicTrimixOn30m()
    {
        // Arrange
        var mix = new GasMix(100, 700);

        // Act
        var breathable = GasSelector.IsGasBreathable(mix, 30000, DefaultContext);

        // Assert
        Assert.True(breathable);
    }

    #endregion

    #region CompareGasDepth Tests

    [Fact]
    public void CompareGasDepth_LowerO2IsDeeper()
    {
        // Arrange
        var ean32 = new GasMix(320, 0);
        var ean50 = new GasMix(500, 0);

        // Act
        var result = GasSelector.CompareGasDepth(ean32, ean50);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void CompareGasDepth_HigherO2IsShallower()
    {
        // Arrange
        var ean50 = new GasMix(500, 0);
        var ean32 = new GasMix(320, 0);

        // Act
        var result = GasSelector.CompareGasDepth(ean50, ean32);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CompareGasDepth_SameO2_HigherHeIsDeeper()
    {
        // Arrange
        var tx2135 = new GasMix(210, 350);
        var ean21 = new GasMix(210, 0);

        // Act
        var result = GasSelector.CompareGasDepth(tx2135, ean21);

        // Assert
        Assert.Equal(-1, result);
    }
    
    [Fact]
    public void CompareGasDepth_SameO2_HigherHeIsDeeperSwitch()
    {
        // Arrange
        var tx2135 = new GasMix(210, 350);
        var ean21 = new GasMix(210, 0);

        // Act
        var result = GasSelector.CompareGasDepth(ean21, tx2135);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CompareGasDepth_IdenticalGases_ReturnsZero()
    {
        // Arrange
        var gas1 = new GasMix(320, 0);
        var gas2 = new GasMix(320, 0);

        // Act
        var result = GasSelector.CompareGasDepth(gas1, gas2);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region FindBailoutGas Tests

    [Fact]
    public void FindBailoutGas_ShouldReturnDeepestSafeGas_WhenOn60m()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 200, HePermille = 210, Use = CylinderUse.Diluent | CylinderUse.Bailout },
            new() { O2Permille = 180, HePermille = 450, Use = CylinderUse.Bottom },
            new() { O2Permille = 10, HePermille = 900, Use = CylinderUse.Bailout },
            new() { O2Permille = 500, HePermille = 0, Use = CylinderUse.Deco },
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Oxygen }
        ];

        // Act
        var index = GasSelector.FindBailoutGas(cylinders, 60000, 1400, DefaultContext);

        // Assert
        Assert.Equal(1, index);
    }

    
    [Fact]
    public void FindBailoutGas_ShouldExcludeDiluentAndO2()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Oxygen },
            new() { O2Permille = 200, HePermille = 210, Use = CylinderUse.Diluent },
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom }
        ];

        // Act
        var index = GasSelector.FindBailoutGas(cylinders, 20000, 1400, DefaultContext);

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void FindBailoutGas_ShouldPreferHigherO2()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 210, HePermille = 0, Use = CylinderUse.Bottom },
            new() { O2Permille = 320, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBailoutGas(cylinders, 30000, 1400, DefaultContext);

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindBailoutGas_ShouldReturnNegative_WhenThereIsNoSafeGas()
    {
        // Arrange
        Span<Cylinder> cylinders =
        [
            new() { O2Permille = 1000, HePermille = 0, Use = CylinderUse.Deco }
        ];

        // Act
        var index = GasSelector.FindBailoutGas(cylinders, 30000, 1400, DefaultContext);

        // Assert
        Assert.Equal(-1, index);
    }

    #endregion

    #region CheckIcd Tests (Add this method to GasSelector first)

    [Fact]
    public void CheckIcd_ShouldReturnWarning_WhenSwitchingFromTxToNx()
    {
        // Arrange
        var oldMix = new GasMix(180, 450);
        var newMix = new GasMix(500, 0);

        // Act
        var warning = GasSelector.CheckIcd(oldMix, newMix, out var dN2, out var dHe);

        // Assert
        Assert.Equal(130, dN2);
        Assert.Equal(-450, dHe);
        Assert.True(warning);
    }

    [Fact]
    public void CheckIcd_ShouldReturnWarning_WhenChangingFromTxWithLowN2ToTxWithHigherN2()
    {
        // Arrange
        var oldMix = new GasMix(120, 650);
        var newMix = new GasMix(210, 350);

        // Act
        var warning = GasSelector.CheckIcd(oldMix, newMix, out _, out _);

        // Assert
        Assert.True(warning);
    }

    [Fact]
    public void CheckIcd_ShouldReturnNoWarning_WhenChangingNxToNx()
    {
        // Arrange
        var oldMix = new GasMix(320, 0);
        var newMix = new GasMix(500, 0);

        // Act
        var warning = GasSelector.CheckIcd(oldMix, newMix, out _, out _);

        // Assert - no He in old gas, no ICD
        Assert.False(warning);
    }

    [Fact]
    public void CheckIcd_ShouldReturnNoWarning_WhenSwitchingFromHighN2TxToLowN2Tx()
    {
        // Arrange
        var oldMix = new GasMix(210, 350);
        var newMix = new GasMix(180, 450);

        // Act
        var warning = GasSelector.CheckIcd(oldMix, newMix, out var dN2, out _);

        // Assert
        Assert.True(dN2 < 0);
        Assert.False(warning);
    }

    #endregion
}