using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Settings;

namespace Buhlmann.Zhl16c.Utilities;

public static class AscentRate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetAscentRate(int currentDepthMm,
        int avgDepthMm,
        AscentDescentSettings settings)
    {
        if (currentDepthMm * 4 > avgDepthMm * 3)
        {
            return settings.AscentRate75MmSec;
        }

        if (currentDepthMm * 2 > avgDepthMm)
        {
            return settings.AscentRate50MmSec;
        }

        if (currentDepthMm > 6000)
        {
            return settings.AscentRateStopsMmSec;
        }

        return settings.AscentRateLast6mMmSec;
    }
}