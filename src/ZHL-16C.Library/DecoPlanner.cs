using System.Text;

namespace ZHL_16C.Library;

/// <summary>
/// Dive decompression planner using ZHL-16C algorithm.
/// Based on Subsurface core/planner. cpp
/// </summary>
public sealed class DecoPlanner
{
    // Standard deco stop depths in meters
    private static readonly int[] DecoStopLevelsMm =
    {
        0, 3000, 6000, 9000, 12000, 15000, 18000, 21000, 24000, 27000,
        30000, 33000, 36000, 39000, 42000, 45000, 48000, 51000, 54000, 57000,
        60000, 63000, 66000, 69000, 72000, 75000, 78000, 81000, 84000, 87000,
        90000, 100000, 110000, 120000, 130000, 140000, 150000
    };

    private readonly Zhl16CAlgorithm _algorithm;
    private readonly AlgorithmConfiguration _config;
    private readonly PlannerSettings _settings;

    public DecoPlanner(AlgorithmConfiguration? config = null, PlannerSettings? settings = null)
    {
        _config = config ?? new AlgorithmConfiguration();
        _settings = settings ?? new PlannerSettings();
        _algorithm = new Zhl16CAlgorithm(_config);
    }

    /// <summary>
    /// Calculate a complete decompression schedule from a dive profile.
    /// </summary>
    /// <param name="diveProfile">List of waypoints (depth, time, gas)</param>
    /// <param name="gases">Available gases for decompression</param>
    /// <param name="surfacePressure">Surface pressure in bar</param>
    /// <param name="salinity">Water salinity in g/10L</param>
    /// <returns>Decompression plan result</returns>
    public DecoPlanResult CalculateDecoPlan(
        List<DiveWaypoint> diveProfile,
        List<DecoGas> gases,
        double surfacePressure = 1.013,
        int salinity = 10300)
    {
        var state = new DecoState();
        var result = new DecoPlanResult();

        // Initialize tissues at surface
        _algorithm.ClearDeco(state, surfacePressure);

        // Sort gases by MOD (deepest first for deco gas selection)
        var sortedGases = gases
            .OrderByDescending(g => g.MaxOperatingDepthMm)
            .ToList();

        // Process the dive profile (descent + bottom time)
        var currentTime = 0;
        var lastDepth = new Depth(0);
        var currentGas = diveProfile.FirstOrDefault()?.Gas ?? GasMix.Air;

        foreach (var waypoint in diveProfile)
        {
            var segmentTime = waypoint.TimeSeconds - currentTime;
            if (segmentTime > 0)
            {
                // Interpolate through the segment
                AddInterpolatedSegment(state, lastDepth, waypoint.Depth,
                    segmentTime, waypoint.Gas, surfacePressure, salinity);
            }

            lastDepth = waypoint.Depth;
            currentGas = waypoint.Gas;
            currentTime = waypoint.TimeSeconds;

            result.Waypoints.Add(new PlanWaypoint
            {
                TimeSeconds = currentTime,
                Depth = waypoint.Depth,
                Gas = waypoint.Gas,
                IsUserEntered = true,
                SegmentType = SegmentType.BottomTime
            });
        }

        // Store bottom time info
        var bottomTime = currentTime;
        var maxDepth = diveProfile.Max(w => w.Depth);
        var avgDepth = new Depth(diveProfile.Sum(w => w.Depth.Mm) / diveProfile.Count);

        // Now calculate ascent with deco stops
        var decoStops = CalculateAscentSchedule(
            state, lastDepth, currentGas, sortedGases,
            surfacePressure, salinity, avgDepth, bottomTime,
            ref currentTime, result.Waypoints);

        result.DecoStops = decoStops;
        result.TotalDecoTimeSeconds = decoStops.Sum(s => s.TimeSeconds);
        result.TotalDiveTimeSeconds = currentTime;
        result.MaxDepth = maxDepth;
        result.BottomTimeSeconds = bottomTime;

        return result;
    }

