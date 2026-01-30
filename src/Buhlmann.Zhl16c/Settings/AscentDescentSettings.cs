using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AscentDescentSettings
{
    public ushort DescentRateMmSec;
    public ushort AscentRate75MmSec;
    public ushort AscentRate50MmSec;
    public ushort AscentRateStopsMmSec;
    public ushort AscentRateLast6mMmSec;
}