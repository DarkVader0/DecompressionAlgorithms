using System.Runtime.InteropServices;

namespace DecompressionAlgorithms.Core.Configurations;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EnvironmentConfiguration
{
    public ushort SurfacePressure; 
    public WaterType WaterType;
}