using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Helpers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct GasMix
{
    public readonly ushort O2Permille;
    public readonly ushort HePermille;

    public GasMix(ushort o2Permille, ushort hePermille)
    {
        O2Permille = o2Permille;
        HePermille = hePermille;
    }

    public ushort N2Permille => (ushort)(1000 - O2Permille - HePermille);

    public bool IsAir => O2Permille is >= 209 and <= 211 && HePermille == 0;

    public bool IsOxygen => O2Permille == 1000;

    public bool IsNitrox => HePermille == 0 && O2Permille > 211;

    public bool IsTrimix => HePermille > 0;

    public static GasMix Air => new(210, 0);
    public static GasMix Oxygen => new(100, 0);
}