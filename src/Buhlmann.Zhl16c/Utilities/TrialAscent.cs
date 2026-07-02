using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Settings;

namespace Buhlmann.Zhl16c.Utilities;

public static class TrialAscent
{
    private const int BaseTimestep = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClearToAscend(
        ref DecoState ds,
        int trialDepthMm,
        int stopLevelMm,
        int avgDepthMm,
        GasMix gasMix,
        DiveMode diveMode,
        int setpointMbar,
        int waitTimeSec,
        double gfLow,
        double gfHigh,
        AscentDescentSettings ascentSettings,
        DiveContext context)
    {
        var saved = ds.Clone();

        if (waitTimeSec > 0)
        {
            ds.AddSegment(
                context.DepthToBar(trialDepthMm),
                gasMix,
                waitTimeSec,
                diveMode,
                setpointMbar);
        }

        var clearToAscend = true;
        var depthMm = trialDepthMm;

        while (depthMm > stopLevelMm)
        {
            var rateMmSec = AscentRate.GetAscentRate(depthMm, avgDepthMm, ascentSettings);
            var deltadMm = rateMmSec * BaseTimestep;

            var stepSec = BaseTimestep;
            if (deltadMm > depthMm)
            {
                deltadMm = depthMm;
                stepSec = Math.Max(1, (int)Math.Round((double)deltadMm / rateMmSec));
            }

            var nextDepthMm = depthMm - deltadMm;

            ds.AddSegment(
                context.DepthToBar((depthMm + nextDepthMm) / 2),
                gasMix,
                stepSec,
                diveMode,
                setpointMbar);

            var ceilingMm = ds.CeilingMm(gfLow, gfHigh, context);

            if (ceilingMm > depthMm - deltadMm)
            {
                clearToAscend = false;
                break;
            }

            depthMm -= deltadMm;
        }

        ds.CopyFrom(ref saved);

        return clearToAscend;
    }
}