    /// <summary>
    /// Calculate the ascent schedule including all deco stops.
    /// </summary>
    private List<DecoStop> CalculateAscentSchedule(
        DecoState state,
        Depth startDepth,
        GasMix bottomGas,
        List<DecoGas> sortedGases,
        double surfacePressure,
        int salinity,
        Depth avgDepth,
        int bottomTime,
        ref int currentTime,
        List<PlanWaypoint> waypoints)
    {
        var stops = new List<DecoStop>();
        var currentDepth = startDepth;
        var currentGas = bottomGas;

        // Find the first stop level above current depth
        var stopIndex = 0;
        for (var i = 0; i < DecoStopLevelsMm.Length; i++)
        {
            if (DecoStopLevelsMm[i] >= currentDepth.Mm)
            {
                stopIndex = Math.Max(0, i - 1);
                break;
            }
        }

        // Adjust for last stop setting (6m vs 3m)
        var lastStopDepthMm = _settings.LastStop ? 6000 : 3000;

        // Main ascent loop
        while (currentDepth.Mm > 0)
        {
            // Determine next stop depth
            var nextStopMm = stopIndex > 0 ? DecoStopLevelsMm[stopIndex] : 0;
            if (nextStopMm > 0 && nextStopMm < lastStopDepthMm)
            {
                nextStopMm = lastStopDepthMm;
            }

            var nextStop = new Depth(nextStopMm);

            // Check for gas switch
            var betterGas = FindBestGas(sortedGases, currentDepth, surfacePressure, salinity);
            if (betterGas != null && !IsSameGas(betterGas.Mix, currentGas))
            {
                // Gas switch - add minimum switch time at current depth
                currentGas = betterGas.Mix;
                var switchTime = _settings.MinSwitchDuration;

                var pressure = Zhl16CAlgorithm.DepthToPressure(currentDepth, salinity, surfacePressure);
                _algorithm.AddSegment(state, pressure, currentGas, switchTime, 0, DiveMode.OC);
                currentTime += switchTime;

                waypoints.Add(new PlanWaypoint
                {
                    TimeSeconds = currentTime,
                    Depth = currentDepth,
                    Gas = currentGas,
                    IsUserEntered = false,
                    SegmentType = SegmentType.GasSwitch
                });
            }

            // Try to ascend to next stop
            if (CanAscendTo(state, currentDepth, nextStop, currentGas,
                    surfacePressure, salinity, avgDepth, bottomTime))
            {
                // Clear to ascend
                var ascentTime = CalculateAscentTime(currentDepth, nextStop, avgDepth, bottomTime);
                AddInterpolatedSegment(state, currentDepth, nextStop,
                    ascentTime, currentGas, surfacePressure, salinity);

                currentTime += ascentTime;
                currentDepth = nextStop;

                waypoints.Add(new PlanWaypoint
                {
                    TimeSeconds = currentTime,
                    Depth = currentDepth,
                    Gas = currentGas,
                    IsUserEntered = false,
                    SegmentType = SegmentType.Ascent
                });

                stopIndex--;
            }
            else
            {
                // Need to wait at current stop
                var stopTime = CalculateStopTime(state, currentDepth, nextStop,
                    currentGas, surfacePressure, salinity, avgDepth, bottomTime);

                if (stopTime > 0)
                {
                    var pressure = Zhl16CAlgorithm.DepthToPressure(currentDepth, salinity, surfacePressure);
                    _algorithm.AddSegment(state, pressure, currentGas, stopTime, 0, DiveMode.OC);
                    currentTime += stopTime;

                    stops.Add(new DecoStop(currentDepth.Mm, stopTime));

                    waypoints.Add(new PlanWaypoint
                    {
                        TimeSeconds = currentTime,
                        Depth = currentDepth,
                        Gas = currentGas,
                        IsUserEntered = false,
                        SegmentType = SegmentType.DecoStop
                    });
                }
            }

            // Safety check - prevent infinite loops
            if (currentTime > bottomTime + 24 * 3600) // Max 24 hours of deco
            {
                break;
            }
        }

        return stops;
    }

    /// <summary>
    /// Check if we can ascend from current depth to target without exceeding ceiling.
    /// </summary>
    private bool CanAscendTo(
        DecoState state,
        Depth fromDepth,
        Depth toDepth,
        GasMix gas,
        double surfacePressure,
        int salinity,
        Depth avgDepth,
        int bottomTime)
    {
        // Create a copy of state to test ascent
        var testState = CloneState(state);

        // Simulate the ascent
        var ascentTime = CalculateAscentTime(fromDepth, toDepth, avgDepth, bottomTime);
        var steps = Math.Max(1, ascentTime);
        var depthPerStep = (fromDepth.Mm - toDepth.Mm) / steps;

        for (var i = 0; i < steps; i++)
        {
            var depthMm = fromDepth.Mm - depthPerStep * (i + 1);
            depthMm = Math.Max(depthMm, toDepth.Mm);

            var pressure = Zhl16CAlgorithm.DepthToPressure(new Depth(depthMm), salinity, surfacePressure);
            _algorithm.AddSegment(testState, pressure, gas, 1, 0, DiveMode.OC);

            // Check ceiling
            var tolerance = _algorithm.TissueToleranceCalc(testState, surfacePressure);
            var ceiling = _algorithm.DecoAllowedDepth(tolerance, surfacePressure, salinity);

            if (ceiling.Mm > depthMm)
            {
                return false; // Would exceed ceiling
            }
        }

        return true;
    }

