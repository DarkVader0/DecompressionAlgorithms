namespace ZHL_16C.Library;

/// <summary>
/// Physical constants used in decompression calculations.
/// Matches Subsurface implementation from core/units.h and core/deco.cpp
/// </summary>
public static class PhysicalConstants
{
    /// <summary>Oxygen in air:  209 permille (20.9%)</summary>
    public const int O2InAirPermille = 209;

    /// <summary>Nitrogen in air: 781 permille (78.1%)</summary>
    public const int N2InAirPermille = 781;

    /// <summary>Nitrogen fraction in air as decimal (0.79)</summary>
    public const double NitrogenFraction = 0.79;

    /// <summary>O2 density in mg/L at STP</summary>
    public const int O2DensityMgPerLiter = 1331;

    /// <summary>N2 density in mg/L at STP</summary>
    public const int N2DensityMgPerLiter = 1165;

    /// <summary>He density in mg/L at STP</summary>
    public const int HeDensityMgPerLiter = 166;

    /// <summary>
    /// Water vapor pressure in bar using Bühlmann value (Rq = 1.0)
    /// </summary>
    public const double WaterVaporPressure = 0.0627;

    /// <summary>Seawater salinity in g/10L</summary>
    public const int SeawaterSalinity = 10300;

    /// <summary>EN13319 standard salinity in g/10L</summary>
    public const int En13319Salinity = 10200;

    /// <summary>Brackish water salinity in g/10L</summary>
    public const int BrackishSalinity = 10100;

    /// <summary>Freshwater salinity in g/10L</summary>
    public const int FreshwaterSalinity = 10000;

    /// <summary>Deco stop interval:  3000mm (3 meters)</summary>
    public const double DecoStopsMultiplierMm = 3000.0;

    /// <summary>Standard atmosphere in mbar</summary>
    public const int StandardAtmosphereMBar = 1013;

    /// <summary>ln(2) / 60 - precomputed for half-life calculations</summary>
    public const double Ln2Over60 = 1.155245301e-02;

    /// <summary>Conservatism correction factor (1.0 = no correction)</summary>
    public const double SubsurfaceConservatismFactor = 1.0;

    /// <summary>0°C in millikelvin</summary>
    public const int ZeroCelsiusInMKelvin = 273150;
}