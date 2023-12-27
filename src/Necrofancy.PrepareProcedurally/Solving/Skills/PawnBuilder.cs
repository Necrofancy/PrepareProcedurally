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
    private readonly BioPossibility bioPossibility;
    private readonly Dictionary<SkillDef, IntRange> skillRanges = new Dictionary<SkillDef, IntRange>();
    private readonly Dictionary<SkillDef, Passion> passions = new Dictionary<SkillDef, Passion>();

    public PawnBuilder(BioPossibility bioPossibility)
    {
        this.bioPossibility = bioPossibility;
        GetInitialSkillRanges();
    }
        
    public float PassionPoints { get; private set; }

    public int MaxOf(SkillDef def) => skillRanges[def].max;

    public bool LockIn(SkillPassionSelection req, int remaining)
    {
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
            exhaustedPoints |= !CanBalanceAroundPassion(skill, Passion.Minor, minorPassionedSkills);
            exhaustedPoints |= !CanBalanceAroundPassion(skill, Passion.Major, majorPassionedSkills);
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
                {
                    skillsWithPassion.Add(skill);
                }
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

    private void GetInitialSkillRanges()
    {
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticRoll(in bioPossibility, 35, skill, 0f);
            var max = EstimateRolling.StaticRoll(in bioPossibility, 35, skill, .98f);
            skillRanges[skill] = new IntRange(min, max);
            passions[skill] = Passion.None;
        }
    }
}