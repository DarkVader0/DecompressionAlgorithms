namespace ZHL_16C.Library;

/// <summary>
/// A point in the dive plan (waypoint).
/// Matches Subsurface core/planner.h divedatapoint struct.
/// </summary>
public sealed class DiveDataPoint
{
    /// <summary>Time at this point (seconds from start)</summary>
    public int Time { get; set; }

    /// <summary>Depth at this point</summary>
    public Depth Depth { get; set; }

    /// <summary>Cylinder/gas index being used</summary>
    public int CylinderId { get; set; }

    /// <summary>Minimum gas required at this point (mbar)</summary>
    public Pressure MinimumGas { get; set; }

    /// <summary>Setpoint for CCR (mbar), 0 for OC</summary>
    public int Setpoint { get; set; }

    /// <summary>Was this point manually entered by user</summary>
    public bool Entered { get; set; }

    /// <summary>Dive mode at this point</summary>
    public DiveMode DiveMode { get; set; } = DiveMode.OC;
}

/// <summary>
/// Complete dive plan structure.
/// Matches Subsurface core/planner.h diveplan struct.
/// </summary>
public sealed class DivePlan
{
    /// <summary>When the dive starts (Unix timestamp)</summary>
    public long When { get; set; }

    /// <summary>Surface pressure</summary>
    public Pressure SurfacePressure { get; set; } = Pressure.FromBar(1.013);

    /// <summary>Bottom SAC rate (ml/min)</summary>
    public int BottomSac { get; set; }

    /// <summary>Deco SAC rate (ml/min)</summary>
    public int DecoSac { get; set; }

    /// <summary>Water salinity (g/10L)</summary>
    public int Salinity { get; set; } = PhysicalConstants.SeawaterSalinity;

    /// <summary>Gradient factor low (0-100)</summary>
    public short GfLow { get; set; }

    /// <summary>Gradient factor high (0-100)</summary>
    public short GfHigh { get; set; }

    /// <summary>List of waypoints in the plan</summary>
    public List<DiveDataPoint> DataPoints { get; } = new();

    /// <summary>Effective GF low (calculated)</summary>
    public int EffectiveGfLow { get; set; }

    /// <summary>Effective GF high (calculated)</summary>
    public int EffectiveGfHigh { get; set; }

    /// <summary>Surface interval before this dive (seconds)</summary>
    public int SurfaceInterval { get; set; }

    /// <summary>Check if plan has any points</summary>
    public bool IsEmpty => DataPoints.Count == 0;

    /// <summary>Total duration of the plan in seconds</summary>
    public int Duration => DataPoints.Count > 0
        ? DataPoints.Max(dp => dp.Time)
        : 0;
}

/// <summary>
/// Planner error codes.
/// Matches Subsurface planner_error_t enum.
/// </summary>
public enum PlannerError
{
    Ok,
    Timeout,
    InappropriateGas,
    NoSuitableBailoutGas
}