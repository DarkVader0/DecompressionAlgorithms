namespace Buhlmann.Zhl16c.Constants;

public static class DecoStopLevels
{
    public const uint StepMm = 3000;

    public static readonly uint[] Mm =
    {
        0, 3000, 6000, 9000, 12000, 15000, 18000, 21000, 24000, 27000,
        30000, 33000, 36000, 39000, 42000, 45000, 48000, 51000, 54000, 57000,
        60000, 63000, 66000, 69000, 72000, 75000, 78000, 81000, 84000, 87000,
        90000, 100000, 110000, 120000, 130000, 140000, 150000, 160000, 170000,
        180000, 190000, 200000, 220000, 240000, 260000, 280000, 300000,
        320000, 340000, 360000, 380000
    };

    public static uint GetNextStopLevel(uint currentDepthMm)
    {
        for (var i = Mm.Length - 1; i >= 0; i--)
        {
            if (Mm[i] < currentDepthMm)
            {
                return Mm[i];
            }
        }

        return 0;
    }

    public static uint RoundUpToStopLevel(uint depthMm)
    {
        foreach (var level in Mm)
        {
            if (level >= depthMm)
            {
                return level;
            }
        }

        return Mm[^1];
    }

    public static int GetStopLevelIndex(uint depthMm)
    {
        for (var i = 0; i < Mm.Length; i++)
        {
            if (Mm[i] == depthMm)
            {
                return i;
            }
        }

        return -1;
    }
}