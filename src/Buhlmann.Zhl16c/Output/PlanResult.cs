using System.Runtime.InteropServices;
using System.Text;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Output;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlanResult
{
    public PlanSegment[] Segments;
    public ushort SegmentCount;
    public CylinderResult[] CylinderResults;
    public ushort CylinderCount;
    public int TimeTotalSec;
    public int BottomTimeSec;
    public int DecoTimeSec;
    public int MaxDepthMm;
    public int AvgDepthMm;
    public ushort CnsPercent;
    public ushort OtuTotal;
    public PlanError Error;

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        var steps = SegmentCount;

        for (var i = 0; i < steps; i++)
        {
            stringBuilder.AppendLine(Segments[i].ToString());
        }

        return stringBuilder.ToString();
    }
}