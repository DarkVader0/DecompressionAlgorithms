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
    private const int DepthM = 45;
    private const int BottomTimeMin = 40;
    private const int DescentRateMmSec = 5 * 1000 / 60;
    private const int DescentTimeSec = DepthM * 1000 / DescentRateMmSec / 60 * 60;
    private const int BottomPhaseSec = BottomTimeMin * 60 - DescentTimeSec;
    private const uint SacBottomMlMin = 20;
    private const uint SacDecoMlMin = 15;

    private Cylinder[] _cylinders = null!;
    private Waypoint[] _waypoints = null!;
    private PlannerSettings _settings;
    private DiveContext _context;

    [GlobalSetup]
    public void Setup()
    {
        _cylinders =
        [
            new Cylinder
            {
                O2Permille = 250,
                HePermille = 0,
                SizeMl = 12_000,
                StartPressureMbar = 200_000,
                Use = CylinderUse.Bottom
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
                DecoPo2Mbar = 1400,
                BottomSacMl = SacBottomMlMin,
                DecoSacMl = SacDecoMlMin
            },
            Reserve = new ReserveGasSettings
            {
                ReservePressureMbar = 50_000,
                CalculateMinGas = true,
                TeamSize = 2,
                SacStressFactor = 4
            },
            AscentDescent = new AscentDescentSettings
            {
                DescentRateMmSec = DescentRateMmSec,
                AscentRate75MmSec = 5 * 1000 / 60,
                AscentRate50MmSec = 5 * 1000 / 60,
                AscentRateStopsMmSec = 5 * 1000 / 60,
                AscentRateLast6mMmSec = 1 * 1000 / 60
            },
            Stops = new StopSettings
            {
                LastStopAt6m = true,
                SafetyStop = false,
                MinSwitchDurationSec = 4,
                ProblemSolvingTimeMin = 4,
                SwitchAtRequiredStop = false
            },
            Rebreather = new RebreatherSettings
            {
                DiveMode = DiveMode.OC
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
            new Waypoint
            {
                DepthMm = DepthM * 1000,
                DurationSeconds = DescentTimeSec,
                CylinderIndex = 0
            },
            new Waypoint
            {
                DepthMm = DepthM * 1000,
                DurationSeconds = BottomPhaseSec,
                CylinderIndex = 0
            }
        ];
    }

    [Benchmark(Description = "Full Plan (45m/40min EAN25)")]
    public PlanResult BenchmarkFullPlan()
    {
        return DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
    }

    [Benchmark(Description = "CNS Calculation")]
    public double BenchmarkCns()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        OxygenToxicity.ApplyToPlan(ref plan, _cylinders, _context);
        return plan.CnsPercent;
    }

    [Benchmark(Description = "OTU Calculation")]
    public double BenchmarkOtu()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        OxygenToxicity.ApplyToPlan(ref plan, _cylinders, _context);
        return plan.OtuTotal;
    }

    [Benchmark(Description = "Rock Bottom Gas (MinGasRequired)")]
    public int BenchmarkRockBottomGas()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        return plan.CylinderResults[0].MinGasRequiredMl;
    }

    [Benchmark(Description = "Gas Used (ml)")]
    public int BenchmarkGasUsed()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        return plan.CylinderResults[0].GasUsedMl;
    }

    [Benchmark(Description = "Remaining Pressure (mbar)")]
    public int BenchmarkRemainingPressure()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        return plan.CylinderResults[0].EndPressureMbar;
    }

    [Benchmark(Description = "TTS / Max Deco (sec)")]
    public int BenchmarkTts()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        return plan.DecoTimeSec;
    }

    [Benchmark(Description = "All Metrics Combined")]
    public DiveSummary BenchmarkAllMetrics()
    {
        var plan = DecoPlanner.Plan(_cylinders, _waypoints, _settings, _context);
        OxygenToxicity.ApplyToPlan(ref plan, _cylinders, _context);

        var cyl = plan.CylinderResults[0];

        return new DiveSummary
        {
            CnsPercent = plan.CnsPercent,
            OtuTotal = plan.OtuTotal,
            DecoTimeSec = plan.DecoTimeSec,
            TotalTimeSec = plan.TimeTotalSec,
            MaxDepthMm = plan.MaxDepthMm,
            GasUsedMl = cyl.GasUsedMl,
            MinGasRequiredMl = cyl.MinGasRequiredMl,
            EndPressureMbar = cyl.EndPressureMbar,
            IsGasSufficient = cyl.EndPressureMbar > 0
        };
    }

    public struct DiveSummary
    {
        public ushort CnsPercent;
        public ushort OtuTotal;
        public int DecoTimeSec;
        public int TotalTimeSec;
        public int MaxDepthMm;
        public int GasUsedMl;
        public int MinGasRequiredMl;
        public int EndPressureMbar;
        public bool IsGasSufficient;
    }
}