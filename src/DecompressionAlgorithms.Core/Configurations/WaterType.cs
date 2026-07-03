namespace DecompressionAlgorithms.Core.Configurations;

/// <summary>
/// This class represents the different water types available in the decompression algorithm. The water types are:
/// - Fresh: Freshwater diving, typically in lakes or rivers.
/// - Salt: Saltwater diving, typically in oceans or seas.
/// - EN13319: A specific water type defined by the EN13319 standard, which is a European standard for diving equipment and accessories, including depth gauges and dive computers.
/// </summary>
public enum WaterType : byte
{
    Fresh,
    Salt,
    EN13319
}