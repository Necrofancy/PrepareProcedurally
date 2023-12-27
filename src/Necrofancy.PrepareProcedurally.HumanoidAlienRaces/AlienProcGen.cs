using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

/// <summary>
/// Procedural generation rules specific to Humanoid Alien Races.
/// </summary>
public static class AlienProcGen
{
    public static Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector,
        List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);

        var addBackToLocked = false;
        if (Editor.LockedPawns.Contains(pawn))
        {
            Editor.LockedPawns.Remove(pawn);
            addBackToLocked = true;
        }

        string pawnChildhoods = null, pawnAdulthoods = null;
        using (TemporarilyChange.PlayerFactionMelaninRange(Editor.MelaninRange))
        using (TemporarilyChange.AgeOnAllRelevantRaceProperties(Editor.RaceAgeRanges))
        {
            bool foundBackstoryToWorkWith = false;
            while (!foundBackstoryToWorkWith)
            {
                pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                ProcGen.OnPawnChanged(pawn);

                if (pawn.story.Childhood.spawnCategories.Any() &&
                    pawn.story.Adulthood?.spawnCategories.Any() == true)
                {
                    pawnChildhoods = pawn.story.Childhood.spawnCategories.First();
                    pawnAdulthoods = pawn.story.Adulthood.spawnCategories.First();
                    foundBackstoryToWorkWith = true;
                }
            }
        }

        var specifier = new SelectBackstorySpecifically(new List<string> { pawnChildhoods, pawnAdulthoods });
        var bio = specifier.GetBestBio(collector.Weight, Editor.TraitRequirements[index]);
        var traits = bio.Traits;

        var builder = new PawnBuilder(bio);
        foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
        {
            if (usability == UsabilityRequirement.Major)
                builder.TryLockInPassion(skill, Passion.Major);
            else if (usability == UsabilityRequirement.Minor)
                builder.TryLockInPassion(skill, Passion.Minor);
        }

        bio.ApplyBackstoryTo(pawn);
        builder.Build().ApplySimulatedSkillsTo(pawn);
        traits.ApplyRequestedTraitsTo(pawn);

        if (addBackToLocked)
        {
            Editor.LockedPawns.Add(pawn);
        }

        return pawn;
    }

    public static void RandomizeTeam(BalancingSituation situation)
    {
        // it's actually impossible to try balancing up-front because I don't know what backstories are available
        // so at each step let's try grabbing some passions and go through each unlocked pawn.

        var variation = new IntRange(10, (int)(Editor.SkillWeightVariation * 10));
        var backgrounds = BackstorySolver.TryToSolveWith(situation, variation);
        var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);
        Editor.LastResults = finalSkills;

        for (int pawnIndex = 0; pawnIndex < Editor.StartingPawns.Count; pawnIndex++)
        {
            var pawn = Editor.StartingPawns[pawnIndex];
            if (Editor.LockedPawns.Contains(pawn) || !Editor.LastResults[pawnIndex].HasValue)
            {
                continue;
            }

            var reqs = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
            foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                var result = Editor.LastResults[pawnIndex].Value.FinalRanges[skill];
                var usability = result.Passion switch
                {
                    Passion.Major => UsabilityRequirement.Major,
                    Passion.Minor => UsabilityRequirement.Minor,
                    _ => UsabilityRequirement.CanBeOff
                };
                reqs.Add((skill, usability));
            }

            var dict = new Dictionary<SkillDef, int>();
            var requiredSkills = new List<SkillDef>();
            var requiredWorkTags = WorkTags.None;

            foreach (var (skill, usability) in reqs)
            {
                switch (usability)
                {
                    case UsabilityRequirement.Major:
                        dict[skill] = 20 * variation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    case UsabilityRequirement.Minor:
                        dict[skill] = 10 * variation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    case UsabilityRequirement.Usable:
                        dict[skill] = 5 * variation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    default:
                        dict[skill] = -5 * variation.RandomInRange;
                        break;
                }
            }

            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (requiredSkills.Any(requiredSkills.Contains))
                {
                    requiredWorkTags |= workType.workTags;
                }
            }

            var collectSpecificPassions = new CollectSpecificPassions(dict, requiredWorkTags);
            Editor.StartingPawns[pawnIndex] = RandomizeSingularPawn(pawn, collectSpecificPassions, reqs);
        }
    }
}