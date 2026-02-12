using Buhlmann.Zhl16c.Coefficients;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class BuhlmannCoefficientTests
{
    [Fact]
    public unsafe void Zhl16C_ShouldLoadAllTissues_WithExpectedSentinelValues()
    {
        // Arrange
        var c = BuhlmannCoefficients.ZHL16C;

        // Act
        // Read a few “sentinel” cells that prove the fixed buffers were populated correctly.
        var n2Half0 = c.N2HalfLife[0];
        var n2Half15 = c.N2HalfLife[15];

        var n2A0 = c.N2A[0];
        var n2B15 = c.N2B[15];

        var heHalf0 = c.HeHalfLife[0];
        var heHalf15 = c.HeHalfLife[15];

        var heA0 = c.HeA[0];
        var heB15 = c.HeB[15];

        var n2F1s0 = c.N2FactorOneSecond[0];
        var heF1s0 = c.HeFactorOneSecond[0];

        // Assert
        Assert.Equal(16, BuhlmannCoefficients.CompartmentCount);

        Assert.Equal(5.0, n2Half0, 12);
        Assert.Equal(635.0, n2Half15, 12);

        Assert.Equal(1.1696, n2A0, 12);
        Assert.Equal(0.9653, n2B15, 12);

        Assert.Equal(1.88, heHalf0, 12);
        Assert.Equal(240.03, heHalf15, 12);

        Assert.Equal(1.6189, heA0, 12);
        Assert.Equal(0.9267, heB15, 12);

        Assert.Equal(2.30782347297664E-003, n2F1s0, 18.0);
        Assert.Equal(6.12608039419837E-003, heF1s0, 18.0);
    }

    [Fact]
    public unsafe void Factor_ShouldReturnPrecomputedOneSecondFactors_WhenPeriodIsOneSecond()
    {
        // Arrange
        var c = BuhlmannCoefficients.ZHL16C;
        const int ci = 0;

        // Act
        var n2 = c.Factor(1, ci, false);
        var he = c.Factor(1, ci, true);

        // Assert
        Assert.Equal(c.N2FactorOneSecond[ci], n2, 18.0);
        Assert.Equal(c.HeFactorOneSecond[ci], he, 18.0);
    }

    [Fact]
    public unsafe void Factor_ShouldMatchFormula_ForNonOneSecondPeriods()
    {
        // Arrange
        var c = BuhlmannCoefficients.ZHL16C;
        const int ci = 0;
        const int periodSec = 60; // 1 minute

        // Act
        var n2 = c.Factor(periodSec, ci, false);
        var he = c.Factor(periodSec, ci, true);

        // Assert
        // Expected = 1 - exp(-periodSeconds * Ln2Over60 / halfLife)
        var expectedN2 = 1.0 - Math.Exp(-periodSec * BuhlmannCoefficients.Ln2Over60 / c.N2HalfLife[ci]);
        var expectedHe = 1.0 - Math.Exp(-periodSec * BuhlmannCoefficients.Ln2Over60 / c.HeHalfLife[ci]);

        Assert.Equal(expectedN2, n2, 15);
        Assert.Equal(expectedHe, he, 15);
    }

    [Fact]
    public void Factor_ShouldIncrease_WhenPeriodIncreases_ForSameTissueAndGas()
    {
        // Arrange
        var c = BuhlmannCoefficients.ZHL16C;
        const int ci = 5;

        // Act
        var f10 = c.Factor(10, ci, false);
        var f60 = c.Factor(60, ci, false);
        var f300 = c.Factor(300, ci, false);

        // Assert
        Assert.True(f10 > 0);
        Assert.True(f60 > f10);
        Assert.True(f300 > f60);
        Assert.True(f300 < 1.0);
    }

    [Fact]
    public void Factor_ShouldBeHigher_ForFasterTissue_ForSamePeriod()
    {
        // Arrange
        var c = BuhlmannCoefficients.ZHL16C;
        const int periodSec = 60;

        // Fast tissue vs slow tissue
        const int fast = 0; // 5 min N2 half-life
        const int slow = 15; // 635 min N2 half-life

        // Act
        var fastFactor = c.Factor(periodSec, fast, false);
        var slowFactor = c.Factor(periodSec, slow, false);

        // Assert
        Assert.True(fastFactor > slowFactor);
    }
}