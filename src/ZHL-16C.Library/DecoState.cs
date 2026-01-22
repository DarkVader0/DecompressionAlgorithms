namespace ZHL_16C.Library;

/// <summary>
/// Represents the current decompression state for all 16 tissue compartments.
/// Matches Subsurface core/deco.h deco_state struct.
/// </summary>
public sealed class DecoState
{
    private const int TissueCount = 16;

    /// <summary>Current N2 saturation pressure (bar) for each tissue</summary>
    public double[] TissueN2Sat { get; } = new double[TissueCount];

    /// <summary>Current He saturation pressure (bar) for each tissue</summary>
    public double[] TissueHeSat { get; } = new double[TissueCount];

    /// <summary>Tolerated ambient pressure by each tissue</summary>
    public double[] ToleratedByTissue { get; } = new double[TissueCount];

    /// <summary>Total inert gas saturation (N2 + He)</summary>
    public double[] TissueInertGasSat { get; } = new double[TissueCount];

    /// <summary>Combined Bühlmann "a" coefficient (weighted by gas loading)</summary>
    public double[] BuehlmannInertGasA { get; } = new double[TissueCount];

    /// <summary>Combined Bühlmann "b" coefficient (weighted by gas loading)</summary>
    public double[] BuehlmannInertGasB { get; } = new double[TissueCount];

    /// <summary>Index of controlling (leading) tissue compartment</summary>
    public int GuidingTissueIndex { get; set; } = -1;

    /// <summary>Pressure at which GF Low applies for this dive</summary>
    public double GfLowPressureThisDive { get; set; }

    /// <summary>Total decompression time in seconds</summary>
    public int DecoTime { get; set; }

    /// <summary>Isobaric Counter Diffusion warning flag</summary>
    public bool IcdWarning { get; set; }
}