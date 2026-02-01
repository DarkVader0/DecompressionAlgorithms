using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Settings;

namespace Buhlmann.Zhl16c.Utilities;

public static class Validation
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PlanError ValidateGasMix(GasMix mix)
    {
        if (mix.O2Permille < 10 || mix.O2Permille > 1000 || mix.HePermille > 1000 ||
            mix.O2Permille + mix.HePermille > 1000)
        {
            return PlanError.InvalidInput;
        }

        return PlanError.Ok;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlanError ValidateCylinder(Cylinder cylinder)
    {
        var error = ValidateGasMix(new GasMix(cylinder.O2Permille, cylinder.HePermille));

        return error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PlanError ValidateGradientFactors(byte gfLow, byte gfHigh)
    {
        if (gfLow < 1 || gfLow > 100 || gfHigh < 1 || gfHigh > 100 || gfLow > gfHigh)
        {
            return PlanError.InvalidInput;
        }

        return PlanError.Ok;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlanError ValidateSettings(PlannerSettings settings)
    {
        var error = ValidateGradientFactors(settings.Deco.GFLow, settings.Deco.GFHigh);

        return error;
    }
}