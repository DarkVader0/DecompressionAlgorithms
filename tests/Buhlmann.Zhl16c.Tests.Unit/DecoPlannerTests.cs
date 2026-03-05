using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class DecoPlannerTests
{
    // testplan.cpp SUBSURFACE TESTS
    [Fact]
    public void Plan_ShouldReturnValidPlan_Test()
    {
        // Arrange
        Cylinder[] cylinders =
        [
            new()
            {
                O2Permille = 250,
                HePermille = 0,
                SizeMl = 24000,
                StartPressureMbar = 200000
            }
        ];
        var settings = new PlannerSettings
        {
            Deco = new DecoSettings
            {
                GFLow = 50,
                GFHigh = 70
            },
            Gas = new GasSettings
            {
                BottomPo2Mbar = 1400,
                DecoPo2Mbar = 1400
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
                MinSwitchDurationSec = 4,
                ProblemSolvingTimeMin = 4,
                SwitchAtRequiredStop = false
            },
            Environment = new EnvironmentSettings
            {
                SurfacePressureMbar = 1013,
                WaterType = WaterType.Salt
            }
        };
        var context = new DiveContext(1013, WaterType.Salt);

        Waypoint[] waypoints =
        [
            new()
            {
                DepthMm = 45 * 1000, DurationSeconds = 45 * 1000 / (5 * 1000) * 60, CylinderIndex = 0
            },
            new()
            {
                DepthMm = 45 * 1000, DurationSeconds = 40 * 60 - 45 * 1000 / (5 * 1000) * 60, CylinderIndex = 0
            }
        ];

        // Act
        var plan = DecoPlanner.Plan(cylinders, waypoints, settings, context);

        // Assert
        Assert.Equal(20, plan.SegmentCount);

        Assert.Equal(0, plan.Segments[0].DepthStartMm);
        Assert.Equal(45000, plan.Segments[0].DepthEndMm);
        Assert.Equal(540, plan.Segments[0].RuntimeEndSec - plan.Segments[0].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[0].CylinderIndex);

        Assert.Equal(45000, plan.Segments[1].DepthStartMm);
        Assert.Equal(45000, plan.Segments[1].DepthEndMm);
        Assert.Equal(1860, plan.Segments[1].RuntimeEndSec - plan.Segments[1].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[1].CylinderIndex);

        Assert.Equal(45000, plan.Segments[2].DepthStartMm);
        Assert.Equal(42000, plan.Segments[2].DepthEndMm);
        Assert.Equal(38, plan.Segments[2].RuntimeEndSec - plan.Segments[2].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[2].CylinderIndex);

        Assert.Equal(42000, plan.Segments[3].DepthStartMm);
        Assert.Equal(39000, plan.Segments[3].DepthEndMm);
        Assert.Equal(38, plan.Segments[3].RuntimeEndSec - plan.Segments[3].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[3].CylinderIndex);

        Assert.Equal(39000, plan.Segments[4].DepthStartMm);
        Assert.Equal(36000, plan.Segments[4].DepthEndMm);
        Assert.Equal(38, plan.Segments[4].RuntimeEndSec - plan.Segments[4].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[4].CylinderIndex);

        Assert.Equal(36000, plan.Segments[5].DepthStartMm);
        Assert.Equal(33000, plan.Segments[5].DepthEndMm);
        Assert.Equal(38, plan.Segments[5].RuntimeEndSec - plan.Segments[5].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[5].CylinderIndex);

        Assert.Equal(33000, plan.Segments[6].DepthStartMm);
        Assert.Equal(30000, plan.Segments[6].DepthEndMm);
        Assert.Equal(38, plan.Segments[6].RuntimeEndSec - plan.Segments[6].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[6].CylinderIndex);

        Assert.Equal(30000, plan.Segments[7].DepthStartMm);
        Assert.Equal(27000, plan.Segments[7].DepthEndMm);
        Assert.Equal(38, plan.Segments[7].RuntimeEndSec - plan.Segments[7].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[7].CylinderIndex);

        Assert.Equal(27000, plan.Segments[8].DepthStartMm);
        Assert.Equal(24000, plan.Segments[8].DepthEndMm);
        Assert.Equal(38, plan.Segments[8].RuntimeEndSec - plan.Segments[8].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[8].CylinderIndex);

        Assert.Equal(24000, plan.Segments[9].DepthStartMm);
        Assert.Equal(21000, plan.Segments[9].DepthEndMm);
        Assert.Equal(38, plan.Segments[9].RuntimeEndSec - plan.Segments[9].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[9].CylinderIndex);

        Assert.Equal(21000, plan.Segments[10].DepthStartMm);
        Assert.Equal(18000, plan.Segments[10].DepthEndMm);
        Assert.Equal(38, plan.Segments[10].RuntimeEndSec - plan.Segments[10].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[10].CylinderIndex);

        Assert.Equal(18000, plan.Segments[11].DepthStartMm);
        Assert.Equal(15000, plan.Segments[11].DepthEndMm);
        Assert.Equal(38, plan.Segments[11].RuntimeEndSec - plan.Segments[11].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[11].CylinderIndex);

        Assert.Equal(15000, plan.Segments[12].DepthStartMm);
        Assert.Equal(15000, plan.Segments[12].DepthEndMm);
        Assert.Equal(280, plan.Segments[12].RuntimeEndSec - plan.Segments[12].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[12].CylinderIndex);

        Assert.Equal(15000, plan.Segments[13].DepthStartMm);
        Assert.Equal(12000, plan.Segments[13].DepthEndMm);
        Assert.Equal(38, plan.Segments[13].RuntimeEndSec - plan.Segments[13].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[13].CylinderIndex);

        Assert.Equal(12000, plan.Segments[14].DepthStartMm);
        Assert.Equal(12000, plan.Segments[14].DepthEndMm);
        Assert.Equal(382, plan.Segments[14].RuntimeEndSec - plan.Segments[14].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[14].CylinderIndex);

        Assert.Equal(12000, plan.Segments[15].DepthStartMm);
        Assert.Equal(9000, plan.Segments[15].DepthEndMm);
        Assert.Equal(38, plan.Segments[15].RuntimeEndSec - plan.Segments[15].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[15].CylinderIndex);

        Assert.Equal(9000, plan.Segments[16].DepthStartMm);
        Assert.Equal(9000, plan.Segments[16].DepthEndMm);
        Assert.Equal(742, plan.Segments[16].RuntimeEndSec - plan.Segments[16].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[16].CylinderIndex);

        Assert.Equal(9000, plan.Segments[17].DepthStartMm);
        Assert.Equal(6000, plan.Segments[17].DepthEndMm);
        Assert.Equal(38, plan.Segments[17].RuntimeEndSec - plan.Segments[17].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[17].CylinderIndex);

        Assert.Equal(6000, plan.Segments[18].DepthStartMm);
        Assert.Equal(6000, plan.Segments[18].DepthEndMm);
        Assert.Equal(5362, plan.Segments[18].RuntimeEndSec - plan.Segments[18].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[18].CylinderIndex);

        Assert.Equal(6000, plan.Segments[19].DepthStartMm);
        Assert.Equal(0, plan.Segments[19].DepthEndMm);
        Assert.Equal(376, plan.Segments[19].RuntimeEndSec - plan.Segments[19].RuntimeStartSec);
        Assert.Equal(0, plan.Segments[19].CylinderIndex);
    }
}