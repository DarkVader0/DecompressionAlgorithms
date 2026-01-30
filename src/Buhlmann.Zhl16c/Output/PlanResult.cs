using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Output;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlanResult
{
    public PlanSegment[] Segments;
    public ushort SegmentCount;
    public CylinderResult[] CylinderResults;
    public ushort CylinderCount;
    public uint TimeTotalSec;
    public uint BottomTimeSec;
    public uint DecoTimeSec;
    public uint MaxDepthMm;
    public uint AvgDepthMm;
    public ushort CnsPercent;
    public ushort OtuTotal;
    public PlanError Error;
}