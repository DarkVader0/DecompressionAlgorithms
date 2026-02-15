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
        var totalRuntimeSec = 0;
        stringBuilder.AppendLine($"{"depth",8}  {"duration",10}  {"runtime",10}  {"gas",12}");

        for (var i = 0; i < SegmentCount; i++)
        {
            ref var seg = ref Segments[i];

            var startDepthM = seg.DepthStartMm / 1000;
            var endDepthM = seg.DepthEndMm / 1000;
            var durationSec = seg.RuntimeEndSec - seg.RuntimeStartSec;
            totalRuntimeSec = seg.RuntimeEndSec;

            var durationMin = (durationSec + 30) / 60;
            var runtimeMin = (totalRuntimeSec + 30) / 60;

            if (startDepthM != endDepthM)
            {
                if (startDepthM == 0 && endDepthM > 0)
                {
                    stringBuilder.AppendLine($"{endDepthM + "m",8}  {durationMin + "min",10}  {runtimeMin + "min",10}  EAN25");
                }
                else if (endDepthM == 0)
                {
                    stringBuilder.AppendLine($"{"0m",8}  {durationMin + "min",10}  {runtimeMin + "min",10}  EAN25");
                }

                continue;
            }

            stringBuilder.AppendLine($"{endDepthM + "m",8}  {durationMin + "min",10}  {runtimeMin + "min",10}  EAN25");
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"Runtime: {(totalRuntimeSec + 30) / 60}min ({totalRuntimeSec}s)");
        
        return stringBuilder.ToString();
    }
}