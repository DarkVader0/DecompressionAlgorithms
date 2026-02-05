using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;

namespace Buhlmann.Zhl16c.Tests.Unit;

public class DiveContextTests
{
    [Fact]
    public void DepthToMbar_ShouldReturnSurfacePressure_WhenDepthIsZero()
    {
        // Arrange
        var ctx = DiveContext.Default;

        // Act
        var result = ctx.DepthToMbar(0);

        // Assert
        Assert.Equal(GasConstants.StandardPressureMbar, result);
    }

    [Fact]
    public void DepthToBar_ShouldEqualDepthToMbarDividedByThousand()
    {
        // Arrange
        var ctx = DiveContext.Default;
        const uint depthMm = 350000;

        // Act
        var mbar = ctx.DepthToMbar(depthMm);
        var bar = ctx.DepthToBar(depthMm);

        // Assert
        Assert.Equal(mbar / 1000.0, bar, 12);
    }

    [Fact]
    public void DepthToMbar_And_MbarToDepthMm_ShouldBeApproximatelyInverse()
    {
        // Arrange
        var ctx = DiveContext.Default;
        const uint depthMm = 350000;

        // Act
        var mbar = ctx.DepthToMbar(depthMm);
        var depthBackMm = ctx.MbarToDepthMm(mbar);

        // Assert
        Assert.InRange(depthBackMm, (long)depthMm - 50, (long)depthMm + 50);
    }

    [Fact]
    public void RelMbarToDepthMm_ShouldConvertRelativePressureToDepth()
    {
        // Arrange
        var ctx = new DiveContext(GasConstants.StandardPressureMbar, WaterType.Salt);

        var specificWeight = GasConstants.SaltWaterSalinity * 0.981 / 100000.0;
        const uint depthMm = 20000;
        var relMbar = (uint)(specificWeight * depthMm);

        // Act
        var resultMm = ctx.RelMbarToDepthMm(relMbar);

        // Assert
        Assert.InRange(resultMm, (long)depthMm - 50, (long)depthMm + 50);
    }

    [Fact]
    public void PO2Mbar_ShouldEqualAmbientMbarTimesO2Fraction()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var ean50 = new GasMix(500, 0);
        const uint depthMm = 21000;

        // Act
        var ambientMbar = ctx.DepthToMbar(depthMm);
        var po2 = ctx.PO2Mbar(ean50, depthMm);

