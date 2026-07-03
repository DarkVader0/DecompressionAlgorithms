using DecompressionAlgorithms.Core.Configurations;

namespace DecompressionAlgorithms.BuhlmannZhl16c.Configurations;

public class BuhlmannAlgorithmConfiguration : AlgorithmConfiguration
{
    public ushort GFLow { get; set; }
    public ushort GFHigh { get; set; }
}