using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GasSettings
{
    public ushort BottomPo2Mbar;
    public ushort DecoPo2Mbar;
    public uint BottomSacMl;
    public uint DecoSacMl;
    public int BestMixEndMm;
    public bool O2IsNarcotic;
}