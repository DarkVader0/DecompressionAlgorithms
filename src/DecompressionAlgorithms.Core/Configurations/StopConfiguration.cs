using System.Runtime.InteropServices;

namespace DecompressionAlgorithms.Core.Configurations;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StopConfiguration
{
    public ushort LastStopDepth;
    public ushort DecoStepSize;
    public ushort StopTimeMinimumSeconds;
    public bool SafetyStop;
    public bool DoO2Breaks;
    public ushort O2BreakDurationSeconds;
    public ushort BackGasBreakDurationSeconds;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GasConfiguration
{
    public bool IsO2Narcotic;
    public float DecoPPO2;
    public float BottomPPO2;
    public ushort TimeToSwitchGasSeconds;
}