namespace ZHL_16C.Library;

public static class Coefficients
{
    /// <summary>
    /// Number of tissue compartments in ZHL-16C model.
    /// </summary>
    public const int TissueCount = 16;

    /// <summary>
    /// Nitrogen "a" coefficients (bar) for each tissue compartment.
    /// Used in M-value calculation:  M = a + P_amb / b
    /// </summary>
    public static readonly double[] N2A =
    {
        1.1696, 1.0, 0.8618, 0.7562,
        0.62, 0.5043, 0.441, 0.4,
        0.375, 0.35, 0.3295, 0.3065,
        0.2835, 0.261, 0.248, 0.2327
    };

    /// <summary>
    /// Nitrogen "b" coefficients (dimensionless) for each tissue compartment.
    /// Used in M-value calculation: M = a + P_amb / b
    /// </summary>
    public static readonly double[] N2B =
    {
        0.5578, 0.6514, 0.7222, 0.7825,
        0.8126, 0.8434, 0.8693, 0.8910,
        0.9092, 0.9222, 0.9319, 0.9403,
        0.9477, 0.9544, 0.9602, 0.9653
    };

    /// <summary>
    /// Nitrogen half-times in minutes for each tissue compartment.
    /// Range: 5 min (fastest) to 635 min (slowest)
    /// </summary>
    public static readonly double[] N2HalfLife =
    {
        5.0, 8.0, 12.5, 18.5,
        27.0, 38.3, 54.3, 77.0,
        109.0, 146.0, 187.0, 239.0,
        305.0, 390.0, 498.0, 635.0
    };

    /// <summary>
    /// Helium "a" coefficients (bar) for each tissue compartment.
    /// Used for trimix/heliox calculations.
    /// </summary>
    public static readonly double[] HeA =
    {
        1.6189, 1.383, 1.1919, 1.0458,
        0.922, 0.8205, 0.7305, 0.6502,
        0.595, 0.5545, 0.5333, 0.5189,
        0.5181, 0.5176, 0.5172, 0.5119
    };

    /// <summary>
    /// Helium "b" coefficients (dimensionless) for each tissue compartment.
    /// </summary>
    public static readonly double[] HeB =
    {
        0.4770, 0.5747, 0.6527, 0.7223,
        0.7582, 0.7957, 0.8279, 0.8553,
        0.8757, 0.8903, 0.8997, 0.9073,
        0.9122, 0.9171, 0.9217, 0.9267
    };

    /// <summary>
    /// Helium half-times in minutes for each tissue compartment.
    /// Helium diffuses ~2.65x faster than nitrogen.
    /// </summary>
    public static readonly double[] HeHalfLife =
    {
        1.88, 3.02, 4.72, 6.99,
        10.21, 14.48, 20.53, 29.11,
        41.20, 55.19, 70.69, 90.34,
        115.29, 147.42, 188.24, 240.03
    };

    /// <summary>
    /// Pre-calculated N2 exposure factors for 1-second intervals.
    /// Formula: 1 - exp(-1 / (halflife * 60) * ln(2))
    /// Used for performance optimization when calculating second-by-second.
    /// </summary>
    public static readonly double[] N2FactorOneSecond =
    {
        2.30782347297664E-003, 1.44301447809736E-003, 9.23769302935806E-004, 6.24261986779007E-004,
        4.27777107246730E-004, 3.01585140931371E-004, 2.12729727268379E-004, 1.50020603047807E-004,
        1.05980191127841E-004, 7.91232600646508E-005, 6.17759153688224E-005, 4.83354552742732E-005,
        3.78761777920511E-005, 2.96212356654113E-005, 2.31974277413727E-005, 1.81926738960225E-005
    };

    /// <summary>
    /// Pre-calculated He exposure factors for 1-second intervals.
    /// </summary>
    public static readonly double[] HeFactorOneSecond =
    {
        6.12608039419837E-003, 3.81800836683133E-003, 2.44456078654209E-003, 1.65134647076792E-003,
        1.13084424730725E-003, 7.97503165599123E-004, 5.62552521860549E-004, 3.96776399429366E-004,
        2.80360036664540E-004, 2.09299583354805E-004, 1.63410794820518E-004, 1.27869320250551E-004,
        1.00198406028040E-004, 7.83611475491108E-005, 6.13689891868496E-005, 4.81280465299827E-005
    };
}