using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DecoSettings
{
    public byte GFLow;
    public byte GFHigh;
}