using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StopSettings
{
    public bool LastStopAt6m;
    public bool SafetyStop;
    public ushort MinSwitchDurationSec;
    public byte ProblemSolvingTimeMin;
    public bool DoO2Breaks;
    public bool SwitchAtRequiredStop;
}