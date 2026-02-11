using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Coefficients;
using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;

namespace Buhlmann.Zhl16c.Helpers;

public unsafe struct DecoState
{
    public fixed double TissueN2Sat[16];
    public fixed double TissueHeSat[16];
    public double GfLowPressureThisDive;
    public int LeadingTissueIndex;

    public void Clear(double surfacePressureBar)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint CeilingMm(double gf, DiveContext context)
    {
        var ceilingBar = CeilingBar(gf);

        if (ceilingBar > GfLowPressureThisDive)
        {
            GfLowPressureThisDive = ceilingBar;
        }

        var ceilingMbar = (uint)(ceilingBar * 1000);

        if (ceilingMbar <= context.SurfacePressureMbar)
        {
            return 0;
        }

        return context.MbarToDepthMm(ceilingMbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint CeilingMm(double gfLow,
        double gfHigh,
        DiveContext context)
    {
        ref readonly var coeff = ref BuhlmannCoefficients.ZHL16C;

        var surfacePressureBar = context.SurfacePressureMbar / 1000.0;
        var lowestCeiling = 0.0;

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

            var tissueCeiling = (b * pInert - gfLow * a * b) / ((1.0 - b) * gfLow + b);

            if (tissueCeiling > lowestCeiling)
            {
                lowestCeiling = tissueCeiling;
            }
        }

        if (lowestCeiling > GfLowPressureThisDive)
        {
            GfLowPressureThisDive = lowestCeiling;
        }

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

            double tolerated;

            var mValueAtSurface = (surfacePressureBar / b + a - surfacePressureBar) * gfHigh + surfacePressureBar;
            var mValueAtFirstStop =
                (GfLowPressureThisDive / b + a - GfLowPressureThisDive) * gfLow + GfLowPressureThisDive;

            if (mValueAtSurface < mValueAtFirstStop)
            {
                tolerated = (-a * b * (gfHigh * GfLowPressureThisDive - gfLow * surfacePressureBar) -
                             (1.0 - b) * (gfHigh - gfLow) * GfLowPressureThisDive * surfacePressureBar +
                             b * (GfLowPressureThisDive - surfacePressureBar) * pInert) /
                            (-a * b * (gfHigh - gfLow) +
                             (1.0 - b) * (gfLow * GfLowPressureThisDive - gfHigh * surfacePressureBar) +
                             b * (GfLowPressureThisDive - surfacePressureBar));
            }
            else
            {
                tolerated = maxCeiling;
            }

            if (tolerated > maxCeiling)
            {
                maxCeiling = tolerated;
                leadingIdx = i;
            }
        }

        LeadingTissueIndex = leadingIdx;

        if (maxCeiling * 1000 <= context.SurfacePressureMbar)
        {
            return 0;
        }

        return context.MbarToDepthMm((uint)(maxCeiling * 1000));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetInertPressure(int compartmentIndex)
    {
        return TissueN2Sat[compartmentIndex] + TissueHeSat[compartmentIndex];
    }

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

    public static DecoState CreateAtSurface(double surfacePressureBar)
    {
        var state = new DecoState();
        state.Clear(surfacePressureBar);
        return state;
    }

    public static DecoState CreateAtSurface()
    {
        return CreateAtSurface(GasConstants.StandardPressureMbar / 1000.0);
    }
}