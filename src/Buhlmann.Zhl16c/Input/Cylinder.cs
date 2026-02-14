using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cylinder
{
    public ushort O2Permille;
    public ushort HePermille;
    public int SizeMl;
    public int StartPressureMbar;
    public CylinderUse Use;
}