using System.Runtime.CompilerServices;

namespace Buhlmann.Zhl16c.Utilities;

public static class OxygenToxicity
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CnsRatePerSecond(uint po2Mbar)
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
    public static double CalculateCns(uint po2Mbar, uint durationSec)
    {
        return CnsRatePerSecond(po2Mbar) * durationSec * 100.0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateCnsTransition(uint startPo2Mbar,
        uint endPo2Mbar,
        uint durationSec)
    {
        var avgPo2Mbar = (startPo2Mbar + endPo2Mbar) / 2;
        return CalculateCns(avgPo2Mbar, durationSec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateOtu(uint po2Mbar, uint durationSec)
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
    public static double CalculateOtuTransition(uint startPo2Mbar,
        uint endPo2Mbar,
        uint durationSec)
    {
        var avgPo2Mbar = (startPo2Mbar + endPo2Mbar) / 2;
        return CalculateOtu(avgPo2Mbar, durationSec);
    }
}