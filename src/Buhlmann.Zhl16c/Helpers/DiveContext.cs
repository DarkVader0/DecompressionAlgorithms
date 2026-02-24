using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Helpers;

public struct DiveContext
{
    public ushort SurfacePressureMbar;
    public ushort Salinity;

    public DiveContext(ushort surfacePressureMbar, WaterType waterType)
    {
        SurfacePressureMbar = surfacePressureMbar;
        Salinity = waterType switch
        {
            WaterType.Fresh => GasConstants.FreshWaterSalinity,
            WaterType.Salt => GasConstants.SaltWaterSalinity,
            WaterType.EN13319 => GasConstants.En13319Salinity,
            _ => GasConstants.SaltWaterSalinity
        };
    }

    private readonly double SpecificWeight => Salinity * 0.981 / 100000.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int DepthToMbar(int depthMm)
    {
        return (int)(SurfacePressureMbar + SpecificWeight * depthMm);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double DepthToBar(int depthMm)
    {
        return DepthToMbar(depthMm) / 1000.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RelMbarToDepthMm(int mbar)
    {
        return (int)(mbar / SpecificWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int MbarToDepthMm(int mbar)
    {
        if (mbar <= SurfacePressureMbar)
        {
            return 0;
        }

        return RelMbarToDepthMm(mbar - SurfacePressureMbar);
    }

    // TODO: Implement option for real vs simple Gas MOD calculations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GasModMm(GasMix mix,
        int po2LimitMbar,
        int roundToMm)
    {
        return GasModMmSimple(mix, po2LimitMbar, roundToMm);
        // var maxPressureMbar = po2LimitMbar * 1000 / mix.O2Permille;
        // var depthMm = (double)MbarToDepthMm(maxPressureMbar);
        //
        // return (int)(depthMm / roundToMm + 0.1) * roundToMm;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GasModMmSimple(GasMix mix,
        int po2LimitMbar,
        int roundToMm)
    {
        var maxPressureMbar = po2LimitMbar * 1000 / mix.O2Permille;
        var depthMm = (double)(maxPressureMbar - 1000) * 10;

        var steps = depthMm / roundToMm;

        var roundedSteps =
            po2LimitMbar <= 1400
                ? Math.Round(steps, MidpointRounding.AwayFromZero)
                : Math.Floor(steps);

        return (int)(roundedSteps * roundToMm);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GasMndMm(GasMix mix,
        int endMm,
        bool o2IsNarcotic,
        int roundToMm)
    {
        var pNarcoticMbar = DepthToMbar(endMm);

        var maxAmbientMbar = 1000000;
        if (o2IsNarcotic)
        {
            maxAmbientMbar = (int)(pNarcoticMbar / 1.0 - mix.HePermille / 1000.0);
        }
        else
        {
            if (mix.N2Permille > 0)
            {
                maxAmbientMbar = pNarcoticMbar * GasConstants.N2InAirPermille / mix.N2Permille;
            }
        }

        var depthMm = (double)MbarToDepthMm(maxAmbientMbar);
        return (int)(depthMm / roundToMm) * roundToMm;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PO2Mbar(GasMix mix, int depthMm)
    {
        return DepthToMbar(depthMm) * mix.O2Permille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PN2Mbar(GasMix mix, int depthMm)
    {
        return DepthToMbar(depthMm) * mix.N2Permille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PHeMbar(GasMix mix, int depthMm)
    {
        return DepthToMbar(depthMm) * mix.HePermille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EndMm(GasMix mix,
        int depthMm,
        bool o2IsNarcotic)
    {
        var narcoticPermille = o2IsNarcotic
            ? (ushort)(mix.O2Permille + mix.N2Permille)
            : mix.N2Permille;

        var ambientMbar = DepthToMbar(depthMm);
        var narcoticMbar = ambientMbar * narcoticPermille / 1000;

        var airNarcoticPermille = o2IsNarcotic ? (ushort)1000 : GasConstants.N2InAirPermille;
        var endAmibientMbar = narcoticMbar * 1000 / airNarcoticPermille;

        return MbarToDepthMm(endAmibientMbar);
    }

    public static DiveContext Default => new(GasConstants.StandardPressureMbar, WaterType.Salt);
}