        // Assert
        Assert.Equal(ambientMbar * 500u / 1000u, po2);
    }

    [Fact]
    public void PN2Mbar_ShouldEqualAmbientMbarTimesN2Fraction()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var air = new GasMix(210, 0);
        const uint depthMm = 10000;

        // Act
        var ambientMbar = ctx.DepthToMbar(depthMm);
        var pn2 = ctx.PN2Mbar(air, depthMm);

        // Assert
        Assert.Equal(ambientMbar * air.N2Permille / 1000u, pn2);
    }

    [Fact]
    public void PHeMbar_ShouldEqualAmbientMbarTimesHeFraction()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var trimix = new GasMix(180, 450);
        const uint depthMm = 30000;

        // Act
        var ambientMbar = ctx.DepthToMbar(depthMm);
        var phe = ctx.PHeMbar(trimix, depthMm);

        // Assert
        Assert.Equal(ambientMbar * 450u / 1000u, phe);
    }

    [Fact]
    public void GasModMmSimple_ShouldReturn60m_WhenTx2021()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var mix = new GasMix(200, 210);
        const uint roundToMm = 1000;

        // Act
        var mod14 = ctx.GasModMmSimple(mix, 1400, roundToMm);

        // Assert
        Assert.Equal(60000u, mod14);
    }

    [Fact]
    public void GasModMmSimple_ShouldReturn21m_WhenNx50()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var mix = new GasMix(500, 0);
        const uint roundToMm = 1000;

        // Act
        var mod14 = ctx.GasModMmSimple(mix, 1600, roundToMm);

        // Assert
        Assert.Equal(21000u, mod14);
    }

    [Fact]
    public void GasModMm_ShouldIncrease_WhenPo2LimitIncreases()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var ean32 = new GasMix(320, 0);
        const uint roundToMm = 1000;

        // Act
        var mod14 = ctx.GasModMm(ean32, 1400, roundToMm);
        var mod16 = ctx.GasModMm(ean32, 1600, roundToMm);

        // Assert
        Assert.True(mod16 >= mod14);
        Assert.Equal(0u, mod14 % roundToMm);
        Assert.Equal(0u, mod16 % roundToMm);
    }

    [Fact]
    public void GasModMm_ShouldBeApproximatelyZero_ForPureOxygenAtOneBarPo2Limit()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var o2 = new GasMix(1000, 0);
        const uint roundToMm = 1000;

        // Act
        var mod = ctx.GasModMm(o2, 1000, roundToMm);

        // Assert
        Assert.InRange(mod, 0u, 1000u);
    }

    [Fact]
    public void EndMm_ShouldEqualDepth_WhenGasMatchesAir_N2OnlyNarcotic()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var air = new GasMix(210, 0);
        const uint depthMm = 30000;

        // Act
        var endMm = ctx.EndMm(air, depthMm, false);

        // Assert
        Assert.InRange(endMm, depthMm, (long)depthMm + 450);
    }

    [Fact]
    public void EndMm_ShouldBeShallower_WhenHeliumIncreases_N2OnlyNarcotic()
    {
        // Arrange
        var ctx = DiveContext.Default;
        const uint depthMm = 60000;

        var tx2135 = new GasMix(210, 350);
        var tx2150 = new GasMix(210, 500);

        // Act
        var end35 = ctx.EndMm(tx2135, depthMm, false);
        var end50 = ctx.EndMm(tx2150, depthMm, false);

        // Assert
        Assert.True(end50 < end35);
    }

    [Fact]
    public void MbarToDepthMm_ShouldReturnZero_WhenGivenSurfacePressure()
    {
        // Arrange
        var ctx = DiveContext.Default;

        // Act
        var depthMm = ctx.MbarToDepthMm(GasConstants.StandardPressureMbar);

        // Assert
        Assert.Equal(0u, depthMm);
    }

    [Fact]
    public void GasMndMm_ShouldBeCloseToEndDepth_ForAir_WhenO2IsNotNarcotic()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var air = new GasMix(209, 0);

        const uint endMm = 30000;
        const int roundToMm = 1000;

        // Act
        var mndMm = ctx.GasMndMm(air, endMm, false, roundToMm);

        // Assert
        Assert.InRange((long)mndMm, 29000, 31000);
        Assert.Equal(0u, mndMm % roundToMm);
    }

    [Fact]
    public void GasMndMm_ShouldIncrease_WhenHeliumIncreases_N2OnlyNarcotic()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var tx2135 = new GasMix(209, 350);
        var tx2150 = new GasMix(209, 500);

        const uint endMm = 30000;
        const int roundToMm = 1000;

        // Act
        var mnd35 = ctx.GasMndMm(tx2135, endMm, false, roundToMm);
        var mnd50 = ctx.GasMndMm(tx2150, endMm, false, roundToMm);

        // Assert
        Assert.True(mnd50 > mnd35);
        Assert.Equal(0u, mnd35 % roundToMm);
        Assert.Equal(0u, mnd50 % roundToMm);
    }

    [Fact]
    public void GasMndMm_ShouldNotChangeWithHelium_WhenO2IsNarcotic_CurrentImplementation()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var tx1835 = new GasMix(180, 350);
        var tx1880 = new GasMix(180, 800);

        const uint endMm = 30000;
        const int roundToMm = 1000;

        // Act
        var mnd35 = ctx.GasMndMm(tx1835, endMm, true, roundToMm);
        var mnd80 = ctx.GasMndMm(tx1880, endMm, true, roundToMm);

        // Assert
        Assert.Equal(mnd35, mnd80);
    }

    [Fact]
    public void GasMndMm_ShouldReturnRoundedDepth_MatchingRequestedGranularity()
    {
        // Arrange
        var ctx = DiveContext.Default;
        var ean32 = new GasMix(320, 0);

        const uint endMm = 27000;
        const int roundToMm = 3000;

        // Act
        var mnd = ctx.GasMndMm(ean32, endMm, false, roundToMm);

        // Assert
        Assert.Equal(0u, mnd % roundToMm);
    }

    [Fact]
    public void GasMndMm_ShouldReturnVeryDeepDepth_WhenMixHasZeroNitrogen_AndO2IsNotNarcotic()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var heliox = new GasMix(200, 800);

        const uint endMm = 30000;
        const int roundToMm = 1000;

        // Act
        var mnd = ctx.GasMndMm(heliox, endMm, false, roundToMm);

        // Assert
        Assert.True(mnd > 1000000);
        Assert.Equal(0u, mnd % roundToMm);
    }
}