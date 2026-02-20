using System.Runtime.InteropServices;
using System.Text;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Input;

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
        var sb = new StringBuilder();
        sb.AppendLine($"{"",3}{"depth",5}  {"duration",8}  {"runtime",8}");

        var i = 0;
        while (i < SegmentCount)
        {
            ref var seg = ref Segments[i];

            if (seg.DepthStartMm > seg.DepthEndMm)
            {
                var ascentStartSec = seg.RuntimeStartSec;
                var ascentEndDepthMm = seg.DepthEndMm;
                var ascentEndSec = seg.RuntimeEndSec;

                while (i + 1 < SegmentCount &&
                       Segments[i + 1].DepthStartMm > Segments[i + 1].DepthEndMm)
                {
                    i++;
                    ascentEndDepthMm = Segments[i].DepthEndMm;
                    ascentEndSec = Segments[i].RuntimeEndSec;
                }

                var durationMin = (ascentEndSec - ascentStartSec + 30) / 60;
                var runtimeMin = (ascentEndSec + 30) / 60;
                var depthM = ascentEndDepthMm / 1000;

                sb.AppendLine($" ➚ {depthM + "m",5}  {durationMin + "min",8}  {runtimeMin + "min",8}");
            }
            else
            {
                var icon = seg.DepthStartMm < seg.DepthEndMm ? "➘" :
                    seg.SegmentType == SegmentType.Bottom ? "➙" : seg.SegmentType ==  SegmentType.GasSwitch ? "⇄" : "-";

                var durationMin = (seg.RuntimeEndSec - seg.RuntimeStartSec + 30) / 60;
                var runtimeMin = (seg.RuntimeEndSec + 30) / 60;
                var depthM = seg.DepthEndMm / 1000;

                sb.AppendLine($" {icon} {depthM + "m",5}  {durationMin + "min",8}  {runtimeMin + "min",8}");
            }

            i++;
        }

        sb.AppendLine();
        sb.AppendLine($"Runtime: {(TimeTotalSec + 30) / 60}min ({TimeTotalSec}s)");
        sb.AppendLine($"Max depth: {MaxDepthMm / 1000}m");

        return sb.ToString();
    }
}