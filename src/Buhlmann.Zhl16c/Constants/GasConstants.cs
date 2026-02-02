namespace Buhlmann.Zhl16c.Constants;

public static class GasConstants
{
    public static ushort O2InAirPermille => 209;
    public static ushort HeInAirPermille => 0;
    public static ushort N2InAirPermille => 781;
    public static ushort FreshWaterSalinity => 10000;
    public static ushort SaltWaterSalinity => 10300;
    public static ushort En13319Salinity => 10200;
    public static ushort StandardPressureMbar => 1013;
    public static ushort BackGasBreakO2DurationSeconds => 12 * 60;
    public static ushort BackGasBreakDurationSeconds => 6 * 60;
}