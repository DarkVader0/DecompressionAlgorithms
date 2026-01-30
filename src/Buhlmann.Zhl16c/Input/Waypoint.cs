using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Waypoint
{
    public uint DepthMm;
    public int DurationSeconds;
    public sbyte CylinderIndex;
}