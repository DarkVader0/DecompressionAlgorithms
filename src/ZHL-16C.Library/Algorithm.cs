namespace ZHL_16C.Library;

/// <summary>
/// ZHL-16C Bühlmann decompression algorithm implementation.
/// Based on Subsurface core/deco.cpp
/// </summary>
public sealed class Zhl16CAlgorithm
{
    private const int TissueCount = 16;

    private readonly AlgorithmConfiguration _config;

    public Zhl16CAlgorithm(AlgorithmConfiguration? config = null)
    {
        _config = config ?? new AlgorithmConfiguration();
    }

    #region Core Algorithm Functions

    /// <summary>
    /// Initialize tissue saturation to surface equilibrium.
    /// Matches Subsurface clear_deco() function.
    /// </summary>
    /// <param name="state">Deco state to initialize</param>
    /// <param name="surfacePressure">Surface pressure in bar</param>
    public void ClearDeco(DecoState state, double surfacePressure)
    {
        // Calculate initial N2 pressure in tissues at surface
        // = (surface_pressure - water_vapor_pressure) * N2_fraction_in_air
        var initialN2 = (surfacePressure - PhysicalConstants.WaterVaporPressure)
            * PhysicalConstants.N2InAirPermille / 1000.0;

        for (var i = 0; i < TissueCount; i++)
        {
            state.TissueN2Sat[i] = initialN2;
            state.TissueHeSat[i] = 0.0;
            state.TissueInertGasSat[i] = initialN2;
            state.ToleratedByTissue[i] = 0.0;
            state.BuehlmannInertGasA[i] = 0.0;
            state.BuehlmannInertGasB[i] = 0.0;
        }

        state.GfLowPressureThisDive = surfacePressure + _config.GfLowPositionMin;
        state.GuidingTissueIndex = -1;
        state.IcdWarning = false;
    }

    /// <summary>
    /// Add a segment of time at given pressure breathing a gas mix.
    /// This is the core tissue loading function.
    /// Matches Subsurface add_segment() function.
    /// </summary>
    /// <param name="state">Current deco state</param>
    /// <param name="ambientPressure">Ambient pressure in bar</param>
    /// <param name="gasMix">Gas mixture being breathed</param>
    /// <param name="periodInSeconds">Duration of segment in seconds</param>
    /// <param name="ccpo2">CCR setpoint in mbar (0 for OC)</param>
    /// <param name="diveMode">Current dive mode</param>
    public void AddSegment(
        DecoState state,
        double ambientPressure,
        GasMix gasMix,
        int periodInSeconds,
        int ccpo2,
        DiveMode diveMode)
    {
        // Calculate inspired gas partial pressures (accounting for water vapor)
        var effectivePressure = ambientPressure - PhysicalConstants.WaterVaporPressure;

        var pressures = GasPressureCalculator.FillPressures(
            effectivePressure, gasMix, ccpo2 / 1000.0, diveMode);

        var icd = false;

        for (var ci = 0; ci < TissueCount; ci++)
        {
            // Calculate over/under saturation
            var pn2Oversat = pressures.N2 - state.TissueN2Sat[ci];
            var pheOversat = pressures.He - state.TissueHeSat[ci];

            // Get compartment-specific factors
            var n2Factor = CalculateFactor(periodInSeconds, ci, GasComponent.N2);
            var heFactor = CalculateFactor(periodInSeconds, ci, GasComponent.He);

            // Apply saturation/desaturation multipliers
            var n2SatMult = pn2Oversat > 0 ? _config.SaturationMultiplier : _config.DesaturationMultiplier;
            var heSatMult = pheOversat > 0 ? _config.SaturationMultiplier : _config.DesaturationMultiplier;

            // Check for Isobaric Counter Diffusion in leading tissue
            if (ci == state.GuidingTissueIndex && pn2Oversat > 0.0 && pheOversat < 0.0)
            {
                if (pn2Oversat * n2SatMult * n2Factor + pheOversat * heSatMult * heFactor > 0)
                {
                    icd = true;
                }
            }

            // Update tissue saturation using Schreiner equation
            state.TissueN2Sat[ci] += n2SatMult * pn2Oversat * n2Factor;
            state.TissueHeSat[ci] += heSatMult * pheOversat * heFactor;
            state.TissueInertGasSat[ci] = state.TissueN2Sat[ci] + state.TissueHeSat[ci];
        }

        state.IcdWarning = icd;
    }

