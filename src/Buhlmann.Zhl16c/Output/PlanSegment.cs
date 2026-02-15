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

    private int DurationSec => RuntimeEndSec - RuntimeStartSec;

    public override string ToString()
    {
        var icon = SegmentType switch
        {
            SegmentType.Descent => "➘",
            SegmentType.Bottom => "➙",
            SegmentType.Ascent => "➚",
            SegmentType.DecoStop => "-",
            SegmentType.GasSwitch => "⇄",
            SegmentType.SafetyStop => "■",
            _ => "?"
        };

        var durationMin = (DurationSec + 30) / 60;
        var runtimeMin = (RuntimeEndSec + 30) / 60;
        var depthM = DepthEndMm / 1000;

        return $" {icon} {depthM + "m",5}  {durationMin + "min",8}  {runtimeMin + "min",8}";
    }
}