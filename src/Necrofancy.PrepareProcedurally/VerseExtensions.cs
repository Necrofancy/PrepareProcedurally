using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

/// <summary>
/// Common extensions for some common Verse/Rimworld classes
/// </summary>
public static class VerseExtensions
{
    private static float Integral(this SimpleCurve curve)
    {
        float areaUnderCurve = 0;
        var current = curve.Points[0];
        for (var i = 1; i < curve.PointsCount; i++)
        {
            var next = curve.Points[i];
            var diffY = Math.Abs(next.y - current.y);
            var min = Math.Min(current.y, next.y);
            var diffX = next.x - current.x;

            areaUnderCurve += diffX * (diffY * 0.5f + min);

            current = next;
        }

        return areaUnderCurve;
    }

    public static float AssumingPercentRoll(this SimpleCurve curve, float bound)
    {
        var area = curve.Integral();
        var target = area * bound;
            
        var current = curve.Points[0];
        for (var i = 1; i < curve.PointsCount; i++)
        {
            var next = curve.Points[i];
            var diffY = Math.Abs(next.y - current.y);
            var diffX = next.x - current.x;
            var minY = Math.Min(current.y, next.y);

            var currentArea = diffX * (diffY * 0.5f + minY);
                
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
        var diff = range.max - range.min;
        var diffInt = (int)Math.Round(diff * percent, MidpointRounding.AwayFromZero);
        return range.min + diffInt;
    }

    public static string AsFireEmojis(this Passion passion)
    {
        return passion switch
        {
            Passion.Major => "🔥🔥",
            Passion.Minor => "🔥",
            _ => string.Empty
        };
    }

    public static bool IsSexualityTrait(this TraitDef trait)
    {
        return trait.exclusionTags.Contains("SexualOrientation");
    }
        
    public static bool AllowsTrait(this IReadOnlyCollection<TraitRequirement> requirements, TraitDef traitDef)
    {
        return requirements.All(requiredTrait => requiredTrait.def != traitDef && !requiredTrait.def.ConflictsWith(traitDef));
    }

    public static bool AllowsTrait(this TraitRequirement requiredTrait, TraitDef traitDef)
    {
        return requiredTrait.def == traitDef || requiredTrait.def.ConflictsWith(traitDef);
    }
}