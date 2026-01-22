namespace ZHL_16C.Library;

/// <summary>
/// Calculates partial gas pressures from ambient pressure and gas mix.
/// Matches Subsurface core/gas.cpp fill_pressures() function.
/// </summary>
internal static class GasPressureCalculator
{
    /// <summary>
    /// Compute partial gas pressures in bar from gasmix and ambient pressure.
    /// </summary>
    /// <param name="ambientPressure">Ambient pressure in bar</param>
    /// <param name="mix">Gas mixture</param>
    /// <param name="po2">PO2 setpoint for rebreathers (0 for OC)</param>
    /// <param name="diveMode">Current dive mode</param>
    /// <returns>Partial pressures of O2, N2, and He</returns>
    public static GasPressures FillPressures(
        double ambientPressure, 
        GasMix mix, 
        double po2, 
        DiveMode diveMode)
    {
        // Rebreather dive with defined PO2
        if (diveMode != DiveMode.OC && po2 > 0)
        {
            if (po2 >= ambientPressure)
            {
                return new GasPressures(ambientPressure, 0.0, 0.0);
            }
            
            double o2 = po2;
            double he, n2;
            
            if (mix.GetO2Permille() == 1000)
            {
                he = n2 = 0;
            }
            else
            {
                he = (ambientPressure - o2) * mix.GetHePermille() / (1000.0 - mix.GetO2Permille());
                n2 = ambientPressure - o2 - he;
            }
            
            return new GasPressures(o2, n2, he);
        }
        
        // PSCR mode (passive semi-closed rebreather)
        if (diveMode == DiveMode.PSCR)
        {
            // Simplified - would need prefs for full implementation
            // For now, treat as OC
        }
        
        // Open circuit:  simple partial pressure calculation
        double o2Pressure = mix.GetO2Permille() / 1000.0 * ambientPressure;
        double hePressure = mix.GetHePermille() / 1000.0 * ambientPressure;
        double n2Pressure = mix.GetN2Permille() / 1000.0 * ambientPressure;
        
        return new GasPressures(o2Pressure, n2Pressure, hePressure);
    }
}