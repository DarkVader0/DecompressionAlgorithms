namespace ZHL_16C.Library;

/// <summary>
/// Dive planner settings - only needed if you're building a dive planner.
/// These are NOT required for the core ZHL-16C algorithm itself.
/// Matches Subsurface core/pref.h preferences struct.
/// </summary>
public sealed class PlannerSettings
{
    /// <summary>Ascent rate in last 6m (mm/min). Default: 9000/60 = 150 mm/min = 9 m/min</summary>
    // ReSharper disable once InconsistentNaming
    public int AscentRateLast6m { get; set; } = 9000 / 60; // 9 m/min

    /// <summary>Ascent rate at stops (mm/min). Default: 9 m/min</summary>
    public int AscentRateStops { get; set; } = 9000 / 60;

    /// <summary>Ascent rate below 50% of max depth (mm/min). Default: 9 m/min</summary>
    public int AscentRate50 { get; set; } = 9000 / 60;

    /// <summary>Ascent rate below 75% of max depth (mm/min). Default: 9 m/min</summary>
    public int AscentRate75 { get; set; } = 9000 / 60;

    /// <summary>Descent rate (mm/min). Default: 18000/60 = 300 mm/min = 18 m/min</summary>
    public int DescentRate { get; set; } = 18000 / 60;

    /// <summary>Bottom SAC rate (ml/min). Default: 20000 = 20 L/min</summary>
    public int BottomSac { get; set; } = 20000;

    /// <summary>Deco SAC rate (ml/min). Default: 17000 = 17 L/min</summary>
    public int DecoSac { get; set; } = 17000;

    /// <summary>SAC factor for reserve calculation. Default: 400 = 4.0x</summary>
    public int SacFactor { get; set; } = 400;

    /// <summary>Reserve gas in mbar. Default: 40000 = 40 bar</summary>
    public int ReserveGas { get; set; } = 40000;

    /// <summary>Bottom PO2 limit (mbar). Default: 1400 = 1.4 bar</summary>
    public int BottomPo2 { get; set; } = 1400;

    /// <summary>Deco PO2 limit (mbar). Default: 1600 = 1.6 bar</summary>
    public int DecoPo2 { get; set; } = 1600;

    /// <summary>Do last stop at 6m instead of 3m</summary>
    public bool LastStop { get; set; } = false;

    /// <summary>Add safety stop on no-deco dives</summary>
    public bool SafetyStop { get; set; } = true;

    /// <summary>Minimum time at a stop before switching gas (seconds)</summary>
    public int MinSwitchDuration { get; set; } = 60;
}