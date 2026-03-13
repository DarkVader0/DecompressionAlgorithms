using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Output;

namespace Buhlmann.Zhl16c.Utilities;

public static class OxygenToxicity
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CnsRatePerSecond(int po2Mbar)
    {
        if (po2Mbar <= 500)
        {
            return 0;
        }

        return po2Mbar <= 1500
            ? Math.Exp(-11.7853 + 0.00193873 * po2Mbar)
            : Math.Exp(-23.6349 + 0.00980829 * po2Mbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateCns(int po2Mbar, int durationSec)
    {
        return CnsRatePerSecond(po2Mbar) * durationSec * 100.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateCnsTransition(int startPo2Mbar,
        int endPo2Mbar,
        int durationSec)
    {
        var avgPo2Mbar = (startPo2Mbar + endPo2Mbar) / 2;
        return CalculateCns(avgPo2Mbar, durationSec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateOtu(int po2Mbar, int durationSec)
    {
        if (po2Mbar <= 500)
        {
            return 0;
        }

        var po2Bar = po2Mbar / 1000.0;
        var durationMin = durationSec / 60.0;

        return durationMin * Math.Pow((po2Bar - 0.5) / 0.5, 0.83);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateOtuTransition(int startPo2Mbar,
        int endPo2Mbar,
        int durationSec)
    {
        var avgPo2Mbar = (startPo2Mbar + endPo2Mbar) / 2;
        return CalculateOtu(avgPo2Mbar, durationSec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyToPlan(ref PlanResult result,
        ReadOnlySpan<Cylinder> cylinders,
        DiveContext context)
    {
        var cns = 0.0;
        var otu = 0.0;

        for (var i = 0; i < result.SegmentCount; i++)
        {
            ref readonly var seg = ref result.Segments[i];
            var mix = new GasMix(cylinders[seg.CylinderIndex].O2Permille, cylinders[seg.CylinderIndex].HePermille);
            var duration = seg.RuntimeEndSec - seg.RuntimeStartSec;

            if (duration <= 0)
            {
                continue;
            }

            if (seg.DepthStartMm == seg.DepthEndMm)
            {
                var po2 = seg is { DiveMode: DiveMode.CCR, SetpointMbar: > 0 }
                    ? Math.Min(seg.SetpointMbar, context.DepthToMbar(seg.DepthEndMm) * mix.O2Permille / 1000)
                    : context.PO2Mbar(mix, seg.DepthEndMm);

                cns += CalculateCns(po2, duration);
                otu += CalculateOtu(po2, duration);

                continue;
            }

            var startPo2 = seg is { DiveMode: DiveMode.CCR, SetpointMbar: > 0 }
                ? Math.Min(seg.SetpointMbar, context.DepthToMbar(seg.DepthStartMm) * mix.O2Permille / 1000)
                : context.PO2Mbar(mix, seg.DepthStartMm);

            var endPo2 = seg is { DiveMode: DiveMode.CCR, SetpointMbar: > 0 }
                ? Math.Min(seg.SetpointMbar, context.DepthToMbar(seg.DepthEndMm) * mix.O2Permille / 1000)
                : context.PO2Mbar(mix, seg.DepthEndMm);

            cns += CalculateCnsTransition(startPo2, endPo2, duration);
            otu += CalculateOtuTransition(startPo2, endPo2, duration);
        }

        result.CnsPercent = (ushort)Math.Min(cns, ushort.MaxValue);
        result.OtuTotal = (ushort)Math.Min(otu, ushort.MaxValue);
    }
}