using System.Runtime.CompilerServices;
using Buhlmann.Zhl16c.Enums;
using Buhlmann.Zhl16c.Input;

namespace Buhlmann.Zhl16c.Helpers;

public static class GasSelector
{
    public static uint BuildGasChangeList(
        ReadOnlySpan<Cylinder> cylinders,
        uint maxDepthMm,
        uint decoPo2Mbar,
        DiveContext context,
        Span<GasChange> gasChanges)
    {
        var count = 0;

        for (var i = 0; i < cylinders.Length && count < gasChanges.Length; i++)
        {
            ref readonly var cyl = ref cylinders[i];

            if (cyl.Use != CylinderUse.Deco)
            {
                continue;
            }

            var mix = new GasMix(cyl.O2Permille, cyl.HePermille);
            var modMm = context.GasModMm(mix, decoPo2Mbar, 3000);

            if (modMm >= maxDepthMm)
            {
                continue;
            }

            var insertPos = count;
            for (var j = 0; j < count; j++)
            {
                if (modMm <= gasChanges[j].DepthMm)
                {
                    continue;
                }

                insertPos = j;
                break;
            }

            for (var j = count; j > insertPos; j--)
            {
                gasChanges[j] = gasChanges[j - 1];
            }

            gasChanges[insertPos] = new GasChange(modMm, (ushort)i);
            count++;
        }

        return (uint)count;
    }

    public static int FindBottomGas(ReadOnlySpan<Cylinder> cylinders)
    {
        for (var i = 0; i < cylinders.Length; i++)
        {
            if (cylinders[i].Use == CylinderUse.Bottom)
            {
                return i;
            }
        }

        return cylinders.Length > 0 ? 0 : -1;
    }

    public static int FindBestAscentGas(
        ReadOnlySpan<Cylinder> cylinders,
        uint currentDepthMm,
        uint decoPo2Mbar,
        DiveContext context)
    {
        var bestIndex = -1;
        var shallowestMod = uint.MaxValue;

        for (var i = 0; i < cylinders.Length; i++)
        {
            ref readonly var cyl = ref cylinders[i];

            if (cyl.Use != CylinderUse.Deco
                && cyl.Use != CylinderUse.Bottom
                && cyl.Use != CylinderUse.Bailout)
            {
                continue;
            }

            var mix = new GasMix(cyl.O2Permille, cyl.HePermille);
            var modMm = context.GasModMm(mix, decoPo2Mbar, 3000);

            if (modMm >= currentDepthMm && modMm < shallowestMod)
            {
                shallowestMod = modMm;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindNextGasChange(
        ReadOnlySpan<GasChange> gasChanges,
        int count,
        int currentDepthMm)
    {
        for (var i = 0; i < count; i++)
        {
            if (gasChanges[i].DepthMm <= currentDepthMm)
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGasSafeAtDepth(
        GasMix mix,
        uint depthMm,
        uint maxPo2Mbar,
        DiveContext context)
    {
        return context.PO2Mbar(mix, depthMm) <= maxPo2Mbar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGasBreathable(
        GasMix mix,
        uint depthMm,
        DiveContext context)
    {
        return context.PO2Mbar(mix, depthMm) >= 160;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareGasDepth(GasMix a, GasMix b)
    {
        if (a.O2Permille < b.O2Permille)
        {
            return -1;
        }

        if (a.O2Permille > b.O2Permille)
        {
            return 1;
        }

        if (a.HePermille > b.HePermille)
        {
            return -1;
        }

        if (a.HePermille < b.HePermille)
        {
            return 1;
        }

        return 0;
    }

    public static int FindBailoutGas(
        ReadOnlySpan<Cylinder> cylinders,
        uint depthMm,
        uint bottomPo2Mbar,
        DiveContext context)
    {
        var bestIndex = -1;
        var bestO2 = 0;

        for (var i = 0; i < cylinders.Length; i++)
        {
            ref readonly var cyl = ref cylinders[i];

            if ((cyl.Use == CylinderUse.Diluent && cyl.Use != CylinderUse.Bailout) ||
                cyl.Use == CylinderUse.Oxygen)
            {
                continue;
            }

            var mix = new GasMix(cyl.O2Permille, cyl.HePermille);

            if (!IsGasSafeAtDepth(mix, depthMm, bottomPo2Mbar, context))
            {
                continue;
            }

            if (!IsGasBreathable(mix, depthMm, context))
            {
                continue;
            }

            if (mix.O2Permille > bestO2)
            {
                bestO2 = mix.O2Permille;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckIcd(GasMix oldMix,
        GasMix newMix,
        out int dN2,
        out int dHe)
    {
        dN2 = newMix.N2Permille - oldMix.N2Permille;
        dHe = newMix.HePermille - oldMix.HePermille;

        return oldMix.HePermille > 0 && dN2 > 0 && dHe < 0 && 5 * dN2 > -dHe;
    }

    public readonly struct GasChange
    {
        public readonly uint DepthMm;
        public readonly ushort CylinderIndex;

        public GasChange(uint depthMm, ushort cylinderIndex)
        {
            DepthMm = depthMm;
            CylinderIndex = cylinderIndex;
        }
    }
}