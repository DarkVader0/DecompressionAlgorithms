using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Helpers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TissueCompartment
{
    public double PN2;
    public double PHe;
    public double PInertGas => PN2 + PHe;
}