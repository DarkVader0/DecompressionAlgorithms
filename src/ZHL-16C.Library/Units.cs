namespace ZHL_16C.Library;

/// <summary>
/// Depth value stored in millimeters for precision.
/// Matches Subsurface core/units.h depth_t struct.
/// </summary>
public readonly record struct Depth(int Mm) : IComparable<Depth>
{
    // IComparable implementation
    public int CompareTo(Depth other)
    {
        return Mm.CompareTo(other.Mm);
    }

    public static Depth FromMeters(double m)
    {
        return new Depth((int)(m * 1000));
    }

    public static Depth FromFeet(double ft)
    {
        return new Depth((int)(ft * 304.8));
    }

    public double ToMeters()
    {
        return Mm / 1000.0;
    }

    public double ToFeet()
    {
        return Mm * 0.00328084;
    }

    // Arithmetic operators
    public static Depth operator +(Depth a, Depth b)
    {
        return new Depth(a.Mm + b.Mm);
    }

    public static Depth operator -(Depth a, Depth b)
    {
        return new Depth(a.Mm - b.Mm);
    }

    public static Depth operator *(Depth a, int multiplier)
    {
        return new Depth(a.Mm * multiplier);
    }

    public static Depth operator /(Depth a, int divisor)
    {
        return new Depth(a.Mm / divisor);
    }

    // Comparison operators
    public static bool operator >(Depth a, Depth b)
    {
        return a.Mm > b.Mm;
    }

    public static bool operator <(Depth a, Depth b)
    {
        return a.Mm < b.Mm;
    }

    public static bool operator >=(Depth a, Depth b)
    {
        return a.Mm >= b.Mm;
    }

    public static bool operator <=(Depth a, Depth b)
    {
        return a.Mm <= b.Mm;
    }

    public override string ToString()
    {
        return $"{ToMeters():F1}m";
    }
}

/// <summary>
/// Pressure value stored in millibar for precision.
/// </summary>
public readonly record struct Pressure(int Mbar)
{
    public static Pressure OneAtmosphere => new(1013);

    public static Pressure FromBar(double bar)
    {
        return new Pressure((int)(bar * 1000));
    }

    public static Pressure FromAtm(double atm)
    {
        return new Pressure((int)(atm * 1013.25));
    }

    public static Pressure FromPsi(double psi)
    {
        return new Pressure((int)(psi / 14.5037738 * 1000));
    }

    public double ToBar()
    {
        return Mbar / 1000.0;
    }

    public double ToAtm()
    {
        return Mbar / 1013.25;
    }

    public static Pressure operator +(Pressure a, Pressure b)
    {
        return new Pressure(a.Mbar + b.Mbar);
    }

    public static Pressure operator -(Pressure a, Pressure b)
    {
        return new Pressure(a.Mbar - b.Mbar);
    }
}

/// <summary>
/// Gas fraction stored in permille (parts per thousand).
/// </summary>
public readonly record struct Fraction(int Permille)
{
    public static Fraction FromPercent(double percent)
    {
        return new Fraction((int)(percent * 10));
    }

    public static Fraction FromDecimal(double fraction)
    {
        return new Fraction((int)(fraction * 1000));
    }

    public double ToDecimal()
    {
        return Permille / 1000.0;
    }

    public double ToPercent()
    {
        return Permille / 10.0;
    }
}

/// <summary>
/// Temperature stored in millikelvin.
/// </summary>
public readonly record struct Temperature(uint Mkelvin)
{
    // From Celsius
    public static Temperature FromCelsius(double c)
    {
        return new Temperature((uint)Math.Round(c * 1000 + PhysicalConstants.ZeroCelsiusInMKelvin));
    }

    // From Kelvin
    public static Temperature FromKelvin(double k)
    {
        return new Temperature((uint)Math.Round(k * 1000));
    }

    // From Fahrenheit (matches F_to_mkelvin in units.h)
    public static Temperature FromFahrenheit(double f)
    {
        return new Temperature((uint)Math.Round((f - 32) * 1000 / 1.8 + PhysicalConstants.ZeroCelsiusInMKelvin));
    }

    // To Celsius
    public double ToCelsius()
    {
        return (Mkelvin - PhysicalConstants.ZeroCelsiusInMKelvin) / 1000.0;
    }

    // To Kelvin
    public double ToKelvin()
    {
        return Mkelvin / 1000.0;
    }

    // To Fahrenheit (matches mkelvin_to_F in units.h)
    public double ToFahrenheit()
    {
        return Mkelvin * 9.0 / 5000.0 - 459.670;
    }
}