using System.Runtime.InteropServices;

namespace DecompressionAlgorithms.Core.Configurations;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MovementConfiguration
{
    public ushort DescentRateDepthPerMinute;
    public ushort AscentRate75DepthPerMinute;
    public ushort AscentRate50DepthPerMinute;
    public ushort AscentRateStopsDepthPerMinute;
    public ushort AscentRateLast6mDepthPerMinute;
}