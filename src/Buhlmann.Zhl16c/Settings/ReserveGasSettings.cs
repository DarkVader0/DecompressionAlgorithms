using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ReserveGasSettings
{
    public int ReservePressureMbar;
    public byte SacStressFactor;
    public byte TeamSize;
    public bool CalculateMinGas;
}