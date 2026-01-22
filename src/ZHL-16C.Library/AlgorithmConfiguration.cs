namespace ZHL_16C.Library;

public sealed class AlgorithmConfiguration
{
    /// <summary>
    /// Safety multiplier for inert gas accumulation (saturation).
    /// Default: 1.0 (100% - no modification)
    /// Values > 1.0 increase conservatism during on-gassing.
    /// </summary>
    public double SaturationMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Safety multiplier for inert gas depletion (desaturation).
    /// Default: 1.0 (100% - no modification)
    /// Values &lt; 1.0 increase conservatism during off-gassing.
    /// </summary>
    public double DesaturationMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Depth of last decompression stop in meters.
    /// Common values: 3, 6 meters.
    /// </summary>
    public int LastDecoStopInMeters { get; set; } = 6;

    /// <summary>
    /// Gradient Factor High (at surface).
    /// Range: 0.0 to 1.0 (commonly expressed as percentage 0-100%)
    /// Default: 0.7 (70%)
    /// </summary>
    public double GfHigh { get; set; } = 0.7;

    /// <summary>
    /// Gradient Factor Low (at bottom/start of deco calculation).
    /// Range: 0.0 to 1.0 (commonly expressed as percentage 0-100%)
    /// Default: 0.5 (50%)
    /// </summary>
    public double GfLow { get; set; } = 0.5;

    /// <summary>
    /// GF Low position minimum below surface (in bar).
    /// Used to determine where GF Low applies.
    /// Default: 1.0
    /// </summary>
    public double GfLowPositionMin { get; set; } = 1.0;
}