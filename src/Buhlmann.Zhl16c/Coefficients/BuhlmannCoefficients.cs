using System.Runtime.CompilerServices;

namespace Buhlmann.Zhl16c.Coefficients;

/// <summary>
/// Bühlmann ZHL-16C decompression model coefficients.
/// Fixed-size struct for zero allocations, matching Subsurface's core/deco.cpp
/// </summary>
public unsafe struct BuhlmannCoefficients
{
    public const int CompartmentCount = 16;

    /// <summary>
    /// Water vapor pressure (bar) - Bühlmann value with Rq = 1.0
    /// </summary>
    public const double WaterVaporPressure = 0.0627;

    /// <summary>
    /// Water vapor pressure (bar) - Schreiner value with Rq = 0.8
    /// </summary>
    public const double WaterVaporPressureSchreiner = 0.0493;

    /// <summary>
    /// ln(2) / 60 - used in half-life calculations
    /// </summary>
    public const double Ln2Over60 = 1.155245301e-02;

    public fixed double N2HalfLife[16];
    public fixed double N2A[16];
    public fixed double N2B[16];
    public fixed double N2FactorOneSecond[16];

    public fixed double HeHalfLife[16];
    public fixed double HeA[16];
    public fixed double HeB[16];
    public fixed double HeFactorOneSecond[16];

