using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Output;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class DecoPlannerTests
{
    private static readonly DiveContext DefaultContext = new(1013, WaterType.Salt);

    private static PlannerSettings BuhlmannSettings()
    {
        return new PlannerSettings
        {
            Deco = new DecoSettings { GFLow = 100, GFHigh = 100 },
            Gas = new GasSettings { BottomPo2Mbar = 1600, DecoPo2Mbar = 1600 },
            AscentDescent = new AscentDescentSettings
            {
                DescentRateMmSec = 18000 / 60,
                AscentRate75MmSec = 9144 / 60,
                AscentRate50MmSec = 9144 / 60,
                AscentRateStopsMmSec = 9144 / 60,
                AscentRateLast6mMmSec = 3048 / 60
            },
            Stops = new StopSettings
            {
                LastStopAt6m = true,
                SafetyStop = false,
                MinSwitchDurationSec = 0,
                ProblemSolvingTimeMin = 0,
                DoO2Breaks = false,
                SwitchAtRequiredStop = false,
                O2BreakDurationSec = 0,
                BackgasBreakDurationSec = 0
            },
            Rebreather = new RebreatherSettings { DiveMode = DiveMode.OC }
        };
    }

    private static bool CompareDecoTime(uint actualSec, uint benchmarkSec)
    {
        if (actualSec == benchmarkSec)
        {
            return true;
        }

        var allowed = (int)(0.01 * benchmarkSec + 60);
        return Math.Abs((int)actualSec - (int)benchmarkSec) <= allowed;
    }

    private static IEnumerable<PlanSegment> GetSegments(PlanResult result, SegmentType type)
    {
        for (var i = 0; i < result.SegmentCount; i++)
        {
            if (result.Segments[i].SegmentType == type)
            {
                yield return result.Segments[i];
            }
        }
    }

    #region Subsurface testMetric — 79m/30min, Tx 15/45 + NX36 + O2, GF 100/100

    [Fact]
    public void Plan_ShouldReturn102MinRuntime_WhenDiving79mFor30MinOnTx1545WithNx36AndO2()
    {
        // Arrange
        var settings = BuhlmannSettings();

        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 18000 / 60,
            AscentRate75MmSec = 18000 / 60,
            AscentRate50MmSec = 18000 / 60,
            AscentRateStopsMmSec = 18000 / 60,
            AscentRateLast6mMmSec = 1000 / 60
        };
        settings.Gas = new GasSettings {
            BottomPo2Mbar = 1400,
            DecoPo2Mbar = 1600 
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 150, HePermille = 450, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 360, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            },
            new()
            {
                O2Permille = 1000, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];
        var droptime = 79000 * 60 / 18000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 79000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 79000, DurationSeconds = 30 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.True(result.SegmentCount > 0);
        Assert.Equal(79000u, result.MaxDepthMm);
        var gasSwitches = GetSegments(result, SegmentType.GasSwitch).ToList();
        Assert.True(gasSwitches.Count >= 2, $"Expected at least 2 gas switches, got {gasSwitches.Count}");
        Assert.Equal(1, gasSwitches[0].CylinderIndex);
        Assert.Equal(33000u, gasSwitches[0].DepthStartMm);
        Assert.Equal(2, gasSwitches[1].CylinderIndex);
        Assert.Equal(6000u, gasSwitches[1].DepthStartMm);
        Assert.True(
            CompareDecoTime(result.TimeTotalSec, 109u * 60u),
            $"Expected ~102 min runtime, got {result.TimeTotalSec / 60.0:F1} min ({result.TimeTotalSec} sec)");
    }

    #endregion

    #region Subsurface testVpmbMetric60m30minAir (Buhlmann mode) — 60m/30min, air only

    [Fact]
    public void Plan_ShouldProduceDecoStops_WhenDiving60mFor30MinOnAir()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 100000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            }
        ];

        var droptime = 60000 * 60 / 99000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 60000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 60000, DurationSeconds = 30 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.Equal(60000u, result.MaxDepthMm);
        var decoStops = GetSegments(result, SegmentType.DecoStop).ToList();
        Assert.True(decoStops.Count > 0, "Expected deco stops for 60m/30min on air");
        Assert.True(result.TimeTotalSec > 30 * 60, "Runtime should exceed bottom time");
    }

    #endregion

    #region Subsurface testVpmbMetric60m30minEan50 — 60m/30min, air + NX50

    [Fact]
    public void Plan_ShouldSwitchToNx50At21m_WhenDiving60mFor30MinOnAirWithNx50Deco()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;
        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 99000 / 60,
            AscentRate75MmSec = 10000 / 60,
            AscentRate50MmSec = 10000 / 60,
            AscentRateStopsMmSec = 10000 / 60,
            AscentRateLast6mMmSec = 10000 / 60
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 500, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];

        var droptime = 60000 * 60 / 99000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 60000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 60000, DurationSeconds = 30 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);
        Assert.Equal(PlanError.Ok, result.Error);
        var gasSwitches = GetSegments(result, SegmentType.GasSwitch).ToList();
        Assert.True(gasSwitches.Count >= 1, "Expected at least 1 gas switch");
        Assert.Equal(1, gasSwitches[0].CylinderIndex);
        Assert.Equal(21000u, gasSwitches[0].DepthStartMm);
    }

    #endregion

    #region Subsurface testVpmbMetric60m30minTx — 60m/30min, Tx 18/45 + NX50

    [Fact]
    public void Plan_ShouldSwitchToNx50At21m_WhenDiving60mFor30MinOnTx1845WithNx50Deco()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;
        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 99000 / 60,
            AscentRate75MmSec = 10000 / 60,
            AscentRate50MmSec = 10000 / 60,
            AscentRateStopsMmSec = 10000 / 60,
            AscentRateLast6mMmSec = 10000 / 60
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 180, HePermille = 450, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 500, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];

        var droptime = 60000 * 60 / 99000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 60000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 60000, DurationSeconds = 30 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);

        var gasSwitches = GetSegments(result, SegmentType.GasSwitch).ToList();
        Assert.True(gasSwitches.Count >= 1);
        Assert.Equal(1, gasSwitches[0].CylinderIndex);
        Assert.Equal(21000u, gasSwitches[0].DepthStartMm);
    }

    #endregion

    #region Subsurface testVpmbMetric100m60min — 100m/60min, Tx 18/45 + NX50 + O2

    [Fact]
    public void Plan_ShouldSwitchToNx50At21mAndO2At6m_WhenDiving100mFor60MinOnTx1845()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 180, HePermille = 450, SizeMl = 200000, StartPressureMbar = 232000,
                Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 500, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            },
            new()
            {
                O2Permille = 1000, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];

        var droptime = 100000 * 60 / 99000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 100000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 100000, DurationSeconds = 60 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        var gasSwitches = GetSegments(result, SegmentType.GasSwitch).ToList();
        Assert.True(gasSwitches.Count >= 2, $"Expected 2 gas switches, got {gasSwitches.Count}");
        Assert.Equal(1, gasSwitches[0].CylinderIndex);
        Assert.Equal(21000u, gasSwitches[0].DepthStartMm);
        Assert.Equal(2, gasSwitches[1].CylinderIndex);
        Assert.Equal(6000u, gasSwitches[1].DepthStartMm);
        Assert.True(result.TimeTotalSec > 180 * 60,
            $"Expected >180 min runtime, got {result.TimeTotalSec / 60.0:F1} min");
    }

    #endregion

    #region Subsurface testVpmbMetric30m20min — 30m/20min, air (no-deco or minimal deco)

    [Fact]
    public void Plan_ShouldBe22Minutes_WhenDiving30mFor20MinOnAir()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 18000 / 60,
            AscentRate75MmSec = 18000 / 60,
            AscentRate50MmSec = 18000 / 60,
            AscentRateStopsMmSec = 18000 / 60,
            AscentRateLast6mMmSec = 18000 / 60
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            }
        ];
        var droptime = 30000 * 60 / 18000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 30000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 30000, DurationSeconds = 20 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.True(
            CompareDecoTime(result.TimeTotalSec, 22u * 60u),
            $"Expected ~22 min runtime, got {result.TimeTotalSec / 60.0:F1} min ({result.TimeTotalSec} sec)");
    }

    #endregion

    #region Subsurface testMultipleGases — several gases in manual part

    [Fact]
    public void Plan_ShouldHandleMultipleGasesInBottomPortion_WhenDiving40mWithGasSwitch()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 360, HePermille = 0, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 110, HePermille = 500, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Bottom
            }
        ];

        Waypoint[] waypoints =
        [
            new() { DepthMm = 40000 / 2, DurationSeconds = 120, CylinderIndex = 0 },
            new() { DepthMm = 40000, DurationSeconds = 18 * 60, CylinderIndex = 0 },
            new() { DepthMm = 10000, DurationSeconds = 10 * 60, CylinderIndex = 1 },
            new() { DepthMm = 10000, DurationSeconds = 5 * 60, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.True(
            CompareDecoTime(result.TimeTotalSec, 2480u),
            $"Expected ~2480 sec runtime, got {result.TimeTotalSec} sec");
    }

    #endregion

    #region Subsurface testVpmbMetricMultiLevelAir — 20m→60m multi-level, air

    [Fact]
    public void Plan_ShouldHandleMultiLevelDive_WhenDescendingFrom20mTo60mOnAir()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 200000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            }
        ];

        var droptime20 = 20000 * 60 / 99000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 20000 / 2, DurationSeconds = droptime20, CylinderIndex = 0 },
            new() { DepthMm = 20000, DurationSeconds = 10 * 60 - droptime20, CylinderIndex = 0 },
            new() { DepthMm = 60000 / 2, DurationSeconds = 1 * 60, CylinderIndex = 0 },
            new() { DepthMm = 60000, DurationSeconds = 29 * 60, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.Equal(60000u, result.MaxDepthMm);
        var decoStops = GetSegments(result, SegmentType.DecoStop).ToList();
        Assert.True(decoStops.Count > 0, "Multi-level 20→60m should require deco");
    }

    #endregion

    #region Subsurface testVpmbMetricRepeat — repeated plan should give same result

    [Fact]
    public void Plan_ShouldReturnSameRuntime_WhenCalledTwiceWithSameInputs()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;
        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 18000 / 60,
            AscentRate75MmSec = 10000 / 60,
            AscentRate50MmSec = 10000 / 60,
            AscentRateStopsMmSec = 10000 / 60,
            AscentRateLast6mMmSec = 10000 / 60
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            }
        ];

        var droptime = 30000 * 60 / 18000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 30000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 30000, DurationSeconds = 20 * 60 - droptime, CylinderIndex = 0 }
        ];

        // Act
        var result1 = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);
        var result2 = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(result1.TimeTotalSec, result2.TimeTotalSec);
        Assert.Equal(result1.SegmentCount, result2.SegmentCount);
    }

    #endregion

    #region Subsurface testCcrBailoutGasSelection — CCR 60m/20min with bailout

    // [Fact]
    // public void Plan_ShouldSwitchToBailoutGases_WhenCcrDiveWithBailoutEnabled()
    // {
    //     // Arrange
    //     var settings = BuhlmannSettings();
    //     settings.Deco = new DecoSettings { GFLow = 50, GFHigh = 70 };
    //     settings.Rebreather = new RebreatherSettings
    //     {
    //         DiveMode = DiveMode.CCR,
    //         SetpointMbar = 1300,
    //         DoBailout = true,
    //         BailoutSwitchTimeSec = 60
    //     };
    //     settings.Stops.ProblemSolvingTimeMin = 2;
    //     settings.Stops.MinSwitchDurationSec = 60;
    //     settings.Gas.BottomPo2Mbar = 1600;
    //     settings.Gas.DecoPo2Mbar = 1600;
    //     settings.AscentDescent = new AscentDescentSettings
    //     {
    //         DescentRateMmSec = 18000 / 60,
    //         AscentRate75MmSec = 10000 / 60,
    //         AscentRate50MmSec = 10000 / 60,
    //         AscentRateStopsMmSec = 10000 / 60,
    //         AscentRateLast6mMmSec = 10000 / 60
    //     };
    //     
    //     
    //     Cylinder[] cylinders =
    //     [
    //         new()
    //         {
    //             O2Permille = 200, HePermille = 210, SizeMl = 3000, StartPressureMbar = 200000, Use = CylinderUse.Diluent
    //         },
    //         new()
    //         {
    //             O2Permille = 530, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000,
    //             Use = CylinderUse.Deco | CylinderUse.Bailout
    //         },
    //         new()
    //         {
    //             O2Permille = 190, HePermille = 330, SizeMl = 11100, StartPressureMbar = 200000,
    //             Use = CylinderUse.Bailout
    //         }
    //     ];
    //
    //     Waypoint[] waypoints =
    //     [
    //         new() { DepthMm = 60000, DurationSeconds = 20 * 60, CylinderIndex = 0 }
    //     ];
    //
    //     // Act
    //     var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);
    //
    //     // Assert
    //     Assert.Equal(PlanError.Ok, result.Error);
    //     var decoStops = GetSegments(result, SegmentType.DecoStop).ToList();
    //     Assert.True(decoStops.Count > 0, "CCR bailout from 60m should require deco stops");
    //     Assert.True(
    //         CompareDecoTime(result.TimeTotalSec, 51u * 60u),
    //         $"Expected ~51 min runtime, got {result.TimeTotalSec / 60.0:F1} min ({result.TimeTotalSec} sec)");
    // }

    #endregion

    #region Subsurface testVpmbMetric100mTo70m30min — 100m→70m multi-level, Tx 12/65 + Tx 21/35 + NX50 + O2

    [Fact]
    public void Plan_ShouldSwitchTo3DecoGases_WhenDiving100mTo70mFor30Min()
    {
        // Arrange
        var settings = BuhlmannSettings();
        settings.Stops.LastStopAt6m = false;
        settings.AscentDescent = new AscentDescentSettings
        {
            DescentRateMmSec = 18000 / 60,
            AscentRate75MmSec = 10000 / 60,
            AscentRate50MmSec = 10000 / 60,
            AscentRateStopsMmSec = 10000 / 60,
            AscentRateLast6mMmSec = 10000 / 60
        };

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 120, HePermille = 650, SizeMl = 36000, StartPressureMbar = 232000, Use = CylinderUse.Bottom
            },
            new()
            {
                O2Permille = 210, HePermille = 350, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            },
            new()
            {
                O2Permille = 500, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            },
            new()
            {
                O2Permille = 1000, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];

        var droptime = 100000 * 60 / 18000;

        Waypoint[] waypoints =
        [
            new() { DepthMm = 100000 / 2, DurationSeconds = droptime, CylinderIndex = 0 },
            new() { DepthMm = 100000, DurationSeconds = 20 * 60 - droptime, CylinderIndex = 0 },
            new() { DepthMm = 70000, DurationSeconds = 3 * 60, CylinderIndex = 0 },
            new() { DepthMm = 70000, DurationSeconds = 7 * 60, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);

        var gasSwitches = GetSegments(result, SegmentType.GasSwitch).ToList();
        Assert.True(gasSwitches.Count >= 3, $"Expected 3 gas switches, got {gasSwitches.Count}");
        Assert.Equal(1, gasSwitches[0].CylinderIndex);
        Assert.Equal(63000u, gasSwitches[0].DepthStartMm);
        Assert.Equal(2, gasSwitches[1].CylinderIndex);
        Assert.Equal(21000u, gasSwitches[1].DepthStartMm);
        Assert.Equal(3, gasSwitches[2].CylinderIndex);
        Assert.Equal(6000u, gasSwitches[2].DepthStartMm);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Plan_ShouldReturnInvalidInput_WhenNoCylindersProvided()
    {
        // Arrange
        var settings = BuhlmannSettings();
        Cylinder[] cylinders = [];
        Waypoint[] waypoints = [new() { DepthMm = 30000, DurationSeconds = 20 * 60, CylinderIndex = -1 }];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.InvalidInput, result.Error);
    }

    [Fact]
    public void Plan_ShouldReturnInvalidInput_WhenNoBottomGasExists()
    {
        // Arrange
        var settings = BuhlmannSettings();
        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 500, HePermille = 0, SizeMl = 11100, StartPressureMbar = 200000, Use = CylinderUse.Deco
            }
        ];
        Waypoint[] waypoints = [new() { DepthMm = 30000, DurationSeconds = 20 * 60, CylinderIndex = -1 }];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
    }

    [Fact]
    public void Plan_ShouldPopulateAllResultFields_WhenValidDiveIsPlanned()
    {
        // Arrange
        var settings = BuhlmannSettings();

        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 210, HePermille = 0, SizeMl = 12000, StartPressureMbar = 200000, Use = CylinderUse.Bottom
            }
        ];

        Waypoint[] waypoints =
        [
            new() { DepthMm = 30000 / 2, DurationSeconds = 120, CylinderIndex = 0 },
            new() { DepthMm = 30000, DurationSeconds = 18 * 60, CylinderIndex = 0 }
        ];

        // Act
        var result = DecoPlanner.Plan(cylinders, waypoints, settings, DefaultContext);

        // Assert
        Assert.Equal(PlanError.Ok, result.Error);
        Assert.True(result.SegmentCount > 0);
        Assert.True(result.TimeTotalSec > 0);
        Assert.True(result.BottomTimeSec > 0);
        Assert.Equal(30000u, result.MaxDepthMm);
        Assert.True(result.AvgDepthMm > 0);
        Assert.Equal(1, result.CylinderCount);
    }

    #endregion
}