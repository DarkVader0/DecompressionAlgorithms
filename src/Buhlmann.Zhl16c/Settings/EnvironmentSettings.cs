using System.Runtime.InteropServices;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EnvironmentSettings
{
    public ushort SurfacePressureMbar;
    public WaterType WaterType;
}