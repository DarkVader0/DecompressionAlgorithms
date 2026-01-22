using System.Diagnostics;
using ZHL_16C.Library;

namespace ZHL_16C.Benchmarks;

/// <summary>
/// Validation tests to verify algorithm output before benchmarking.
/// Run this to see actual results and validate correctness.
/// </summary>
public static class ValidationTests
{
    public static void RunAll()
    {
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("VALIDATION TESTS - ZHL-16C Algorithm");
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine();

        Run_40m_45min_Benchmark();
    }

    public static void Run_40m_45min_Benchmark()
    {
        Console.WriteLine("TEST: 40m for 45 minutes");
        Console.WriteLine("-".PadRight(70, '-'));
        Console.WriteLine();

        // Configure algorithm with GF 30/75
        var config = new AlgorithmConfiguration
        {
            GfLow = 0.30,
            GfHigh = 0.75,
            LastDecoStopInMeters = 6
        };

        var settings = new PlannerSettings
        {
            DescentRate = 5000 / 60, // 5 m/min
            AscentRate75 = 5000 / 60, // 5 m/min
            AscentRate50 = 5000 / 60, // 5 m/min
            AscentRateStops = 5000 / 60, // 5 m/min
            AscentRateLast6m = 1000 / 60, // 1 m/min
            LastStop = true, // 6m last stop
            MinSwitchDuration = 4 * 60, // 4 min gas switch
            BottomSac = 20000, // 20 L/min
            DecoSac = 15000 // 15 L/min
        };

        var planner = new DecoPlanner(config, settings);

        var surfacePressure = 1.013;
        var salinity = PhysicalConstants.SeawaterSalinity;

        // 8 min descent at 5m/min to 40m
        var descentTime = 8 * 60;

        var profile = new List<DiveWaypoint>
        {
            new() { TimeSeconds = 0, Depth = Depth.FromMeters(0), Gas = GasMix.Air },
            new() { TimeSeconds = descentTime, Depth = Depth.FromMeters(40), Gas = GasMix.Air },
            new() { TimeSeconds = 45 * 60, Depth = Depth.FromMeters(40), Gas = GasMix.Air }
        };

        var gases = new List<DecoGas>
        {
            DecoGas.Create(GasMix.Air, 1.4),
            DecoGas.Create(GasMix.Nitrox(50))
        };

        // Print inputs
        Console.WriteLine("INPUTS:");
        Console.WriteLine("  Depth: 40m");
        Console.WriteLine("  Bottom Time: 45 min (including 8 min descent)");
        Console.WriteLine($"  Water: Salt water ({salinity} g/10L)");
        Console.WriteLine($"  GF: {config.GfLow * 100:F0}/{config.GfHigh * 100:F0}");
        Console.WriteLine("  Descent Rate: 5 m/min");
        Console.WriteLine("  Ascent Rate: 5 m/min (1 m/min last 6m)");
        Console.WriteLine("  Last Stop: 6m");
        Console.WriteLine("  Gas Switch Time: 4 min");
        Console.WriteLine();
        Console.WriteLine("GASES:");
        Console.WriteLine("  Air - 24L @ 200 bar");
        Console.WriteLine("  EAN50 - 80 cuft @ 200 bar (MOD ~21m)");
        Console.WriteLine();

        // Time the calculation
        var sw = Stopwatch.StartNew();
        var result = planner.CalculateDecoPlan(profile, gases, surfacePressure, salinity);
        sw.Stop();

        Console.WriteLine($"Calculation Time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine();

        // Print results
        Console.WriteLine("RESULTS:");
        Console.WriteLine("-".PadRight(70, '-'));
        Console.WriteLine($"  Max Depth: {result.MaxDepth.ToMeters():F1}m");
        Console.WriteLine($"  Bottom Time: {result.BottomTimeSeconds / 60} min");
        Console.WriteLine($"  Total Dive Time: {result.TotalDiveTimeSeconds / 60} min");
        Console.WriteLine($"  Total Deco Time: {result.TotalDecoTimeSeconds / 60} min");
        Console.WriteLine($"  TTS:  {result.TtsSeconds / 60} min");
        Console.WriteLine($"  Is Deco Dive: {result.IsDecoDive}");
        Console.WriteLine();

        if (result.DecoStops.Count > 0)
        {
            Console.WriteLine("DECOMPRESSION STOPS:");
            Console.WriteLine($"  {"Depth",-8} {"Time",-12} {"Cumulative"}");
            var cumulative = 0;
            foreach (var stop in result.DecoStops.OrderByDescending(s => s.DepthMm))
            {
                cumulative += stop.TimeSeconds;
                Console.WriteLine(
                    $"  {stop.DepthMeters,4:F0}m     {stop.TimeMinutes,3} min       {cumulative / 60} min");
            }

            Console.WriteLine();
        }

        Console.WriteLine("DIVE PROFILE:");
        Console.WriteLine($"{"Depth",-8} {"Runtime",-10} {"Gas",-12} {"Segment"}");
        Console.WriteLine("  " + "-".PadRight(50, '-'));
        foreach (var wp in result.Waypoints)
        {
            if (wp.SegmentType == SegmentType.Ascent && wp.Depth.ToMeters() != 0)
            {
                continue;
            }

            Console.WriteLine(
                $"{wp.Depth.ToMeters(),5:F0}m {wp.Runtime,-10} {wp.Gas.Name,-12} {wp.SegmentType}");
        }

        Console.WriteLine();
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine();
    }
}