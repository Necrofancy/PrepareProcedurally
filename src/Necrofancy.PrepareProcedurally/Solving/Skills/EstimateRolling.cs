using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

/// <summary>
/// Estimates a given bound of rolling for a pawn skill.
/// In order to determine passions (which are doled out to the N highest skills) there is a bias factor applied
/// to get the "best" rolls possible for skills that need to hit certain passion thresholds.
/// <para>These methods, when accounting for a passion, do so by making each related roll:</para>
/// <para>Major: 95% of the max value.</para>
/// <para>Minor: ~80% of the max value.</para>
/// <para>None: ~50% of the max value.</para>
/// </summary>
public static class EstimateRolling
{
    private static readonly IntRange NoRange = new(0, 20);

    /// <summary>
    /// All skills that usually backstory-related roll 0-4 for a start, as a whole value.
    /// </summary>
    public static readonly IntRange StartingValue = new(0, 4);

    /// <summary>
    /// Starting value for a skill not usually related to backstories. This means Animals in vanilla.
    /// This is randomized using this curve as a basis.
    /// </summary>
    private static readonly SimpleCurve NonBackstoryStartingValueCurve = new()
    {
        new CurvePoint(0f, 0f),
        new CurvePoint(0.5f, 150f),
        new CurvePoint(4f, 150f),
        new CurvePoint(5f, 25f),
        new CurvePoint(10f, 5f),
        new CurvePoint(15f, 0f)
    };

    /// <summary>
    /// Each backstory bonus gets a bonus multiplier.
    /// </summary>
    private static readonly FloatRange BackstoryMultiplier = new(1.0f, 1.4f);

    /// <summary>
    /// A curve measuring X as the biological age of a pawn, multiplied by its age roll,
    /// to determine the skill multiplier for a pawn's skill by the age.
    /// Note because of how <see cref="Rand.Range(float, float)"/> works, for estimating
    /// I am setting the min to 1 where it is not this in the Rimworld assembly.
    /// </summary>
    private static readonly SimpleCurve AgeSkillMaxFactorCurve = new()
    {
        new CurvePoint(0f, 1f),
        new CurvePoint(10f, 1f),
        new CurvePoint(35f, 1f),
        new CurvePoint(60f, 1.6f)
    };

    /// <summary>
    /// Vanilla pawn generation will also taper higher skill rolls. This essentially applies two soft-caps
    /// to natural pawn generation - additional levels past 10 will be penalized 40%, past 16 by ~57%
    /// </summary>
    private static readonly SimpleCurve LevelFinalAdjustmentCurve = new()
    {
        new CurvePoint(0.0f, 0.0f),
        new CurvePoint(10f, 10f),
        new CurvePoint(20f, 16f),
        new CurvePoint(27f, 20f)
    };

    /// <summary>
    /// Given an existing curve, returns a new curve isolating to a specific range.
    /// e.g. if I have an existing age probability curve for humans giving distribution points between 15-100 years
    /// of age, what would the curve be if I were to limited to between 40 and 60?
    /// </summary>
    /// <param name="existingCurve">An existing super-curve.</param>
    /// <param name="range">A minimum and maximum range to clamp the curve to</param>
    /// <returns>A curve that only contains the distribution within the desired range.</returns>
    public static SimpleCurve SubSampleCurve(SimpleCurve existingCurve, IntRange range)
    {
        var newCurve = new SimpleCurve();
        const float distance = 0.1f;
        var almostMin = range.min + distance;
        var almostMax = range.max - distance;

        newCurve.Add(range.min, 0);
        newCurve.Add(almostMin, existingCurve.Evaluate(almostMin));

        foreach (var midpoint in existingCurve.Where(p => range.min < p.x && p.x < range.max)) newCurve.Add(midpoint);

        newCurve.Add(almostMax, existingCurve.Evaluate(almostMax));
        newCurve.Add(range.max, 0);

        return newCurve;
    }

    public static int StaticRoll(in BioPossibility bioPossibility, float age, SkillDef skill, float roll)
    {
        var start = skill.usuallyDefinedInBackstories
            ? StartingValue.AssumingPercentRoll(roll)
            : NonBackstoryStartingValueCurve.AssumingPercentRoll(roll);

        float story = 0;
        var backstoryMultiplier = BackstoryMultiplier.AssumingPercentRoll(roll);

        // if we wanted to "positively" roll on a bad backstory, we want the _inverse_.
        var backstoryNegativeMultiplier = BackstoryMultiplier.AssumingPercentRoll(1 - roll);

        void AddBackstory(BackstoryDef backstory)
        {
            if (!backstory.skillGains.TryGetValue(skill, out var value))
                return;

            var multiplied = value > 0
                ? backstoryMultiplier * value
                : backstoryNegativeMultiplier * value;
            story += multiplied;
        }

        AddBackstory(bioPossibility.Childhood);
        AddBackstory(bioPossibility.Adulthood);

        var ageMax = AgeSkillMaxFactorCurve.Evaluate(age);
        var ageMultiplierRange = new FloatRange(1.0f, ageMax);
        var ageMultiplier = ageMultiplierRange.AssumingPercentRoll(roll);
        var curveValue = (start + story) * ageMultiplier;
        var fromAdjustmentCurve = LevelFinalAdjustmentCurve.Evaluate(curveValue);

        return Mathf.Clamp(Mathf.RoundToInt(fromAdjustmentCurve), 0, 20);
    }

    public static int StaticRoll(Pawn pawn, SkillDef skill, float roll)
    {
        var start = skill.usuallyDefinedInBackstories
            ? StartingValue.AssumingPercentRoll(roll)
            : NonBackstoryStartingValueCurve.AssumingPercentRoll(roll);

        float story = 0;
        var backstoryMultiplier = BackstoryMultiplier.AssumingPercentRoll(roll);

        // if we wanted to "positively" roll on a bad backstory, we want the _inverse_.
        var backstoryNegativeMultiplier = BackstoryMultiplier.AssumingPercentRoll(1 - roll);

        void AddBackstory(BackstoryDef backstory)
        {
            if (!backstory.skillGains.TryGetValue(skill, out var value))
                return;

            var multiplied = value > 0
                ? backstoryMultiplier * value
                : backstoryNegativeMultiplier * value;
            story += multiplied;
        }

        AddBackstory(pawn.story.Childhood);
        AddBackstory(pawn.story.Adulthood);

        foreach (var trait in pawn.story.traits.allTraits)
            if (trait.CurrentData.skillGains is { } gains && gains.TryGetValue(skill, out var gain))
                story += gain;

        var ageMax = AgeSkillMaxFactorCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
        var ageMultiplierRange = new FloatRange(1.0f, ageMax);
        var ageMultiplier = ageMultiplierRange.AssumingPercentRoll(roll);
        var curveValue = (start + story) * ageMultiplier;
        var fromAdjustmentCurve = LevelFinalAdjustmentCurve.Evaluate(curveValue);

        return Mathf.Clamp(Mathf.RoundToInt(fromAdjustmentCurve), 0, 20);
    }

    public static IReadOnlyDictionary<SkillDef, IntRange> PossibleSkillRangesOf(float age,
        in BioPossibility possibility)
    {
        var dict = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = StaticRoll(in possibility, age, skill, 0f);
            var max = StaticRoll(in possibility, age, skill, .98f);
            dict[skill] = new IntRange(min, max);
        }

        return dict;
    }
}