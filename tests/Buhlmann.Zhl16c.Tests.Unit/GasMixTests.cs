using Buhlmann.Zhl16c.Helpers;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class GasMixTests
{
    [Fact]
    public void N2Permille_ShouldBeRemainderToOneThousand()
    {
        // Arrange
        var mix = new GasMix(180, 350);

        // Act
        var n2 = mix.N2Permille;

        // Assert
        Assert.Equal((ushort)470, n2);
    }

    [Fact]
    public void IsAir_ShouldBeTrue_ForO2Between209And211_AndHeZero()
    {
        // Arrange
        var air209 = new GasMix(209, 0);
        var air210 = new GasMix(210, 0);
        var air211 = new GasMix(211, 0);

        // Act
        var r209 = air209.IsAir;
        var r210 = air210.IsAir;
        var r211 = air211.IsAir;

        // Assert
        Assert.True(r209);
        Assert.True(r210);
        Assert.True(r211);
    }

    [Fact]
    public void IsAir_ShouldBeFalse_WhenHeliumIsNotZero()
    {
        // Arrange
        var notAir = new GasMix(210, 10);

        // Act
        var result = notAir.IsAir;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAir_ShouldBeFalse_WhenO2OutOfRange()
    {
        // Arrange
        var low = new GasMix(208, 0);
        var high = new GasMix(212, 0);

        // Act
        var lowResult = low.IsAir;
        var highResult = high.IsAir;

        // Assert
        Assert.False(lowResult);
        Assert.False(highResult);
    }

    [Fact]
    public void IsOxygen_ShouldBeTrue_OnlyWhenO2IsThousand()
    {
        // Arrange
        var o2 = new GasMix(1000, 0);
        var notO2 = new GasMix(999, 0);

        // Act
        var r1 = o2.IsOxygen;
        var r2 = notO2.IsOxygen;

        // Assert
        Assert.True(r1);
        Assert.False(r2);
    }

    [Fact]
    public void IsNitrox_ShouldBeTrue_WhenHeIsZero_AndO2GreaterThan211()
    {
        // Arrange
        var ean32 = new GasMix(320, 0);
        var ean50 = new GasMix(500, 0);
        var air = new GasMix(210, 0);
        var trimix = new GasMix(320, 200);

        // Act
        var r32 = ean32.IsNitrox;
        var r50 = ean50.IsNitrox;
        var rAir = air.IsNitrox;
        var rTx = trimix.IsNitrox;

        // Assert
        Assert.True(r32);
        Assert.True(r50);
        Assert.False(rAir);
        Assert.False(rTx);
    }

    [Fact]
    public void IsTrimix_ShouldBeTrue_WhenHeliumGreaterThanZero()
    {
        // Arrange
        var tx = new GasMix(180, 450);
        var nitrox = new GasMix(320, 0);

        // Act
        var rTx = tx.IsTrimix;
        var rNx = nitrox.IsTrimix;

        // Assert
        Assert.True(rTx);
        Assert.False(rNx);
    }

    [Fact]
    public void StaticAir_ShouldBeAir()
    {
        // Arrange
        var air = GasMix.Air;

        // Act
        var isAir = air.IsAir;

        // Assert
        Assert.True(isAir);
        Assert.Equal((ushort)210, air.O2Permille);
        Assert.Equal((ushort)0, air.HePermille);
        Assert.Equal((ushort)790, air.N2Permille);
    }

    [Fact]
    public void StaticOxygen_ShouldBePureOxygen()
    {
        // Arrange
        var o2 = GasMix.Oxygen;

        // Act
        var isO2 = o2.IsOxygen;

        // Assert
        Assert.True(isO2);
        Assert.Equal((ushort)1000, o2.O2Permille);
        Assert.Equal((ushort)0, o2.HePermille);
        Assert.Equal((ushort)0, o2.N2Permille);
    }
}