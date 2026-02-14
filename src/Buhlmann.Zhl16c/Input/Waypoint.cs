using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Waypoint
{
    public int DepthMm;
    public int DurationSeconds;
    public sbyte CylinderIndex;
}