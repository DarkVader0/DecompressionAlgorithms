using System.Runtime.InteropServices;

namespace DecompressionAlgorithms.Core.Configurations;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SacConfiguration
{
    public float BottomSacVolumePerMin;
    public float DecoSacVolumePerMin;
}