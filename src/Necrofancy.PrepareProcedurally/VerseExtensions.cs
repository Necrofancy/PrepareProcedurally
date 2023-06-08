using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Noise;

namespace Necrofancy.PrepareProcedurally
{
    /// <summary>
    /// Common extensions for some common Verse/Rimworld classes
    /// </summary>
    public static class VerseExtensions
    {
        public static float Integral(this SimpleCurve curve)
        {
            float areaUnderCurve = 0;
            var current = curve.Points[0];
            for (int i = 1; i < curve.PointsCount; i++)
            {
                var next = curve.Points[i];
                float diffY = Math.Abs(next.y - current.y);
                float min = Math.Min(current.y, next.y);
                float diffX = next.x - current.x;

                areaUnderCurve += diffX * (diffY * 0.5f + min);

                current = next;
            }

            return areaUnderCurve;
        }

        public static float AssumingPercentRoll(this SimpleCurve curve, float bound)
        {
            float area = curve.Integral();
            float target = area * bound;
            
            var current = curve.Points[0];
            for (int i = 1; i < curve.PointsCount; i++)
            {
                var next = curve.Points[i];
                float diffY = Math.Abs(next.y - current.y);
                float diffX = next.x - current.x;
                float minY = Math.Min(current.y, next.y);

                float currentArea = diffX * (diffY * 0.5f + minY);
                
                if (target >= 0)
                {
                    if (currentArea > target)
                        return current.x + diffX * (target / currentArea);
                    target -= currentArea;
                }

                current = next;
            }

            return curve.Points[curve.PointsCount-1].x;
        }

        public static float AssumingPercentRoll(this FloatRange range, float percent)
        {
            return range.min + (range.max - range.min) * percent;
        }

        public static float AssumingPercentRoll(this IntRange range, float percent)
        {
            int diff = range.max - range.min;
            int diffInt = (int)Math.Round(diff * percent, MidpointRounding.AwayFromZero);
            return range.min + diffInt;
        }

        public static string AsFireEmojis(this Passion passion)
        {
            return passion == Passion.Major
                ? "🔥🔥"
                : passion == Passion.Minor
                    ? "🔥"
                    : string.Empty;
        }
    }
}