namespace DecompressionAlgorithms.Core.Configurations;

/// <summary>
/// This class represents the different dive modes available in the decompression algorithm. The dive modes are:
/// - OC: Open Circuit diving, where the diver breathes from a tank and exhales into the water.
/// - CCR: Closed Circuit Rebreather diving, where the diver breathes from a rebreather that recycles the exhaled gas.
/// - PSCR: Passive Semi-Closed Rebreather diving, where the diver breathes from a rebreather that allows some gas to escape while recycling the rest.
/// </summary>
public enum DiveMode : byte
{
    OC,
    CCR,
    PSCR
}