    /// <summary>
    /// Calculate the tissue tolerance (ceiling pressure).
    /// Returns the minimum tolerable ambient pressure.
    /// Matches Subsurface tissue_tolerance_calc() function (Bühlmann mode only).
    /// </summary>
    /// <param name="state">Current deco state</param>
    /// <param name="surfacePressure">Surface pressure in bar</param>
    /// <returns>Tolerated ambient pressure in bar</returns>
    public double TissueToleranceCalc(DecoState state, double surfacePressure)
    {
        var gfHigh = _config.GfHigh;
        var gfLow = _config.GfLow;
        var lowestCeiling = 0.0;
        var tissueLowestCeiling = new double[TissueCount];

        // First, calculate weighted a and b coefficients for each tissue
        for (var ci = 0; ci < TissueCount; ci++)
        {
            if (state.TissueInertGasSat[ci] > 0)
            {
                // Weighted average of a and b based on gas loading
                state.BuehlmannInertGasA[ci] =
                    (Coefficients.N2A[ci] * state.TissueN2Sat[ci] +
                     Coefficients.HeA[ci] * state.TissueHeSat[ci])
                    / state.TissueInertGasSat[ci];

                state.BuehlmannInertGasB[ci] =
                    (Coefficients.N2B[ci] * state.TissueN2Sat[ci] +
                     Coefficients.HeB[ci] * state.TissueHeSat[ci])
                    / state.TissueInertGasSat[ci];
            }
            else
            {
                state.BuehlmannInertGasA[ci] = Coefficients.N2A[ci];
                state.BuehlmannInertGasB[ci] = Coefficients.N2B[ci];
            }
        }

        // Calculate tissue ceiling using GF_low
        for (var ci = 0; ci < TissueCount; ci++)
        {
            var a = state.BuehlmannInertGasA[ci];
            var b = state.BuehlmannInertGasB[ci];
            var pInert = state.TissueInertGasSat[ci];

            // Ceiling formula with gradient factor: 
            // ceiling = (b * pInert - gf * a * b) / ((1 - b) * gf + b)
            tissueLowestCeiling[ci] = (b * pInert - gfLow * a * b) / ((1.0 - b) * gfLow + b);

            if (tissueLowestCeiling[ci] > lowestCeiling)
            {
                lowestCeiling = tissueLowestCeiling[ci];
            }
        }

        // Track the deepest ceiling encountered (for GF interpolation)
        if (lowestCeiling > state.GfLowPressureThisDive)
        {
            state.GfLowPressureThisDive = lowestCeiling;
        }

        // Now calculate tolerated pressure with GF interpolation
        var toleratedPressure = 0.0;

        for (var ci = 0; ci < TissueCount; ci++)
        {
            var a = state.BuehlmannInertGasA[ci];
            var b = state.BuehlmannInertGasB[ci];
            var pInert = state.TissueInertGasSat[ci];
            double tolerated;

            // Check if we need to interpolate between GF_low and GF_high
            var gfLowPressure = state.GfLowPressureThisDive;

            // M-value at surface with GF_high
            var mValueSurface = (surfacePressure / b + a - surfacePressure) * gfHigh + surfacePressure;

            // M-value at GF_low depth with GF_low  
            var mValueGfLow = (gfLowPressure / b + a - gfLowPressure) * gfLow + gfLowPressure;

            if (mValueSurface < mValueGfLow)
            {
                // Interpolate GF between surface and first ceiling
                tolerated = (-a * b * (gfHigh * gfLowPressure - gfLow * surfacePressure) -
                             (1.0 - b) * (gfHigh - gfLow) * gfLowPressure * surfacePressure +
                             b * (gfLowPressure - surfacePressure) * pInert) /
                            (-a * b * (gfHigh - gfLow) +
                             (1.0 - b) * (gfLow * gfLowPressure - gfHigh * surfacePressure) +
                             b * (gfLowPressure - surfacePressure));
            }
            else
            {
                tolerated = toleratedPressure;
            }

            state.ToleratedByTissue[ci] = tolerated;

            if (tolerated >= toleratedPressure)
            {
                state.GuidingTissueIndex = ci;
                toleratedPressure = tolerated;
            }
        }

        return toleratedPressure;
    }