    /// <summary>
    /// Return Buhlmann factor for a particular period and tissue index.
    /// Same as Subsurface's factor() function.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Factor(int periodSeconds,
        int ci,
        bool isHelium)
    {
        if (periodSeconds == 1)
        {
            return isHelium ? HeFactorOneSecond[ci] : N2FactorOneSecond[ci];
        }

        var halfLife = isHelium ? HeHalfLife[ci] : N2HalfLife[ci];
        return 1.0 - Math.Exp(-periodSeconds * Ln2Over60 / halfLife);
    }

    /// <summary>
    /// ZHL-16C coefficients singleton.
    /// </summary>
    public static readonly BuhlmannCoefficients ZHL16C = Create();

    private static BuhlmannCoefficients Create()
    {
        var c = new BuhlmannCoefficients();

        // N2 half-times (minutes)
        c.N2HalfLife[0] = 5.0;
        c.N2HalfLife[1] = 8.0;
        c.N2HalfLife[2] = 12.5;
        c.N2HalfLife[3] = 18.5;
        c.N2HalfLife[4] = 27.0;
        c.N2HalfLife[5] = 38.3;
        c.N2HalfLife[6] = 54.3;
        c.N2HalfLife[7] = 77.0;
        c.N2HalfLife[8] = 109.0;
        c.N2HalfLife[9] = 146.0;
        c.N2HalfLife[10] = 187.0;
        c.N2HalfLife[11] = 239.0;
        c.N2HalfLife[12] = 305.0;
        c.N2HalfLife[13] = 390.0;
        c.N2HalfLife[14] = 498.0;
        c.N2HalfLife[15] = 635.0;

        // N2 'a' coefficients (bar)
        c.N2A[0] = 1.1696;
        c.N2A[1] = 1.0;
        c.N2A[2] = 0.8618;
        c.N2A[3] = 0.7562;
        c.N2A[4] = 0.62;
        c.N2A[5] = 0.5043;
        c.N2A[6] = 0.441;
        c.N2A[7] = 0.4;
        c.N2A[8] = 0.375;
        c.N2A[9] = 0.35;
        c.N2A[10] = 0.3295;
        c.N2A[11] = 0.3065;
        c.N2A[12] = 0.2835;
        c.N2A[13] = 0.261;
        c.N2A[14] = 0.248;
        c.N2A[15] = 0.2327;

        // N2 'b' coefficients
        c.N2B[0] = 0.5578;
        c.N2B[1] = 0.6514;
        c.N2B[2] = 0.7222;
        c.N2B[3] = 0.7825;
        c.N2B[4] = 0.8126;
        c.N2B[5] = 0.8434;
        c.N2B[6] = 0.8693;
        c.N2B[7] = 0.8910;
        c.N2B[8] = 0.9092;
        c.N2B[9] = 0.9222;
        c.N2B[10] = 0.9319;
        c.N2B[11] = 0.9403;
        c.N2B[12] = 0.9477;
        c.N2B[13] = 0.9544;
        c.N2B[14] = 0.9602;
        c.N2B[15] = 0.9653;

        // N2 factor for 1-second exposure
        c.N2FactorOneSecond[0] = 2.30782347297664E-003;
        c.N2FactorOneSecond[1] = 1.44301447809736E-003;
        c.N2FactorOneSecond[2] = 9.23769302935806E-004;
        c.N2FactorOneSecond[3] = 6.24261986779007E-004;
        c.N2FactorOneSecond[4] = 4.27777107246730E-004;
        c.N2FactorOneSecond[5] = 3.01585140931371E-004;
        c.N2FactorOneSecond[6] = 2.12729727268379E-004;
        c.N2FactorOneSecond[7] = 1.50020603047807E-004;
        c.N2FactorOneSecond[8] = 1.05980191127841E-004;
        c.N2FactorOneSecond[9] = 7.91232600646508E-005;
        c.N2FactorOneSecond[10] = 6.17759153688224E-005;
        c.N2FactorOneSecond[11] = 4.83354552742732E-005;
        c.N2FactorOneSecond[12] = 3.78761777920511E-005;
        c.N2FactorOneSecond[13] = 2.96212356654113E-005;
        c.N2FactorOneSecond[14] = 2.31974277413727E-005;
        c.N2FactorOneSecond[15] = 1.81926738960225E-005;

        // He half-times (minutes)
        c.HeHalfLife[0] = 1.88;
        c.HeHalfLife[1] = 3.02;
        c.HeHalfLife[2] = 4.72;
        c.HeHalfLife[3] = 6.99;
        c.HeHalfLife[4] = 10.21;
        c.HeHalfLife[5] = 14.48;
        c.HeHalfLife[6] = 20.53;
        c.HeHalfLife[7] = 29.11;
        c.HeHalfLife[8] = 41.20;
        c.HeHalfLife[9] = 55.19;
        c.HeHalfLife[10] = 70.69;
        c.HeHalfLife[11] = 90.34;
        c.HeHalfLife[12] = 115.29;
        c.HeHalfLife[13] = 147.42;
        c.HeHalfLife[14] = 188.24;
        c.HeHalfLife[15] = 240.03;

        // He 'a' coefficients (bar)
        c.HeA[0] = 1.6189;
        c.HeA[1] = 1.383;
        c.HeA[2] = 1.1919;
        c.HeA[3] = 1.0458;
        c.HeA[4] = 0.922;
        c.HeA[5] = 0.8205;
        c.HeA[6] = 0.7305;
        c.HeA[7] = 0.6502;
        c.HeA[8] = 0.595;
        c.HeA[9] = 0.5545;
        c.HeA[10] = 0.5333;
        c.HeA[11] = 0.5189;
        c.HeA[12] = 0.5181;
        c.HeA[13] = 0.5176;
        c.HeA[14] = 0.5172;
        c.HeA[15] = 0.5119;

        // He 'b' coefficients
        c.HeB[0] = 0.4770;
        c.HeB[1] = 0.5747;
        c.HeB[2] = 0.6527;
        c.HeB[3] = 0.7223;
        c.HeB[4] = 0.7582;
        c.HeB[5] = 0.7957;
        c.HeB[6] = 0.8279;
        c.HeB[7] = 0.8553;
        c.HeB[8] = 0.8757;
        c.HeB[9] = 0.8903;
        c.HeB[10] = 0.8997;
        c.HeB[11] = 0.9073;
        c.HeB[12] = 0.9122;
        c.HeB[13] = 0.9171;
        c.HeB[14] = 0.9217;
        c.HeB[15] = 0.9267;

        // He factor for 1-second exposure
        c.HeFactorOneSecond[0] = 6.12608039419837E-003;
        c.HeFactorOneSecond[1] = 3.81800836683133E-003;
        c.HeFactorOneSecond[2] = 2.44456078654209E-003;
        c.HeFactorOneSecond[3] = 1.65134647076792E-003;
        c.HeFactorOneSecond[4] = 1.13084424730725E-003;
        c.HeFactorOneSecond[5] = 7.97503165599123E-004;
        c.HeFactorOneSecond[6] = 5.62552521860549E-004;
        c.HeFactorOneSecond[7] = 3.96776399429366E-004;
        c.HeFactorOneSecond[8] = 2.80360036664540E-004;
        c.HeFactorOneSecond[9] = 2.09299583354805E-004;
        c.HeFactorOneSecond[10] = 1.63410794820518E-004;
        c.HeFactorOneSecond[11] = 1.27869320250551E-004;
        c.HeFactorOneSecond[12] = 1.00198406028040E-004;
        c.HeFactorOneSecond[13] = 7.83611475491108E-005;
        c.HeFactorOneSecond[14] = 6.13689891868496E-005;
        c.HeFactorOneSecond[15] = 4.81280465299827E-005;

        return c;
    }
}