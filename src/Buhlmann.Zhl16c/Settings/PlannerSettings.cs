using System.Runtime.InteropServices;

namespace Buhlmann.Zhl16c.Settings;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlannerSettings
{
    public DecoSettings Deco;
    public GasSettings Gas;
    public ReserveGasSettings Reserve;
    public AscentDescentSettings AscentDescent;
    public StopSettings Stops;
    public RebreatherSettings Rebreather;
    public EnvironmentSettings Environment;
}