    /// <summary>
    /// Calculate time needed at a stop before we can ascend to next level.
    /// Uses binary search for efficiency.
    /// </summary>
    private int CalculateStopTime(
        DecoState state,
        Depth stopDepth,
        Depth nextStop,
        GasMix gas,
        double surfacePressure,
        int salinity,
        Depth avgDepth,
        int bottomTime)
    {
        const int TimeStep = 60; // 1 minute increments
        const int MaxStopTime = 180 * 60; // 3 hours max

        var pressure = Zhl16CAlgorithm.DepthToPressure(stopDepth, salinity, surfacePressure);
        var totalTime = 0;

        while (totalTime < MaxStopTime)
        {
            // Add a minute of stop time
            _algorithm.AddSegment(state, pressure, gas, TimeStep, 0, DiveMode.OC);
            totalTime += TimeStep;

            // Check if we can now ascend
            if (CanAscendTo(state, stopDepth, nextStop, gas,
                    surfacePressure, salinity, avgDepth, bottomTime))
            {
                // Round up to next minute
                return (totalTime + 59) / 60 * 60;
            }
        }

        return MaxStopTime;
    }

    /// <summary>
    /// Find the best available gas for a given depth.
    /// </summary>
    private DecoGas? FindBestGas(List<DecoGas> gases,
        Depth depth,
        double surfacePressure,
        int salinity)
    {
        var pressure = Zhl16CAlgorithm.DepthToPressure(depth, salinity, surfacePressure);

        // Find highest O2 gas that's safe at this depth
        return gases
            .Where(g => g.MinOperatingDepthMm <= depth.Mm && g.MaxOperatingDepthMm >= depth.Mm)
            .OrderByDescending(g => g.Mix.GetO2Permille())
            .FirstOrDefault();
    }

    /// <summary>
    /// Add segment with interpolation for depth changes.
    /// </summary>
    private void AddInterpolatedSegment(
        DecoState state,
        Depth fromDepth,
        Depth toDepth,
        int timeSeconds,
        GasMix gas,
        double surfacePressure,
        int salinity)
    {
        if (timeSeconds <= 0)
        {
            return;
        }

        // For short segments or constant depth, use single segment
        if (timeSeconds <= 10 || fromDepth.Mm == toDepth.Mm)
        {
            var avgPressure = (Zhl16CAlgorithm.DepthToPressure(fromDepth, salinity, surfacePressure) +
                               Zhl16CAlgorithm.DepthToPressure(toDepth, salinity, surfacePressure)) / 2;
            _algorithm.AddSegment(state, avgPressure, gas, timeSeconds, 0, DiveMode.OC);
            return;
        }

        // Interpolate second by second for accuracy
        var depthDelta = toDepth.Mm - fromDepth.Mm;
        for (var t = 0; t < timeSeconds; t++)
        {
            var currentDepthMm = fromDepth.Mm + depthDelta * t / timeSeconds;
            var pressure = Zhl16CAlgorithm.DepthToPressure(new Depth(currentDepthMm), salinity, surfacePressure);
            _algorithm.AddSegment(state, pressure, gas, 1, 0, DiveMode.OC);
        }
    }

    /// <summary>
    /// Calculate ascent time between two depths.
    /// </summary>
    private int CalculateAscentTime(Depth fromDepth,
        Depth toDepth,
        Depth avgDepth,
        int bottomTime)
    {
        if (toDepth.Mm >= fromDepth.Mm)
        {
            return 0;
        }

        var rate = AscentCalculator.AscentVelocity(fromDepth, avgDepth, bottomTime, _settings);
        if (rate <= 0)
        {
            rate = 150; // Default 9m/min
        }

        return (int)Math.Ceiling((double)(fromDepth.Mm - toDepth.Mm) / rate);
    }

    private static bool IsSameGas(GasMix a, GasMix b)
    {
        return a.GetO2Permille() == b.GetO2Permille() &&
               a.GetHePermille() == b.GetHePermille();
    }

    private DecoState CloneState(DecoState source)
    {
        var clone = new DecoState
        {
            GuidingTissueIndex = source.GuidingTissueIndex,
            GfLowPressureThisDive = source.GfLowPressureThisDive,
            IcdWarning = source.IcdWarning
        };

        Array.Copy(source.TissueN2Sat, clone.TissueN2Sat, 16);
        Array.Copy(source.TissueHeSat, clone.TissueHeSat, 16);
        Array.Copy(source.ToleratedByTissue, clone.ToleratedByTissue, 16);
        Array.Copy(source.TissueInertGasSat, clone.TissueInertGasSat, 16);
        Array.Copy(source.BuehlmannInertGasA, clone.BuehlmannInertGasA, 16);
        Array.Copy(source.BuehlmannInertGasB, clone.BuehlmannInertGasB, 16);

        return clone;
    }
}

