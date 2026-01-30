using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Output;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlanSegment
{
    public uint RuntimeStartSec;
    public uint RuntimeEndSec;
    public uint DepthStartMm;
    public uint DepthEndMm;
    public byte CylinderIndex;
    public int GasUsedMl;
    public SegmentType SegmentType;
    public ushort SetpointMbar;
    public DiveMode DiveMode;
}