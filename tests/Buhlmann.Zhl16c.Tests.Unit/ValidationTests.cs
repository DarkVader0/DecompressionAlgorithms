using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class ValidationTests
{
    [Fact]
    public void ValidateCylinder_ShouldReturnOk_WhenCylinderIsValid()
    {
        // Arrange
        var cylinder = new Cylinder();
        cylinder.O2Permille = 210; // 21% O2
        cylinder.HePermille = 0; // 0% He
        cylinder.SizeMl = 12000; // 12L
        cylinder.StartPressureMbar = 200000; // 200 bar
        cylinder.Use = CylinderUse.Bottom;

        // Act
        var result = Validation.ValidateCylinder(cylinder);

        // Assert
        Assert.Equal(PlanError.Ok, result);
    }

    [Fact]
    public void ValidateCylinder_ShouldReturnInvalidInput_WhenCylinderHasInvalidGasMixWithTooMuchO2()
    {
        // Arrange
        var cylinder = new Cylinder();
        cylinder.O2Permille = 1200; // Invalid O2 percentage
        cylinder.HePermille = 0; // 0% He
        cylinder.SizeMl = 12000; // 12L
        cylinder.StartPressureMbar = 200000; // 200 bar
        cylinder.Use = CylinderUse.Bottom;

        // Act
        var result = Validation.ValidateCylinder(cylinder);

        // Assert
        Assert.Equal(PlanError.InvalidInput, result);
    }

    [Fact]
    public void ValidateCylinder_ShouldReturnInvalidInput_WhenCylinderHasInvalidGasMixWithTooMuchHe()
    {
        // Arrange
        var cylinder = new Cylinder();
        cylinder.O2Permille = 210; // Invalid O2 percentage
        cylinder.HePermille = 10000; // 0% He
        cylinder.SizeMl = 12000; // 12L
        cylinder.StartPressureMbar = 200000; // 200 bar
        cylinder.Use = CylinderUse.Bottom;

        // Act
        var result = Validation.ValidateCylinder(cylinder);

        // Assert
        Assert.Equal(PlanError.InvalidInput, result);
    }

    [Fact]
    public void ValidateSettings_ShouldReturnOk_WhenSettingsAreValid()
    {
        // Arrange
        var settings = new PlannerSettings();
        settings.Deco.GFLow = 30;
        settings.Deco.GFHigh = 85;

        // Act
        var result = Validation.ValidateSettings(settings);

        // Assert
        Assert.Equal(PlanError.Ok, result);
    }

    [Fact]
    public void ValidateSettings_ShouldReturnInvalidInput_WhenSettingsHaveInvalidGradientFactors()
    {
        // Arrange
        var settings = new PlannerSettings();
        settings.Deco.GFLow = 90;
        settings.Deco.GFHigh = 80;

        // Act
        var result = Validation.ValidateSettings(settings);

        // Assert
        Assert.Equal(PlanError.InvalidInput, result);
    }
}