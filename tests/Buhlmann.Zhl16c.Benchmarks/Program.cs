// Arrange

using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Settings;
using Buhlmann.Zhl16c.Utilities;

PlanNoDeco();
PlanDeco45();

return;


void PlanNoDeco()
{
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
    var plan = DecoPlanner.Plan(cylinders, waypoints, settings, context);
    
    Console.WriteLine(plan.ToString());
}
void PlanDeco45()
{
    Cylinder[] cylinders =
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
            DecoPo2Mbar = 1600
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
    var plan = DecoPlanner.Plan(cylinders, waypoints, settings, context);
    
    Console.WriteLine(plan.ToString());
}