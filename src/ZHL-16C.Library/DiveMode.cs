namespace ZHL_16C.Library;

/// <summary>
/// Dive mode enumeration matching Subsurface's core/divemode.h
/// </summary>
public enum DiveMode
{
    /// <summary>Open Circuit</summary>
    // ReSharper disable once InconsistentNaming
    OC,

    /// <summary>Closed Circuit Rebreather</summary>
    // ReSharper disable once InconsistentNaming
    CCR,

    /// <summary>Passive Semi-Closed Rebreather</summary>
    // ReSharper disable once InconsistentNaming
    PSCR
}

public static class DiveModeExtensions
{
    /// <summary>
    /// Returns true if the dive mode is a rebreather mode (CCR or PSCR)
    /// </summary>
    public static bool IsRebreather(this DiveMode mode)
    {
        return mode is DiveMode.CCR or DiveMode.PSCR;
    }
}