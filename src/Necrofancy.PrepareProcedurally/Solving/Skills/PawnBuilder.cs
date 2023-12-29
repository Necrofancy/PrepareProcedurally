using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public class PawnBuilder
{
    private readonly Dictionary<SkillDef, IntRange> skillRanges = new();
    private readonly Dictionary<SkillDef, IntRange> targetRanges = new();
    private readonly Dictionary<SkillDef, Passion> passions = new();

    private readonly HashSet<SkillDef> forcedPassions = new();
    private readonly HashSet<SkillDef> disallowedPassions = new();
    private Passion highestPassion = Passion.Minor;

    public PawnBuilder(BioPossibility bio, float age)
    {
        disallowedPassions.AddRange(SkillDisables.GetSkillsDisabled(bio.Childhood, bio.Adulthood));

        GetInitialSkillRanges(bio, age);
    }

    public PawnBuilder(Pawn pawn)
    {
        disallowedPassions.AddRange(SkillDisables.GetSkillsDisabled(pawn.story.Childhood, pawn.story.Adulthood));
        foreach (var trait in pawn.story.traits.allTraits)
        {
            if (trait.def.forcedPassions is { } forced)
                forcedPassions.AddRange(forced);
            if (trait.def.conflictingPassions is { } conflicting)
                disallowedPassions.AddRange(conflicting);
        }

        GetInitialSkillRanges(pawn);
    }

    public float PassionPoints { get; private set; }

    public int MaxOf(SkillDef def)
    {
        return skillRanges[def].max;
    }

    public bool LockIn(SkillPassionSelection req, int remaining)
    {
        if (disallowedPassions.Contains(req.Skill))
            return false;

        var used = req.Total - remaining;
        var currentPassionNeeded =
            used < req.major
                ? Passion.Major
                : used < req.major + req.minor
                    ? Passion.Minor
                    : Passion.None;

        var def = req.Skill;
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

    public bool TryLockInPassion(SkillDef def, Passion passion)
    {
        if (disallowedPassions.Contains(def))
            return false;

        var currentPassion = passions[def];
        if (currentPassion >= passion)
            return true;

        var pointDiff = PointsFor(passion) - PointsFor(currentPassion);
        if (pointDiff >= Editor.MaxPassionPoints - PassionPoints)
            return false;

        var skillRange = skillRanges[def];
        var newMin = NewMin(skillRange, passion);
        skillRanges[def] = new IntRange(newMin, skillRange.max);

        passions[def] = passion;
        PassionPoints += pointDiff;
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

    public SkillFinalizationResult Build()
    {
        var minorPassionedSkills = new List<SkillDef>();
        var majorPassionedSkills = new List<SkillDef>();
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

        List<SkillDef> skillsDefs = DefDatabase<SkillDef>.AllDefsListForReading;
        for (int i = skillsDefs.Count - 1; i >= 0; i--)
        {
            var skillNeedingOtherPassionsClipped = skillsDefs[i];
            var passion = passions[skillNeedingOtherPassionsClipped];
            var range = skillRanges[skillNeedingOtherPassionsClipped];
            ClipMax(skillNeedingOtherPassionsClipped, range.min, passion);
        }
        
        var levels = new Dictionary<SkillDef, PassionAndLevel>();
        foreach (var skill in skills)
        {
            var finalRange = skillRanges[skill];
            var finalPassion = passions[skill];
            levels[skill] = new PassionAndLevel(finalRange.min, finalRange.max, finalPassion);
        }

        return new SkillFinalizationResult(levels, !exhaustedPoints);
    }

    private bool CanBalanceAroundPassion(SkillDef skill, Passion requirement, List<SkillDef> skillsWithPassion)
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

    private IntRange GetRangeOfSkills(IEnumerable<SkillDef> skills)
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

    private void PushMin(IEnumerable<SkillDef> skills, int value)
    {
        foreach (var skill in skills)
        {
            var range = skillRanges[skill];
            var newMin = Math.Max(range.min, value);
            skillRanges[skill] = new IntRange(newMin, range.max);
        }
    }

    private void ClipMax(SkillDef skillToClip, int max, Passion passion)
    {
        List<SkillDef> defs = DefDatabase<SkillDef>.AllDefsListForReading;
        
        var actualCap = max-1;
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

    private void GetInitialSkillRanges(BioPossibility bio, float age)
    {
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticRoll(in bio, age, skill, 0f);
            var max = EstimateRolling.StaticRoll(in bio, age, skill, .98f);
            skillRanges[skill] = new IntRange(min, max);
            targetRanges[skill] = new IntRange(min, max);
            passions[skill] = Passion.None;
        }
    }

    private void GetInitialSkillRanges(Pawn pawn)
    {
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticRoll(pawn, skill, 0f);
            var max = EstimateRolling.StaticRoll(pawn, skill, .98f);
            skillRanges[skill] = new IntRange(min, max);
            targetRanges[skill] = new IntRange(min, max);
            passions[skill] = Passion.None;
        }
    }
}