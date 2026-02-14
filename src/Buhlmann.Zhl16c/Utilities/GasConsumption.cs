using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Helpers;

namespace Buhlmann.Zhl16c.Utilities;

public static class GasConsumption
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateAtDepth(int depthMm,
        int durationSec,
        int sacMlMin,
        DiveContext context)
    {
        var pressureBar = context.DepthToBar(depthMm);
        var surfacePressureBar = context.SurfacePressureMbar / 1000.0;
        var ata = pressureBar / surfacePressureBar;

        return (int)(sacMlMin * durationSec / 60.0 * ata);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateForTransition(int startDepthMm,
        int endDepthMm,
        int durationSec,
        int sacMlMin,
        DiveContext context)
    {
        var avgDepthMm = (startDepthMm + endDepthMm) / 2;
        return CalculateAtDepth(avgDepthMm, durationSec, sacMlMin, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TransitionDuration(int startDepthMm,
        int endDepthMm,
        int rateMmMin)
    {
        var distanceMm = Math.Abs((int)endDepthMm - (int)startDepthMm);

        return (int)(distanceMm * 60.0 / rateMmMin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RemainingPressureMbar(int startPressureMbar,
        int gasUsedMl,
        int cylinderVolumeMl)
    {
        var totalGasMl = (long)startPressureMbar * cylinderVolumeMl / 1000;
        var remainingGasMl = totalGasMl - gasUsedMl;

        if (remainingGasMl <= 0)
        {
            return 0;
        }

        return (int)(remainingGasMl * 1000 / cylinderVolumeMl);
    }
}