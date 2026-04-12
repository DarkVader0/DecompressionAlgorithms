using BenchmarkDotNet.Attributes;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Output;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Benchmarks;

[MemoryDiagnoser(false)]
public class DivePlanBenchmarks
{
    private Cylinder[] _cylinders = null!;
    private Waypoint[] _waypoints = null!;
    private PlannerSettings _settings;
    private DiveContext _context;

    [GlobalSetup]
    public void Setup()
    {
        _cylinders =
        [
            new()
            {
                O2Permille = 250,
                HePermille = 0,
                SizeMl = 24000,
                StartPressureMbar = 200000
            },
            new()
            {
                O2Permille = 500,
                HePermille = 0,
                SizeMl = 12000,
                StartPressureMbar = 200000,
                Use = CylinderUse.Deco
            }
        ];
        _settings = new PlannerSettings
        {
            Deco = new DecoSettings
            {
                GFLow = 50,
                GFHigh = 70
            },
            Gas = new GasSettings
            {
                BottomPo2Mbar = 1400,
                DecoPo2Mbar = 1600,
                BottomSacMl = 15000,
                DecoSacMl = 10000,
            },
            Reserve = new ReserveGasSettings
            {
                ReservePressureMbar = 50000,
                CalculateMinGas = true,
                TeamSize = 2,
                SacStressFactor = 4
            },
            AscentDescent = new AscentDescentSettings
            {
                DescentRateMmSec = 5 * 1000 / 60,
                AscentRate75MmSec = 5 * 1000 / 60,
                AscentRate50MmSec = 5 * 1000 / 60,
                AscentRateStopsMmSec = 5 * 1000 / 60,
                AscentRateLast6mMmSec = 1 * 1000 / 60
            },
            Stops = new StopSettings
            {
                LastStopAt6m = true,
                SafetyStop = false,
                MinSwitchDurationSec = 3 * 60,
                ProblemSolvingTimeMin = 4,
                SwitchAtRequiredStop = false
            },
            Environment = new EnvironmentSettings
            {
                SurfacePressureMbar = 1013,
                WaterType = WaterType.Salt
            }
        };
        _context = new DiveContext(1013, WaterType.Salt);

        _waypoints =
        [
            new()
            {
                DepthMm = 48 * 1000, DurationSeconds = 48 * 1000 / (5 * 1000) * 60, CylinderIndex = 0
            },
            new()
            {
                DepthMm = 48 * 1000, DurationSeconds = 40 * 60 - 48 * 1000 / (5 * 1000) * 60, CylinderIndex = 0
            }
        ];
    }

    [Benchmark(Description = "Full Plan (48m/48min NX25,NX50)")]
    public PlanResult BenchmarkFullPlan()
    {
        return DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
    }
}