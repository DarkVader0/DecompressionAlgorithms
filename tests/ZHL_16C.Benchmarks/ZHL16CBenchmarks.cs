using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZHL_16C.Library;

namespace ZHL_16C.Benchmarks;

/// <summary>
/// Benchmarks for ZHL-16C decompression algorithm.
/// </summary>
[MemoryDiagnoser(false)]
// [RankColumn]
public class Zhl16CBenchmarks
{
    private const double SurfacePressure = 1.013;
    private const int Salinity = 10300; // Salt water
    private Zhl16CAlgorithm _algorithm = null!;
    private AlgorithmConfiguration _config = null!;
    private List<DecoGas> _gases = null!;
    private DecoPlanner _planner = null!;

    private List<DiveWaypoint> _profile40m40min = null!;
    private PlannerSettings _settings = null!;
    private DecoState _state = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Configure algorithm with GF 60/80
        _config = new AlgorithmConfiguration
        {
            GfLow = 0.60,
            GfHigh = 0.80,
            LastDecoStopInMeters = 6
        };

        _settings = new PlannerSettings
        {
            // 5 m/min descent and ascent
            DescentRate = 5000 / 60,
            AscentRate75 = 5000 / 60,
            AscentRate50 = 5000 / 60,
            AscentRateStops = 5000 / 60,

            // 1 m/min from last stop (6m)
            AscentRateLast6m = 1000 / 60,

            LastStop = true,
            MinSwitchDuration = 4 * 60, // 4 minute gas switch

            BottomSac = 20000,
            DecoSac = 15000
        };

        _algorithm = new Zhl16CAlgorithm(_config);
        _planner = new DecoPlanner(_config, _settings);
        _state = new DecoState();

        // 40m for 40 minutes profile
        const int descentTime = 8 * 60; // 8 min descent at 5m/min
        _profile40m40min =
        [
            new DiveWaypoint { TimeSeconds = 0, Depth = Depth.FromMeters(0), Gas = GasMix.Air },
            new DiveWaypoint { TimeSeconds = descentTime, Depth = Depth.FromMeters(40), Gas = GasMix.Air },
            new DiveWaypoint { TimeSeconds = 40 * 60, Depth = Depth.FromMeters(40), Gas = GasMix.Air }
        ];

