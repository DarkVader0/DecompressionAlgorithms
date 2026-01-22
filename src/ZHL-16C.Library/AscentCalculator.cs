namespace ZHL_16C.Library;

/// <summary>
/// Ascent velocity calculation matching Subsurface core/planner.cpp
/// </summary>
public static class AscentCalculator
{
    /// <summary>
    /// Calculate ascent velocity based on current depth.
    /// Matches Subsurface ascent_velocity() function.
    /// </summary>
    /// <param name="depth">Current depth</param>
    /// <param name="avgDepth">Average depth of dive</param>
    /// <param name="bottomTime">Bottom time in seconds</param>
    /// <param name="settings">Planner settings</param>
    /// <returns>Ascent velocity in mm/sec</returns>
    public static int AscentVelocity(Depth depth,
        Depth avgDepth,
        int bottomTime,
        PlannerSettings settings)
    {
        // Use different ascent rates based on depth zones
        // Deeper = slower ascent to reduce bubble formation

        if (depth.Mm > avgDepth.Mm * 1.5)
        {
            // Very deep - use slowest rate (ascrate75)
            return settings.AscentRate75;
        }

        if (depth.Mm > avgDepth.Mm)
        {
            // Deep - use ascrate50
            return settings.AscentRate50;
        }

        if (depth.Mm > 6000) // > 6m
        {
            // Shallow but above 6m - use ascratestops
            return settings.AscentRateStops;
        }

        // Last 6m - use slowest rate
        return settings.AscentRateLast6m;
    }

    /// <summary>
    /// Calculate time to ascend between two depths.
    /// </summary>
    public static int AscentTime(Depth fromDepth,
        Depth toDepth,
        int ascentRate)
    {
        if (ascentRate <= 0)
        {
            return 0;
        }

        var deltaDepth = fromDepth.Mm - toDepth.Mm;
        if (deltaDepth <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)deltaDepth / ascentRate);
    }
}