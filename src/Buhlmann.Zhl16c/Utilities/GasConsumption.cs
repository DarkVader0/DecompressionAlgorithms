using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Helpers;

namespace Buhlmann.Zhl16c.Utilities;

public static class GasConsumption
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateAtDepth(uint depthMm,
        uint durationSec,
        uint sacMlMin,
        DiveContext context)
    {
        var pressureBar = context.DepthToBar(depthMm);
        var surfacePressureBar = context.SurfacePressureMbar / 1000.0;
        var ata = pressureBar / surfacePressureBar;

        return (uint)(sacMlMin * durationSec / 60.0 * ata);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateForTransition(uint startDepthMm,
        uint endDepthMm,
        uint durationSec,
        uint sacMlMin,
        DiveContext context)
    {
        var avgDepthMm = (startDepthMm + endDepthMm) / 2;
        return CalculateAtDepth(avgDepthMm, durationSec, sacMlMin, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TransitionDuration(uint startDepthMm,
        uint endDepthMm,
        uint rateMmMin)
    {
        var distanceMm = Math.Abs((int)endDepthMm - (int)startDepthMm);

        return (uint)(distanceMm * 60.0 / rateMmMin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RemainingPressureMbar(uint startPressureMbar,
        uint gasUsedMl,
        uint cylinderVolumeMl)
    {
        var totalGasMl = (long)startPressureMbar * cylinderVolumeMl / 1000;
        var remainingGasMl = totalGasMl - gasUsedMl;

        if (remainingGasMl <= 0)
        {
            return 0;
        }

        return (uint)(remainingGasMl * 1000 / cylinderVolumeMl);
    }
}