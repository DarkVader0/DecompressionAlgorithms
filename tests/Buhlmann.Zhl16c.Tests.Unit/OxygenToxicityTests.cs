using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class OxygenToxicityTests
{
    [Fact]
    public void CnsRatePerSecond_ShouldReturnZero_WhenPo2Is500OrLess()
    {
        Assert.Equal(0.0, OxygenToxicity.CnsRatePerSecond(209));
        Assert.Equal(0.0, OxygenToxicity.CnsRatePerSecond(500));
        Assert.Equal(0.0, OxygenToxicity.CnsRatePerSecond(0));
    }

    [Fact]
    public void CnsRatePerSecond_ShouldUseLowerCurve_WhenPo2IsAtOrBelow1500()
    {
        // Arrange
        var po2Mbar = 1500;
        var expected = Math.Exp(-11.7853 + 0.00193873 * po2Mbar);

        // Act
        var result = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert (tight tolerance; it's a direct formula)
        Assert.InRange(result, expected * 0.999999, expected * 1.000001);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldUseUpperCurve_WhenPo2IsAbove1500()
    {
        // Arrange
        var po2Mbar = 1600;
        var expected = Math.Exp(-23.6349 + 0.00980829 * po2Mbar);

        // Act
        var result = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.InRange(result, expected * 0.999999, expected * 1.000001);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldReturnZero_WhenOnSurface()
    {
        // Arrange
        var po2Mbar = 209;

        // Act
        var result = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateCns_ShouldBeAboutTwentySevenPercent_WhenAt665mbarFor167Minutes()
    {
        // Arrange
        var po2Mbar = 665;
        var durationSec = 167 * 60;

        // Act
        var result = OxygenToxicity.CalculateCns(po2Mbar, durationSec);

        // Assert
        Assert.InRange(result, 27, 28);
    }

    [Fact]
    public void CalculateCns_ShouldBeFourtyPercent_WhenMultiStageDive()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var ean25 = new GasMix(250, 0);
        var ean50 = new GasMix(500, 0);

        // Depths (mm)
        const int d0 = 0;
        const int d6 = 6000;
        const int d9 = 9000;
        const int d12 = 12000;
        const int d15 = 15000;
        const int d21 = 21000;
        const int d48 = 48000;

        var po20Ean25 = ctx.PO2Mbar(ean25, d0);
        var po248Ean25 = ctx.PO2Mbar(ean25, d48);
        var po221Ean25 = ctx.PO2Mbar(ean25, d21);

        var po221Ean50 = ctx.PO2Mbar(ean50, d21);
        var po215Ean50 = ctx.PO2Mbar(ean50, d15);
        var po212Ean50 = ctx.PO2Mbar(ean50, d12);
        var po29Ean50 = ctx.PO2Mbar(ean50, d9);
        var po26Ean50 = ctx.PO2Mbar(ean50, d6);
        var po20Ean50 = ctx.PO2Mbar(ean50, d0);

        var t10 = 10 * 60;
        var t30 = 30 * 60;
        var t6 = 6 * 60;
        var t3 = 3 * 60;
        var t1 = 1 * 60;
        var t2 = 2 * 60;
        var t4 = 4 * 60;
        var t7 = 7 * 60;
        var t39 = 39 * 60;

        // Act
        // Plan:
        // ➘ 48m 10min (EAN25)
        // ➙ 48m 30min (EAN25)
        // ➚ 21m 6min (EAN25)
        // - 21m 3min (EAN50)
        // ➚ 15m 1min, - 15m 2min
        // ➚ 12m 1min, - 12m 4min
        // ➚ 9m  1min, - 9m  7min
        // ➚ 6m  1min, - 6m  39min
        // ➚ 0m  6min
        var result =
            OxygenToxicity.CalculateCnsTransition(po20Ean25, po248Ean25, t10) +
            OxygenToxicity.CalculateCns(po248Ean25, t30) +
            OxygenToxicity.CalculateCnsTransition(po248Ean25, po221Ean25, t6) +
            OxygenToxicity.CalculateCns(po221Ean50, t3) +
            OxygenToxicity.CalculateCnsTransition(po221Ean50, po215Ean50, t1) +
            OxygenToxicity.CalculateCns(po215Ean50, t2) +
            OxygenToxicity.CalculateCnsTransition(po215Ean50, po212Ean50, t1) +
            OxygenToxicity.CalculateCns(po212Ean50, t4) +
            OxygenToxicity.CalculateCnsTransition(po212Ean50, po29Ean50, t1) +
            OxygenToxicity.CalculateCns(po29Ean50, t7) +
            OxygenToxicity.CalculateCnsTransition(po29Ean50, po26Ean50, t1) +
            OxygenToxicity.CalculateCns(po26Ean50, t39) +
            OxygenToxicity.CalculateCnsTransition(po26Ean50, po20Ean50, t6);

        // Assert
        Assert.InRange(result, 48.9, 49.0);
    }

    [Fact]
    public void CalculateCns_ShouldBeAboutNinetySixPercent_WhenAt1600mbarFor45Minutes()
    {
        // Arrange
        var po2Mbar = 1600;
        var durationSec = 45 * 60;

        // Act
        var result = OxygenToxicity.CalculateCns(po2Mbar, durationSec);

        // Assert
        Assert.InRange(result, 95, 97);
    }

    [Fact]
    public void CalculateOtu_ShouldReturnZero_WhenPo2Is500OrLess()
    {
        Assert.Equal(0.0, OxygenToxicity.CalculateOtu(209, 60));
        Assert.Equal(0.0, OxygenToxicity.CalculateOtu(500, 120));
        Assert.Equal(0.0, OxygenToxicity.CalculateOtu(0, 300));
    }

    [Fact]
    public void CalculateOtu_ShouldBeAbout120_WhenMultistageDive()
    {
        // Arrange
        var ctx = DiveContext.Default;

        var ean25 = new GasMix(250, 0);
        var ean50 = new GasMix(500, 0);

        const int d0 = 0;
        const int d6 = 6000;
        const int d9 = 9000;
        const int d12 = 12000;
        const int d15 = 15000;
        const int d21 = 21000;
        const int d48 = 48000;

        var po20Ean25 = ctx.PO2Mbar(ean25, d0);
        var po248Ean25 = ctx.PO2Mbar(ean25, d48);
        var po221Ean25 = ctx.PO2Mbar(ean25, d21);

        var po221Ean50 = ctx.PO2Mbar(ean50, d21);
        var po215Ean50 = ctx.PO2Mbar(ean50, d15);
        var po212Ean50 = ctx.PO2Mbar(ean50, d12);
        var po29Ean50 = ctx.PO2Mbar(ean50, d9);
        var po26Ean50 = ctx.PO2Mbar(ean50, d6);
        var po20Ean50 = ctx.PO2Mbar(ean50, d0);

        var t10 = 10 * 60;
        var t30 = 30 * 60;
        var t6 = 6 * 60;
        var t3 = 3 * 60;
        var t1 = 1 * 60;
        var t2 = 2 * 60;
        var t4 = 4 * 60;
        var t7 = 7 * 60;
        var t39 = 39 * 60;

        // Act
        // Plan:
        // ➘ 48m 10min (EAN25)
        // ➙ 48m 30min (EAN25)
        // ➚ 21m 6min (EAN25)
        // - 21m 3min (EAN50)
        // ➚ 15m 1min, - 15m 2min
        // ➚ 12m 1min, - 12m 4min
        // ➚ 9m  1min, - 9m  7min
        // ➚ 6m  1min, - 6m  39min
        // ➚ 0m  6min
        var result =
            OxygenToxicity.CalculateOtuTransition(po20Ean25, po248Ean25, t10) +
            OxygenToxicity.CalculateOtu(po248Ean25, t30) +
            OxygenToxicity.CalculateOtuTransition(po248Ean25, po221Ean25, t6) +
            OxygenToxicity.CalculateOtu(po221Ean50, t3) +
            OxygenToxicity.CalculateOtuTransition(po221Ean50, po215Ean50, t1) +
            OxygenToxicity.CalculateOtu(po215Ean50, t2) +
            OxygenToxicity.CalculateOtuTransition(po215Ean50, po212Ean50, t1) +
            OxygenToxicity.CalculateOtu(po212Ean50, t4) +
            OxygenToxicity.CalculateOtuTransition(po212Ean50, po29Ean50, t1) +
            OxygenToxicity.CalculateOtu(po29Ean50, t7) +
            OxygenToxicity.CalculateOtuTransition(po29Ean50, po26Ean50, t1) +
            OxygenToxicity.CalculateOtu(po26Ean50, t39) +
            OxygenToxicity.CalculateOtuTransition(po26Ean50, po20Ean50, t6);

        // Assert
        Assert.InRange(result, 119.5, 120);
    }
}