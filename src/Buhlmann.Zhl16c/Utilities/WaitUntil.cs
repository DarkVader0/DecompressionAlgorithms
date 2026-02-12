using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Settings;

namespace Buhlmann.Zhl16c.Utilities;

public static class WaitUntil
{
    private const int MaxWaitSeconds = 48 * 3600;
    private const int TimeoutReturnSeconds = 50 * 3600;

    public static int FindClearTime(
        ref DecoState ds,
        int clock,
        int min,
        int leap,
        int stepSizeSec,
        uint depthMm,
        uint targetDepthMm,
        uint avgDepthMm,
        GasMix gasMix,
        DiveMode diveMode,
        int setpointMbar,
        double gfLow,
        double gfHigh,
        AscentDescentSettings ascentSettings,
        DiveContext context)
    {
        if (min >= MaxWaitSeconds)
        {
            return TimeoutReturnSeconds;
        }

        var upper = min + leap + stepSizeSec - 1 - (min + leap - 1) % stepSizeSec;

        if (!TrialAscent.IsClearToAscend(
                ref ds, depthMm, targetDepthMm, avgDepthMm,
                gasMix, diveMode, setpointMbar,
                upper - clock,
                gfLow, gfHigh, ascentSettings, context))
        {
            return FindClearTime(
                ref ds, clock, upper, leap, stepSizeSec,
                depthMm, targetDepthMm, avgDepthMm,
                gasMix, diveMode, setpointMbar,
                gfLow, gfHigh, ascentSettings, context);
        }

        if (upper - min <= stepSizeSec)
        {
            return upper;
        }

        return FindClearTime(
            ref ds, clock, min, leap / 2, stepSizeSec,
            depthMm, targetDepthMm, avgDepthMm,
            gasMix, diveMode, setpointMbar,
            gfLow, gfHigh, ascentSettings, context);
    }
}