        // Available gases
        _gases =
        [
            DecoGas.Create(GasMix.Air, 1.4),
            DecoGas.Create(GasMix.Nitrox(50))
        ];
    }

    #region Core Algorithm Benchmarks

    [Benchmark(Description = "ClearDeco - Initialize tissues")]
    public void Benchmark_ClearDeco()
    {
        _algorithm.ClearDeco(_state, SurfacePressure);
    }

    [Benchmark(Description = "AddSegment - 1 second")]
    public void Benchmark_AddSegment_1Second()
    {
        var pressure = Zhl16CAlgorithm.DepthToPressure(Depth.FromMeters(40), Salinity, SurfacePressure);
        _algorithm.AddSegment(_state, pressure, GasMix.Air, 1, 0, DiveMode.OC);
    }

    [Benchmark(Description = "AddSegment - 60 seconds")]
    public void Benchmark_AddSegment_60Seconds()
    {
        var pressure = Zhl16CAlgorithm.DepthToPressure(Depth.FromMeters(40), Salinity, SurfacePressure);
        _algorithm.AddSegment(_state, pressure, GasMix.Air, 60, 0, DiveMode.OC);
    }

    [Benchmark(Description = "TissueToleranceCalc - Calculate ceiling")]
    public double Benchmark_TissueToleranceCalc()
    {
        return _algorithm.TissueToleranceCalc(_state, SurfacePressure);
    }

    [Benchmark(Description = "DecoAllowedDepth - Get allowed depth")]
    public Depth Benchmark_DecoAllowedDepth()
    {
        var tolerance = _algorithm.TissueToleranceCalc(_state, SurfacePressure);
        return _algorithm.DecoAllowedDepth(tolerance, SurfacePressure);
    }

    #endregion

    #region Full Dive Simulation Benchmarks

    [Benchmark(Description = "Simulate 40m/20min bottom time only")]
    public void Benchmark_SimulateBottomTime_40m_20min()
    {
        var state = new DecoState();
        _algorithm.ClearDeco(state, SurfacePressure);

        var pressure40m = Zhl16CAlgorithm.DepthToPressure(Depth.FromMeters(40), Salinity, SurfacePressure);

        // Simulate descent (2 min at average pressure)
        var avgPressure = (SurfacePressure + pressure40m) / 2;
        _algorithm.AddSegment(state, avgPressure, GasMix.Air, 120, 0, DiveMode.OC);

        // Simulate 20 min at 40m
        _algorithm.AddSegment(state, pressure40m, GasMix.Air, 20 * 60, 0, DiveMode.OC);

        // Calculate ceiling
        _algorithm.TissueToleranceCalc(state, SurfacePressure);
    }

    [Benchmark(Description = "Simulate 40m/40min bottom time only")]
    public void Benchmark_SimulateBottomTime_40m_40min()
    {
        var state = new DecoState();
        _algorithm.ClearDeco(state, SurfacePressure);

        var pressure40m = Zhl16CAlgorithm.DepthToPressure(Depth.FromMeters(40), Salinity, SurfacePressure);

        // Simulate descent (8 min)
        var avgPressure = (SurfacePressure + pressure40m) / 2;
        _algorithm.AddSegment(state, avgPressure, GasMix.Air, 8 * 60, 0, DiveMode.OC);

        // Simulate 32 min at 40m
        _algorithm.AddSegment(state, pressure40m, GasMix.Air, 32 * 60, 0, DiveMode.OC);

        // Calculate ceiling
        _algorithm.TissueToleranceCalc(state, SurfacePressure);
    }

    #endregion

    #region Full Planner Benchmarks

    [Benchmark(Description = "Full Plan:  40m/40min with deco (Air + EAN50)")]
    public DecoPlanResult Benchmark_FullPlan_40m_40min()
    {
        return _planner.CalculateDecoPlan(_profile40m40min, _gases);
    }

    [Benchmark(Description = "Full Plan: 30m/30min recreational")]
    public DecoPlanResult Benchmark_FullPlan_30m_30min_Recreational()
    {
        var profile = new List<DiveWaypoint>
        {
            new() { TimeSeconds = 0, Depth = Depth.FromMeters(0), Gas = GasMix.Air },
            new() { TimeSeconds = 6 * 60, Depth = Depth.FromMeters(30), Gas = GasMix.Air },
            new() { TimeSeconds = 30 * 60, Depth = Depth.FromMeters(30), Gas = GasMix.Air }
        };

        var gases = new List<DecoGas>
        {
            DecoGas.Create(GasMix.Air, 1.4)
        };

        return _planner.CalculateDecoPlan(profile, gases);
    }

    [Benchmark(Description = "Full Plan: 60m/20min trimix")]
    public DecoPlanResult Benchmark_FullPlan_60m_20min_Trimix()
    {
        var trimix = GasMix.Trimix(21, 35); // 21/35

        var profile = new List<DiveWaypoint>
        {
            new() { TimeSeconds = 0, Depth = Depth.FromMeters(0), Gas = trimix },
            new() { TimeSeconds = 6 * 60, Depth = Depth.FromMeters(60), Gas = trimix },
            new() { TimeSeconds = 20 * 60, Depth = Depth.FromMeters(60), Gas = trimix }
        };

        var gases = new List<DecoGas>
        {
            DecoGas.Create(trimix, 1.4),
            DecoGas.Create(GasMix.Nitrox(50)),
            DecoGas.Create(GasMix.Oxygen)
        };

        return _planner.CalculateDecoPlan(profile, gases);
    }

    #endregion
}