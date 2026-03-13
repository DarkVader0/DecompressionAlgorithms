using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Output;

namespace Buhlmann.Zhl16c.Utilities;

public static class UnitConverter
{
    private const double FeetToMmFactor = 304.8;
    private const double CuftToMlFactor = 28316.846592;
    private const double PsiToMbarFactor = 68.9476;
    private const double FtPerMinToMmPerSecFactor = 304.8 / 60.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FeetToMm(double feet)
    {
        return (int)(feet * FeetToMmFactor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MmToFeet(int mm)
    {
        return mm / FeetToMmFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PsiToMbar(double psi)
    {
        return (int)(psi * PsiToMbarFactor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MbarToPsi(int mbar)
    {
        return mbar / PsiToMbarFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CuftToMl(double cuft)
    {
        return (int)(cuft * CuftToMlFactor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MlToCuft(int ml)
    {
        return ml / CuftToMlFactor;
    }

    /// <summary>Converts ft/min to mm/sec.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort FtPerMinToMmPerSec(double ftPerMin)
    {
        return (ushort)(ftPerMin * FtPerMinToMmPerSecFactor);
    }

    /// <summary>Converts mm/sec to ft/min.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MmPerSecToFtPerMin(ushort mmPerSec)
    {
        return mmPerSec / FtPerMinToMmPerSecFactor;
    }

    /// <summary>
    /// Creates a metric Waypoint from imperial depth in feet.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Waypoint WaypointFromFeet(double depthFeet,
        int durationSeconds,
        sbyte cylinderIndex)
    {
        return new Waypoint
        {
            DepthMm = FeetToMm(depthFeet),
            DurationSeconds = durationSeconds,
            CylinderIndex = cylinderIndex
        };
    }

    /// <summary>
    /// Creates a metric Cylinder from imperial size (cuft) and pressure (psi).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cylinder CylinderFromImperial(
        ushort o2Permille,
        ushort hePermille,
        double sizeCuft,
        double startPressurePsi,
        CylinderUse use = CylinderUse.None)
    {
        return new Cylinder
        {
            O2Permille = o2Permille,
            HePermille = hePermille,
            SizeMl = CuftToMl(sizeCuft),
            StartPressureMbar = PsiToMbar(startPressurePsi),
            Use = use
        };
    }

    /// <summary>
    /// Returns a plan segment's start and end depths in feet.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (double StartFeet, double EndFeet) SegmentDepthsInFeet(in PlanSegment seg)
    {
        return (MmToFeet(seg.DepthStartMm), MmToFeet(seg.DepthEndMm));
    }

    /// <summary>
    /// Returns a plan result's max and average depths in feet.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (double MaxFeet, double AvgFeet) ResultDepthsInFeet(in PlanResult result)
    {
        return (MmToFeet(result.MaxDepthMm), MmToFeet(result.AvgDepthMm));
    }
}