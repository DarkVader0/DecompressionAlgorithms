using System.Runtime.CompilerServices;

namespace Buhlmann.Zhl16c.Utilities;

public static class OxygenToxicity
{
    public const double CnsHalfLifeMin = 90.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CnsRatePerMinute(int po2Mbar)
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
        return CnsRatePerMinute(po2Mbar) * durationSec / 60.0 * 100.0; 
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateCnsTransition(int startPo2Mbar, int endPo2Mbar, int durationSec)
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
}