    /// <summary>
    /// Calculate the allowed depth based on tissue tolerance.
    /// Matches Subsurface deco_allowed_depth() function.
    /// </summary>
    /// <param name="tissueTolerance">Tolerated pressure from TissueToleranceCalc()</param>
    /// <param name="surfacePressure">Surface pressure in bar</param>
    /// <param name="salinity">Water salinity in g/10L</param>
    /// <param name="smooth">If false, round to 3m/10ft increments</param>
    /// <returns>Allowed depth</returns>
    public Depth DecoAllowedDepth(
        double tissueTolerance,
        double surfacePressure,
        int salinity = 10300,
        bool smooth = false)
    {
        // Calculate pressure delta (avoid negative depths)
        var pressureDelta = tissueTolerance > surfacePressure
            ? tissueTolerance - surfacePressure
            : 0.0;

        // Convert pressure to depth
        // pressure (bar) = depth (m) * density * g / 100000
        // For seawater: ~0.1 bar per meter
        var depthMm = PressureToDepthMm(pressureDelta, salinity);

        if (!smooth)
        {
            // Round up to next 3m stop
            depthMm = Math.Ceiling(depthMm / PhysicalConstants.DecoStopsMultiplierMm)
                      * PhysicalConstants.DecoStopsMultiplierMm;
        }

        // Apply last deco stop depth limit
        if (depthMm > 0 && depthMm < _config.LastDecoStopInMeters * 1000)
        {
            depthMm = _config.LastDecoStopInMeters * 1000;
        }

        return new Depth((int)Math.Round(depthMm));
    }

    /// <summary>
    /// Get the current gradient factor at a given pressure.
    /// Interpolates between GF_low and GF_high.
    /// Matches Subsurface get_gf() function.
    /// </summary>
    public double GetGf(DecoState state,
        double ambientPressure,
        double surfacePressure)
    {
        var gfLow = _config.GfLow;
        var gfHigh = _config.GfHigh;

        if (state.GfLowPressureThisDive > surfacePressure)
        {
            var gf = (ambientPressure - surfacePressure) /
                (state.GfLowPressureThisDive - surfacePressure) *
                (gfLow - gfHigh) + gfHigh;
            return Math.Max(gfLow, gf);
        }

        return gfLow;
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Calculate the Bühlmann factor for tissue gas loading.
    /// factor = 1 - e^(-t * ln(2) / (halflife * 60))
    /// </summary>
    private double CalculateFactor(int periodInSeconds,
        int tissueIndex,
        GasComponent gas)
    {
        // Use pre-calculated 1-second factors for optimization
        if (periodInSeconds == 1)
        {
            return gas == GasComponent.N2
                ? Coefficients.N2FactorOneSecond[tissueIndex]
                : Coefficients.HeFactorOneSecond[tissueIndex];
        }

        // Calculate factor for longer periods
        var halfLife = gas == GasComponent.N2
            ? Coefficients.N2HalfLife[tissueIndex]
            : Coefficients.HeHalfLife[tissueIndex];

        // ln(2)/60 = 0.0115524530...
        return 1.0 - Math.Exp(-periodInSeconds * PhysicalConstants.Ln2Over60 / halfLife);
    }

    /// <summary>
    /// Convert pressure (bar) to depth (mm) based on salinity.
    /// </summary>
    private static double PressureToDepthMm(double pressureBar, int salinity)
    {
        // pressure = depth * salinity * g / 1e7
        // depth = pressure * 1e7 / (salinity * g)
        // For simplicity:  ~10m per bar in seawater
        var specificWeight = salinity * 0.981 / 100000.0;
        return pressureBar / specificWeight * 1000.0; // Convert to mm
    }

    /// <summary>
    /// Convert depth (mm) to pressure (bar) based on salinity.
    /// </summary>
    public static double DepthToPressure(Depth depth,
        int salinity,
        double surfacePressure)
    {
        var specificWeight = salinity * 0.981 / 100000.0;
        return surfacePressure + depth.Mm / 1000.0 * specificWeight;
    }

    #endregion
}