/// <summary>
/// A waypoint in the input dive profile.
/// </summary>
public sealed class DiveWaypoint
{
    public required int TimeSeconds { get; init; }
    public required Depth Depth { get; init; }
    public required GasMix Gas { get; init; }
}

/// <summary>
/// A gas available for decompression.
/// </summary>
public sealed class DecoGas
{
    public required GasMix Mix { get; init; }

    /// <summary>Minimum operating depth in mm (for hypoxic mixes)</summary>
    public int MinOperatingDepthMm { get; init; }

    /// <summary>Maximum operating depth in mm (based on PO2 limit)</summary>
    public required int MaxOperatingDepthMm { get; init; }

    /// <summary>
    /// Create a deco gas with automatic MOD calculation.
    /// </summary>
    public static DecoGas Create(GasMix mix,
        double maxPo2 = 1.6,
        double surfacePressure = 1.013,
        int salinity = 10300)
    {
        // MOD = (maxPO2 / fO2 - surfacePressure) / specificWeight * 1000
        var fO2 = mix.GetO2Permille() / 1000.0;
        var specificWeight = salinity * 0.981 / 100000.0;
        var modMm = (int)((maxPo2 / fO2 - surfacePressure) / specificWeight * 1000);

        // For hypoxic mixes, calculate minimum depth
        var minDepthMm = 0;
        if (fO2 < 0.18) // Hypoxic threshold
        {
            var minPo2 = 0.16;
            minDepthMm = (int)((minPo2 / fO2 - surfacePressure) / specificWeight * 1000);
            minDepthMm = Math.Max(0, minDepthMm);
        }

        return new DecoGas
        {
            Mix = mix,
            MinOperatingDepthMm = minDepthMm,
            MaxOperatingDepthMm = Math.Max(0, modMm)
        };
    }
}

/// <summary>
/// Segment type in the dive plan.
/// </summary>
public enum SegmentType
{
    Descent,
    BottomTime,
    Ascent,
    DecoStop,
    GasSwitch,
    SafetyStop
}

/// <summary>
/// A waypoint in the calculated dive plan.
/// </summary>
public sealed class PlanWaypoint
{
    public int TimeSeconds { get; init; }
    public Depth Depth { get; init; }
    public GasMix Gas { get; init; }
    public bool IsUserEntered { get; init; }
    public SegmentType SegmentType { get; init; }

    /// <summary>Runtime at this point (minutes: seconds)</summary>
    public string Runtime => $"{TimeSeconds / 60}:{TimeSeconds % 60:D2}";
}

/// <summary>
/// A decompression stop in the plan.
/// </summary>
public readonly record struct DecoStop(int DepthMm, int TimeSeconds)
{
    public double DepthMeters => DepthMm / 1000.0;
    public int TimeMinutes => (TimeSeconds + 59) / 60; // Round up

    public override string ToString()
    {
        return $"{DepthMeters:F0}m for {TimeMinutes} min";
    }
}

/// <summary>
/// Complete result of a decompression plan calculation.
/// </summary>
public sealed class DecoPlanResult
{
    /// <summary>All waypoints in the dive plan</summary>
    public List<PlanWaypoint> Waypoints { get; } = new();

    /// <summary>All decompression stops</summary>
    public List<DecoStop> DecoStops { get; set; } = new();

    /// <summary>Total decompression time in seconds</summary>
    public int TotalDecoTimeSeconds { get; set; }

    /// <summary>Total dive time in seconds</summary>
    public int TotalDiveTimeSeconds { get; set; }

    /// <summary>Maximum depth reached</summary>
    public Depth MaxDepth { get; set; }

    /// <summary>Bottom time in seconds</summary>
    public int BottomTimeSeconds { get; set; }

    /// <summary>Total Time to Surface from end of bottom time</summary>
    public int TtsSeconds => TotalDiveTimeSeconds - BottomTimeSeconds;

    /// <summary>Check if this is a decompression dive</summary>
    public bool IsDecoDive => DecoStops.Count > 0;

    /// <summary>Get formatted summary</summary>
    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Max Depth: {MaxDepth.ToMeters():F1}m");
        sb.AppendLine($"Bottom Time: {BottomTimeSeconds / 60} min");
        sb.AppendLine($"Total Dive Time: {TotalDiveTimeSeconds / 60} min");

        if (IsDecoDive)
        {
            sb.AppendLine($"Deco Time: {TotalDecoTimeSeconds / 60} min");
            sb.AppendLine($"TTS: {TtsSeconds / 60} min");
            sb.AppendLine("Stops:");
            foreach (var stop in DecoStops)
            {
                sb.AppendLine($"  {stop}");
            }
        }
        else
        {
            sb.AppendLine("No-Deco Dive");
        }

        return sb.ToString();
    }
}