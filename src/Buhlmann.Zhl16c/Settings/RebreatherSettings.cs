using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RebreatherSettings
{
    public DiveMode DiveMode;
    public ushort SetpointMbar;
    public ushort PscrRatio;
    public bool DoBailout;
    public ushort BailoutSacMl;
    public ushort BailoutSwitchTimeSec;
}