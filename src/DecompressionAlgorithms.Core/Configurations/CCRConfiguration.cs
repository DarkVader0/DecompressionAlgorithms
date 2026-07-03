using System.Runtime.InteropServices;

namespace DecompressionAlgorithms.Core.Configurations;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CCRConfiguration
{
    public ushort BottomSetpoint;
    public ushort DecoSetpoint;
}