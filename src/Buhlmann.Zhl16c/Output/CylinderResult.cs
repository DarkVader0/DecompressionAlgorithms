using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Output;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CylinderResult
{
    public int EndPressureMbar;
    public int GasUsedMl;
    public int MinGasRequiredMl;
}