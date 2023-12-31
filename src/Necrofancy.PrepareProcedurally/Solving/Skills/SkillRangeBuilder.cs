using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public abstract class SkillRangeBuilder<T> where T : class
{
    private readonly Dictionary<T, Passion> passions = new();
    private readonly Dictionary<T, IntRange> skillRanges;
    private readonly HashSet<T> forcedPassions;
    private readonly HashSet<T> disallowedPassions;
    private readonly float maxPassionPoints;
    private Passion highestPassion = Passion.Minor;

    protected SkillRangeBuilder(
        Dictionary<T, IntRange> skillRanges,
        HashSet<T> forcedPassions,
        HashSet<T> disallowedPassions,
        float maxPassionPoints)
    {
        this.skillRanges = skillRanges;
        this.forcedPassions = forcedPassions;
        this.disallowedPassions = disallowedPassions;
        this.maxPassionPoints = maxPassionPoints;
        foreach (var (skill, _) in this.skillRanges)
            passions[skill] = Passion.None;
    }

    protected abstract IReadOnlyList<T> GetAllSkillDefinitions();

    public float PassionPoints { get; private set; }

    public int MaxOf(T def)
    {
        return skillRanges[def].max;
    }

    public bool LockIn(T skill, int total, int major, int minor, int remaining)
    {
        if (disallowedPassions.Contains(skill))
            return false;

        var used = total - remaining;
        var currentPassionNeeded =
            used < major
                ? Passion.Major
                : used < major + minor
                    ? Passion.Minor
                    : Passion.None;

        var def = skill;
        var range = skillRanges[def];

        if (!TryLockInPassion(def, currentPassionNeeded))
            return false;

        var newMin = NewMin(range, currentPassionNeeded);

        skillRanges[def] = new IntRange(newMin, range.max);
        if (highestPassion < currentPassionNeeded)
            highestPassion = currentPassionNeeded;
        return true;
    }

    private static int NewMin(IntRange range, Passion currentPassionNeeded)
    {
        // if we need to hard-force a passion that has no bonuses provided by it, make the roll as high as possible
        // a more natural-feeling generation. Otherwise most non-passion ranges are kludged to 0-0 range
        // and then it's entirely obvious that this wasn't randomized.
        var noBonusesButNeedPassion = range.min == 0 && currentPassionNeeded >= Passion.Minor;

        // major passions should probably force a high roll to make minor rolls easier to reason about.
        var storiedMajorPassion = currentPassionNeeded == Passion.Major;

        // minor passions can afford to swing a bit more widely.
        var storiedMinorPassion = currentPassionNeeded == Passion.Minor;

        var newMin =
            noBonusesButNeedPassion
                ? range.max
                : storiedMajorPassion
                    ? Math.Max(range.max - 2, range.min)
                    : storiedMinorPassion
                        ? Math.Max(range.max - 4, range.min)
                        : range.min;
        return newMin;
    }

    public bool TryLockInPassion(T def, Passion passion)
    {
        if (disallowedPassions.Contains(def))
            return false;

        var currentPassion = passions[def];
        if (currentPassion >= passion)
            return true;

        var pointDiff = PointsFor(passion) - PointsFor(currentPassion);
        if (pointDiff >= maxPassionPoints - PassionPoints)
            return false;

        var skillRange = skillRanges[def];
        var newMin = NewMin(skillRange, passion);
        skillRanges[def] = new IntRange(newMin, skillRange.max);

        passions[def] = passion;
        PassionPoints += pointDiff;

        if (highestPassion < passion)
            highestPassion = passion;
        return true;
    }

    private static float PointsFor(Passion passion)
    {
        return passion == Passion.Major
            ? 1.5f
            : passion == Passion.Minor
                ? 1.0f
                : 0.0f;
    }

    public (Dictionary<T, PassionAndLevel> result, bool exhaustedPoints) Build()
    {
        var minorPassionedSkills = new List<T>();
        var majorPassionedSkills = new List<T>();
        foreach (var kv in passions)
        {
            if (kv.Value == Passion.Major)
                majorPassionedSkills.Add(kv.Key);
            if (kv.Value == Passion.Minor)
                minorPassionedSkills.Add(kv.Key);
        }

        var exhaustedPoints = false;
        var skills = skillRanges
            .OrderByDescending(x => x.Value.min)
            .Select(x => x.Key)
            .ToList();

        foreach (var skill in skills)
        {
            if (forcedPassions.Contains(skill))
            {
                majorPassionedSkills.Remove(skill);
                minorPassionedSkills.Remove(skill);
                passions[skill] = highestPassion;
            }

            exhaustedPoints |= !CanBalanceAroundPassion(skill, Passion.Minor, minorPassionedSkills);
            exhaustedPoints |= !CanBalanceAroundPassion(skill, Passion.Major, majorPassionedSkills);
        }

        var skillsDefs = GetAllSkillDefinitions();
        for (var i = skillsDefs.Count - 1; i >= 0; i--)
        {
            var skillNeedingOtherPassionsClipped = skillsDefs[i];
            var passion = passions[skillNeedingOtherPassionsClipped];
            var range = skillRanges[skillNeedingOtherPassionsClipped];
            ClipMax(skillNeedingOtherPassionsClipped, range.min, passion);
        }

        var levels = new Dictionary<T, PassionAndLevel>();
        foreach (var skill in skills)
        {
            var finalRange = skillRanges[skill];
            var finalPassion = passions[skill];
            levels[skill] = new PassionAndLevel(finalRange.min, finalRange.max, finalPassion);
        }

        return (levels, !exhaustedPoints);
    }

    private bool CanBalanceAroundPassion(T skill, Passion requirement, List<T> skillsWithPassion)
    {
        var thisSkillPassion = passions[skill];
        var thisSkillRange = skillRanges[skill];
        if (skillsWithPassion.Any() && thisSkillPassion < requirement)
        {
            var passionateRange = GetRangeOfSkills(skillsWithPassion);
            if (passionateRange.max <= thisSkillRange.min)
            {
                if (TryLockInPassion(skill, requirement))
                    skillsWithPassion.Add(skill);
                else
                    return false;
            }
            else if (passionateRange.min <= thisSkillRange.max)
            {
                var newMax = Math.Max(thisSkillRange.min, passionateRange.min);
                skillRanges[skill] = new IntRange(thisSkillRange.min, newMax);
                PushMin(skillsWithPassion, newMax);
            }
        }

        return true;
    }

    private IntRange GetRangeOfSkills(IEnumerable<T> skills)
    {
        int min = int.MaxValue, max = int.MaxValue;
        foreach (var skill in skills)
        {
            var range = skillRanges[skill];
            min = Math.Min(range.min, min);
            max = Math.Min(range.max, max);
        }

        return new IntRange(min, max);
    }

    private void PushMin(IEnumerable<T> skills, int value)
    {
        foreach (var skill in skills)
        {
            var range = skillRanges[skill];
            var newMin = Math.Max(range.min, value);
            skillRanges[skill] = new IntRange(newMin, range.max);
        }
    }

    private void ClipMax(T skillToClip, int max, Passion passion)
    {
        var defs = GetAllSkillDefinitions();

        var actualCap = max - 1;
        foreach (var skill in defs)
        {
            if (skill == skillToClip)
            {
                actualCap = max;
                continue;
            }

            var passionOfSkill = passions[skill];
            if (passionOfSkill >= passion)
                continue;

            var skillRange = skillRanges[skill];
            var newMax = Math.Max(skillRange.min, Math.Min(actualCap, skillRange.max));
            skillRanges[skill] = new IntRange(skillRange.min, newMax);
        }
    }
}