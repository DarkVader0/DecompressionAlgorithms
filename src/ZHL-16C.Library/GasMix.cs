namespace ZHL_16C.Library;

/// <summary>
/// Gas component enumeration matching Subsurface's core/gash
/// </summary>
public enum GasComponent
{
    N2,
    He,
    O2
}

/// <summary>
/// Gas type classification matching Subsurface's gastype enum
/// </summary>
public enum GasType
{
    Air,
    Nitrox,
    HypoxicTrimix, // O2 <= 18%
    NormoxicTrimix, // O2 18-23%
    HyperoxicTrimix, // O2 > 23% with He
    Oxygen // O2 >= 98%
}

/// <summary>
/// Gas mixture stored in permille (parts per thousand).
/// Matches Subsurface core/gas.h gasmix struct.
/// </summary>
public readonly record struct GasMix(Fraction O2, Fraction He)
{
    /// <summary>Nitrogen fraction (calculated:  1000 - O2 - He)</summary>
    public Fraction N2 => new(1000 - GetO2Permille() - He.Permille);

    // Predefined mixes
    public static GasMix Air => new(new Fraction(0), new Fraction(0));
    public static GasMix Invalid => new(new Fraction(-1), new Fraction(-1));
    public static GasMix Oxygen => new(new Fraction(1000), new Fraction(0));

    public bool IsAir =>
        GetHePermille() == 0 &&
        (O2.Permille == 0 || (GetO2Permille() >= 208 && GetO2Permille() <= 210));

    public bool IsValid => O2.Permille >= 0;

    public GasType GasType
    {
        get
        {
            if (IsAir)
            {
                return GasType.Air;
            }

            if (GetO2Permille() >= 980)
            {
                return GasType.Oxygen;
            }

            if (GetHePermille() == 0)
            {
                return GetO2Permille() >= 230 ? GasType.Nitrox : GasType.Air;
            }

            if (GetO2Permille() <= 180)
            {
                return GasType.HypoxicTrimix;
            }

            return GetO2Permille() <= 230 ? GasType.NormoxicTrimix : GasType.HyperoxicTrimix;
        }
    }

    public string Name
    {
        get
        {
            if (!IsValid)
            {
                return "Invalid";
            }

            if (IsAir)
            {
                return "Air";
            }

            if (GetHePermille() == 0 && GetO2Permille() < 1000)
            {
                return $"NX{(GetO2Permille() + 5) / 10}";
            }

            if (GetHePermille() == 0 && GetO2Permille() == 1000)
            {
                return "Oxygen";
            }

            return $"{(GetO2Permille() + 5) / 10}/{(GetHePermille() + 5) / 10}";
        }
    }

    /// <summary>Get O2 in permille, defaulting to air (209) if 0</summary>
    public int GetO2Permille()
    {
        return O2.Permille == 0 ? PhysicalConstants.O2InAirPermille : O2.Permille;
    }

    /// <summary>Get He in permille</summary>
    public int GetHePermille()
    {
        return He.Permille;
    }

    /// <summary>Get N2 in permille</summary>
    public int GetN2Permille()
    {
        return 1000 - GetO2Permille() - GetHePermille();
    }

    public static GasMix Nitrox(int o2Percent)
    {
        return new GasMix(new Fraction(o2Percent * 10), new Fraction(0));
    }

    public static GasMix Trimix(int o2Percent, int hePercent)
    {
        return new GasMix(new Fraction(o2Percent * 10), new Fraction(hePercent * 10));
    }
}

/// <summary>
/// Partial pressures of gases at a given depth/pressure.
/// Matches Subsurface core/gas.h gas_pressures struct.
/// </summary>
public record struct GasPressures(
    double O2,
    double N2,
    double He);