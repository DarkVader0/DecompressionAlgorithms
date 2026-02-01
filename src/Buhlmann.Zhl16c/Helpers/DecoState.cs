using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Coefficients;
using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Helpers;

/// <summary>
/// Tracks decompression state for all 16 tissue compartments.
/// This is the heart of ZHL-16C calculations.
/// </summary>
public unsafe struct DecoState
{
    /// <summary>
    /// N2 tissue saturation for each compartment (bar).
    /// </summary>
    public fixed double TissueN2Sat[16];

    /// <summary>
    /// He tissue saturation for each compartment (bar).
    /// </summary>
    public fixed double TissueHeSat[16];

    /// <summary>
    /// Ceiling pressure at GF Low for this dive (bar).
    /// </summary>
    public double GfLowPressureThisDive;

    /// <summary>
    /// Index of the leading (controlling) tissue compartment.
    /// </summary>
    public int LeadingTissueIndex;

    /// <summary>
    /// Initialize tissues to surface saturation.
    /// </summary>
    /// <param name="surfacePressureBar">Surface pressure in bar</param>
    public void Clear(double surfacePressureBar)
    {
        // Ambient N2 pressure = (surface - water vapor) * N2 fraction
        var ambientN2 = (surfacePressureBar - BuhlmannCoefficients.WaterVaporPressure)
            * GasConstants.N2InAirPermille / 1000.0;

        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            TissueN2Sat[i] = ambientN2;
            TissueHeSat[i] = 0.0;
        }

        GfLowPressureThisDive = surfacePressureBar;
        LeadingTissueIndex = 0;
    }

    /// <summary>
    /// Add a segment at constant depth to the deco calculation.
    /// Updates tissue loading using Schreiner equation.
    /// </summary>
    /// <param name="pressureBar">Ambient pressure in bar</param>
    /// <param name="gasMix">Gas mix being breathed</param>
    /// <param name="periodSeconds">Duration in seconds</param>
    /// <param name="diveMode">Dive mode (OC, CCR, etc.)</param>
    /// <param name="setpointMbar">CCR setpoint in mbar (0 for OC)</param>
    public void AddSegment(
        double pressureBar,
        GasMix gasMix,
        int periodSeconds,
        DiveMode diveMode,
        int setpointMbar)
    {
        ref readonly var coeff = ref BuhlmannCoefficients.ZHL16C;

        var ambientPressure = pressureBar - BuhlmannCoefficients.WaterVaporPressure;

        double ppN2, ppHe;

        if (diveMode == DiveMode.CCR && setpointMbar > 0)
        {
            var ppO2 = setpointMbar / 1000.0;
            if (ppO2 > pressureBar)
            {
                ppO2 = pressureBar;
            }

            var remainingPressure = ambientPressure - ppO2;
            if (gasMix.O2Permille < 1000)
            {
                ppHe = remainingPressure * gasMix.HePermille / (1000 - gasMix.O2Permille);
                ppN2 = remainingPressure - ppHe;
            }
            else
            {
                ppHe = 0;
                ppN2 = 0;
            }
        }
        else
        {
            ppN2 = ambientPressure * gasMix.N2Permille / 1000.0;
            ppHe = ambientPressure * gasMix.HePermille / 1000.0;
        }

        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            var n2Factor = coeff.Factor(periodSeconds, i, false);
            var heFactor = coeff.Factor(periodSeconds, i, true);

            TissueN2Sat[i] += (ppN2 - TissueN2Sat[i]) * n2Factor;
            TissueHeSat[i] += (ppHe - TissueHeSat[i]) * heFactor;
        }
    }

    /// <summary>
    /// Calculate the ceiling (minimum tolerated ambient pressure) for given GF.
    /// </summary>
    /// <param name="gf">Gradient factor (0.0 - 1.0)</param>
    /// <returns>Ceiling pressure in bar</returns>
    public double CeilingBar(double gf)
    {
        ref readonly var coeff = ref BuhlmannCoefficients.ZHL16C;

        var maxCeiling = 0.0;
        var leadingIdx = 0;

        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            var pInert = TissueN2Sat[i] + TissueHeSat[i];

            double a, b;
            if (pInert > 0)
            {
                a = (coeff.N2A[i] * TissueN2Sat[i] + coeff.HeA[i] * TissueHeSat[i]) / pInert;
                b = (coeff.N2B[i] * TissueN2Sat[i] + coeff.HeB[i] * TissueHeSat[i]) / pInert;
            }
            else
            {
                a = coeff.N2A[i];
                b = coeff.N2B[i];
            }

            var ceiling = (b * pInert - gf * a * b) / ((1.0 - b) * gf + b);

            if (ceiling <= maxCeiling)
            {
                continue;
            }

            maxCeiling = ceiling;
            leadingIdx = i;
        }

        LeadingTissueIndex = leadingIdx;
        return maxCeiling;
    }

    /// <summary>
    /// Calculate the ceiling in mm for given GF and dive context.
    /// Returns 0 if surface is allowed.
    /// </summary>
    /// <param name="gf">Gradient factor (0.0 - 1.0)</param>
    /// <param name="context">Dive context for pressure/depth conversion</param>
    /// <returns>Ceiling depth in mm, or 0 if clear to surface</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint CeilingMm(double gf, DiveContext context)
    {
        var ceilingBar = CeilingBar(gf);
        var ceilingMbar = (uint)(ceilingBar * 1000);

        if (ceilingMbar <= context.SurfacePressureMbar)
        {
            return 0;
        }

        return context.MbarToDepthMm(ceilingMbar);
    }

    /// <summary>
    /// Get total inert gas pressure for a compartment.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetInertPressure(int compartmentIndex)
    {
        return TissueN2Sat[compartmentIndex] + TissueHeSat[compartmentIndex];
    }

    /// <summary>
    /// Create a copy of the current state for trial calculations.
    /// </summary>
    public DecoState Clone()
    {
        var copy = new DecoState();

        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            copy.TissueN2Sat[i] = TissueN2Sat[i];
            copy.TissueHeSat[i] = TissueHeSat[i];
        }

        copy.GfLowPressureThisDive = GfLowPressureThisDive;
        copy.LeadingTissueIndex = LeadingTissueIndex;

        return copy;
    }

    /// <summary>
    /// Copy state from another DecoState (for restoring after trial).
    /// </summary>
    public void CopyFrom(ref DecoState other)
    {
        for (var i = 0; i < BuhlmannCoefficients.CompartmentCount; i++)
        {
            TissueN2Sat[i] = other.TissueN2Sat[i];
            TissueHeSat[i] = other.TissueHeSat[i];
        }

        GfLowPressureThisDive = other.GfLowPressureThisDive;
        LeadingTissueIndex = other.LeadingTissueIndex;
    }

    /// <summary>
    /// Create a new DecoState initialized for surface at given pressure.
    /// </summary>
    public static DecoState CreateAtSurface(double surfacePressureBar)
    {
        var state = new DecoState();
        state.Clear(surfacePressureBar);
        return state;
    }

    /// <summary>
    /// Create a new DecoState initialized for surface at standard pressure.
    /// </summary>
    public static DecoState CreateAtSurface()
    {
        return CreateAtSurface(GasConstants.StandardPressureMbar / 1000.0);
    }
}