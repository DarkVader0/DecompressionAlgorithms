using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Output;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlanSegment
{
    public int RuntimeStartSec;
    public int RuntimeEndSec;
    public int DepthStartMm;
    public int DepthEndMm;
    public byte CylinderIndex;
    public int GasUsedMl;
    public SegmentType SegmentType;
    public ushort SetpointMbar;
    public DiveMode DiveMode;
}