using Buhlmann.Zhl16c.Constants;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Helpers;
using Buhlmann.Zhl16c.Input;
using Buhlmann.Zhl16c.Output;
using Buhlmann.Zhl16c.Settings;

namespace Buhlmann.Zhl16c.Utilities;

public static class DecoPlanner
{
    private const int BaseTimestep = 2;
    private const int DefaultTimestep = 60;
    private const int MaxSegments = 128;

    public static PlanResult Plan(
        ReadOnlySpan<Cylinder> cylinders,
        ReadOnlySpan<Waypoint> waypoints,
        PlannerSettings settings,
        DiveContext context)
    {
        var result = new PlanResult
        {
            Segments = new PlanSegment[MaxSegments],
            CylinderResults = new CylinderResult[cylinders.Length],
            CylinderCount = (ushort)cylinders.Length,
            Error = PlanError.Ok
        };

        var segmentCount = 0;
        var surfacePressureBar = context.SurfacePressureMbar / 1000.0;
        var ds = DecoState.CreateAtSurface(surfacePressureBar);
        var gfLow = settings.Deco.GFLow / 100.0;
        var gfHigh = settings.Deco.GFHigh / 100.0;

        var bottomGasIdx = GasSelector.FindBottomGas(cylinders);
        if (bottomGasIdx < 0)
        {
            result.Error = PlanError.InvalidInput;
            return result;
        }

        var diveMode = settings.Rebreather.DiveMode;
        var setpointMbar = diveMode == DiveMode.CCR ? settings.Rebreather.SetpointMbar : 0;

        uint depthMm = 0;
        var clock = 0;
        uint maxDepthMm = 0;
        long depthTimeIntegral = 0;

        foreach (var waypoint in waypoints)
        {
            ref readonly var wp = ref waypoint;
            var cylIdx = wp.CylinderIndex >= 0 ? wp.CylinderIndex : bottomGasIdx;
            var mix = new GasMix(cylinders[cylIdx].O2Permille, cylinders[cylIdx].HePermille);

            var startDepthMm = depthMm;
            var endDepthMm = wp.DepthMm;
            var duration = wp.DurationSeconds;

            for (var sec = 0; sec < duration; sec++)
            {
                var interpDepthMm = startDepthMm +
                                    (uint)((endDepthMm - startDepthMm) * sec / duration);

                ds.AddSegment(
                    context.DepthToBar(interpDepthMm),
                    mix, 1, diveMode, setpointMbar);
            }

            depthTimeIntegral += (startDepthMm + endDepthMm) * duration / 2;

            result.Segments[segmentCount++] = new PlanSegment
            {
                RuntimeStartSec = (uint)clock,
                RuntimeEndSec = (uint)(clock + duration),
                DepthStartMm = startDepthMm,
                DepthEndMm = endDepthMm,
                CylinderIndex = (byte)cylIdx,
                SegmentType = endDepthMm > startDepthMm
                    ? SegmentType.Descent
                    : endDepthMm < startDepthMm
                        ? SegmentType.Ascent
                        : SegmentType.Bottom,
                DiveMode = diveMode,
                SetpointMbar = (ushort)setpointMbar
            };

            clock += duration;
            depthMm = endDepthMm;

            if (depthMm > maxDepthMm)
            {
                maxDepthMm = depthMm;
            }
        }

        var bottomTime = clock;
        var avgDepthMm = clock > 0 ? (uint)(depthTimeIntegral / clock) : 0;
        var currentCylIdx = bottomGasIdx;
        var currentMix = new GasMix(
            cylinders[currentCylIdx].O2Permille,
            cylinders[currentCylIdx].HePermille);

        if (diveMode is DiveMode.CCR or DiveMode.PSCR && settings.Rebreather.DoBailout)
        {
            diveMode = DiveMode.OC;
            setpointMbar = 0;

            var bailoutSec = Math.Max(
                settings.Stops.MinSwitchDurationSec,
                60 * settings.Stops.ProblemSolvingTimeMin);

            var bailoutIdx = GasSelector.FindBailoutGas(
                cylinders, depthMm, settings.Gas.BottomPo2Mbar, context);

            if (bailoutIdx >= 0)
            {
                currentCylIdx = bailoutIdx;
                currentMix = new GasMix(
                    cylinders[currentCylIdx].O2Permille,
                    cylinders[currentCylIdx].HePermille);
            }
            else
            {
                result.Error = PlanError.NoBailoutGas;
            }

            ds.AddSegment(
                context.DepthToBar(depthMm), currentMix,
                bailoutSec, diveMode, 0);

            result.Segments[segmentCount++] = new PlanSegment
            {
                RuntimeStartSec = (uint)clock,
                RuntimeEndSec = (uint)(clock + bailoutSec),
                DepthStartMm = depthMm,
                DepthEndMm = depthMm,
                CylinderIndex = (byte)currentCylIdx,
                SegmentType = SegmentType.Bottom,
                DiveMode = diveMode
            };

            clock += bailoutSec;
            bottomTime += bailoutSec;
        }

        Span<GasSelector.GasChange> gasChanges = stackalloc GasSelector.GasChange[cylinders.Length];
        var gasChangeCount = (int)GasSelector.BuildGasChangeList(
            cylinders, maxDepthMm, settings.Gas.DecoPo2Mbar, context, gasChanges);
        var gi = gasChangeCount - 1;

        var stopLevels = DecoStopLevels.Mm;

        var minStopIdx = settings.Stops.LastStopAt6m ? 2 : 1;

        var stopIdx = 0;
        for (var i = 0; i < stopLevels.Length; i++)
        {
            if (stopLevels[i] >= depthMm)
            {
                break;
            }

            stopIdx = i;
        }

        ds.CeilingMm(gfLow, gfHigh, context);

        var lastStopTime = DefaultTimestep;
        var ascentStartClock = clock;

        var o2breaking = false;
        var o2breakNext = false;
        var breakFromCylIdx = -1;
        var o2BreakDurationSec = settings.Stops.O2BreakDurationSec > 0
            ? settings.Stops.O2BreakDurationSec
            : 12 * 60;
        var backgasBreakDurationSec = settings.Stops.BackgasBreakDurationSec > 0
            ? settings.Stops.BackgasBreakDurationSec
            : 6 * 60;

        var breakCylIdx = bottomGasIdx;

        while (depthMm > 0 && result.Error == PlanError.Ok)
        {
            var nextStopMm = stopIdx >= minStopIdx ? stopLevels[stopIdx] : 0u;
            var ascentStartDepth = depthMm;

            while (depthMm > nextStopMm)
            {
                var rateMmSec = AscentRate.GetAscentRate(depthMm, avgDepthMm, settings.AscentDescent);
                var deltad = (uint)(rateMmSec * BaseTimestep);

                if (depthMm - deltad < nextStopMm)
                {
                    deltad = depthMm - nextStopMm;
                }

                ds.AddSegment(
                    context.DepthToBar(depthMm), currentMix,
                    BaseTimestep, diveMode, setpointMbar);

                clock += BaseTimestep;
                depthMm -= deltad;
            }

            if (depthMm != ascentStartDepth)
            {
                result.Segments[segmentCount++] = new PlanSegment
                {
                    RuntimeStartSec = (uint)ascentStartClock,
                    RuntimeEndSec = (uint)clock,
                    DepthStartMm = ascentStartDepth,
                    DepthEndMm = depthMm,
                    CylinderIndex = (byte)currentCylIdx,
                    SegmentType = SegmentType.Ascent,
                    DiveMode = diveMode,
                    SetpointMbar = (ushort)setpointMbar
                };
                ascentStartClock = clock;
            }

            if (depthMm == 0)
            {
                break;
            }

            if (gi >= 0 && depthMm <= gasChanges[gi].DepthMm)
            {
                var newCylIdx = (int)gasChanges[gi].CylinderIndex;
                if (newCylIdx != currentCylIdx)
                {
                    currentCylIdx = newCylIdx;
                    currentMix = new GasMix(
                        cylinders[currentCylIdx].O2Permille,
                        cylinders[currentCylIdx].HePermille);

                    result.Segments[segmentCount++] = new PlanSegment
                    {
                        RuntimeStartSec = (uint)clock,
                        RuntimeEndSec = (uint)clock,
                        DepthStartMm = depthMm,
                        DepthEndMm = depthMm,
                        CylinderIndex = (byte)currentCylIdx,
                        SegmentType = SegmentType.GasSwitch,
                        DiveMode = diveMode,
                        SetpointMbar = (ushort)setpointMbar
                    };

                    if (cylinders[currentCylIdx].O2Permille != 1000 &&
                        settings.Stops.MinSwitchDurationSec > 0)
                    {
                        var switchDur = (int)settings.Stops.MinSwitchDurationSec;
                        ds.AddSegment(
                            context.DepthToBar(depthMm), currentMix,
                            switchDur, diveMode, setpointMbar);
                        clock += switchDur;
                    }
                }

                gi--;
            }

            if (stopIdx > minStopIdx || stopIdx > 0)
            {
                stopIdx--;
            }

            var nextTarget = stopIdx >= minStopIdx ? stopLevels[stopIdx] : 0u;

            if (TrialAscent.IsClearToAscend(
                    ref ds, depthMm, nextTarget, avgDepthMm,
                    currentMix, diveMode, setpointMbar, 0,
                    gfLow, gfHigh, settings.AscentDescent, context))
            {
                continue;
            }

            while (true)
            {
                if (TrialAscent.IsClearToAscend(
                        ref ds, depthMm, nextTarget, avgDepthMm,
                        currentMix, diveMode, setpointMbar, 0,
                        gfLow, gfHigh, settings.AscentDescent, context))
                {
                    break;
                }

                var newClock = WaitUntil.FindClearTime(
                    ref ds, clock, clock, lastStopTime * 2 + 1, DefaultTimestep,
                    depthMm, nextTarget, avgDepthMm,
                    currentMix, diveMode, setpointMbar,
                    gfLow, gfHigh, settings.AscentDescent, context);

                lastStopTime = newClock - clock;

                if (lastStopTime >= 48 * 3600 && depthMm >= 6000 && !o2breaking)
                {
                    result.Error = PlanError.Timeout;
                    break;
                }

                o2breaking = false;
                var stopCylIdx = currentCylIdx;

                if (settings.Stops.DoO2Breaks)
                {
                    if (cylinders[currentCylIdx].O2Permille == 1000)
                    {
                        if (lastStopTime >= o2BreakDurationSec)
                        {
                            lastStopTime = o2BreakDurationSec;
                            o2breaking = true;
                            o2breakNext = true;
                            breakFromCylIdx = currentCylIdx;

                            result.Segments[segmentCount++] = new PlanSegment
                            {
                                RuntimeStartSec = (uint)clock,
                                RuntimeEndSec = (uint)(clock + lastStopTime),
                                DepthStartMm = depthMm,
                                DepthEndMm = depthMm,
                                CylinderIndex = (byte)currentCylIdx,
                                SegmentType = SegmentType.DecoStop,
                                DiveMode = diveMode,
                                SetpointMbar = (ushort)setpointMbar
                            };

                            currentCylIdx = breakCylIdx;
                            currentMix = new GasMix(
                                cylinders[currentCylIdx].O2Permille,
                                cylinders[currentCylIdx].HePermille);
                        }
                    }
                    else if (o2breakNext)
                    {
                        if (lastStopTime >= backgasBreakDurationSec)
                        {
                            lastStopTime = backgasBreakDurationSec;
                            o2breaking = true;
                            o2breakNext = false;

                            result.Segments[segmentCount++] = new PlanSegment
                            {
                                RuntimeStartSec = (uint)clock,
                                RuntimeEndSec = (uint)(clock + lastStopTime),
                                DepthStartMm = depthMm,
                                DepthEndMm = depthMm,
                                CylinderIndex = (byte)currentCylIdx,
                                SegmentType = SegmentType.DecoStop,
                                DiveMode = diveMode,
                                SetpointMbar = (ushort)setpointMbar
                            };

                            currentCylIdx = breakFromCylIdx;
                            currentMix = new GasMix(
                                cylinders[currentCylIdx].O2Permille,
                                cylinders[currentCylIdx].HePermille);
                        }
                    }
                }

                var stopMix = new GasMix(
                    cylinders[stopCylIdx].O2Permille,
                    cylinders[stopCylIdx].HePermille);

                ds.AddSegment(
                    context.DepthToBar(depthMm), stopMix,
                    lastStopTime, diveMode, setpointMbar);

                if (!o2breaking)
                {
                    result.Segments[segmentCount++] = new PlanSegment
                    {
                        RuntimeStartSec = (uint)clock,
                        RuntimeEndSec = (uint)(clock + lastStopTime),
                        DepthStartMm = depthMm,
                        DepthEndMm = depthMm,
                        CylinderIndex = (byte)stopCylIdx,
                        SegmentType = SegmentType.DecoStop,
                        DiveMode = diveMode,
                        SetpointMbar = (ushort)setpointMbar
                    };
                }

                clock += lastStopTime;
                ascentStartClock = clock;

                if (!o2breaking)
                {
                    break;
                }
            }
        }

        result.SegmentCount = (ushort)segmentCount;
        result.TimeTotalSec = (uint)clock;
        result.BottomTimeSec = (uint)bottomTime;
        result.DecoTimeSec = (uint)(clock - bottomTime);
        result.MaxDepthMm = maxDepthMm;
        result.AvgDepthMm = avgDepthMm;

        return result;
    }
}