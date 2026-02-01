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

    private double SpecificWeight => Salinity * 0.981 / 100000.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint DepthToMbar(uint depthMm)
    {
        return (uint)(SurfacePressureMbar + SpecificWeight * depthMm);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DepthToBar(uint depthMm)
    {
        return DepthToMbar(depthMm) / 1000.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint RelMbarToDepthMm(uint mbar)
    {
        return (uint)(mbar / SpecificWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint MbarToDepthMm(uint mbar)
    {
        if (mbar <= SurfacePressureMbar)
        {
            return 0;
        }

        return RelMbarToDepthMm(mbar - SurfacePressureMbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GasModMm(GasMix mix,
        uint po2LimitMbar,
        uint roundToMm)
    {
        var maxPressureMbar = po2LimitMbar * 1000 / mix.O2Permille;
        var depthMm = (double)MbarToDepthMm(maxPressureMbar);

        return (uint)(depthMm / roundToMm + 0.1) * roundToMm;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GasMndMm(GasMix mix,
        uint endMm,
        bool o2IsNarcotic,
        uint roundToMm)
    {
        var pNarcoticMbar = DepthToMbar(endMm);

        uint maxAmbientMbar = 1000000;
        if (o2IsNarcotic)
        {
            maxAmbientMbar = (uint)(pNarcoticMbar / 1.0 - mix.HePermille / 1000.0);
        }
        else
        {
            if (mix.N2Permille > 0)
            {
                maxAmbientMbar = pNarcoticMbar * GasConstants.N2InAirPermille / mix.N2Permille;
            }
        }

        var depthMm = (double)MbarToDepthMm(maxAmbientMbar);
        return (uint)(depthMm / roundToMm) * roundToMm;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint PO2Mbar(GasMix mix, uint depthMm)
    {
        return DepthToMbar(depthMm) * mix.O2Permille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint PN2Mbar(GasMix mix, uint depthMm)
    {
        return DepthToMbar(depthMm) * mix.N2Permille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint PHeMbar(GasMix mix, uint depthMm)
    {
        return DepthToMbar(depthMm) * mix.HePermille / 1000;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint EndMm(GasMix mix,
        uint depthMm,
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