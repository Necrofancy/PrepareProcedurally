using Necrofancy.PrepareProcedurally.Rules;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Solving;

/// <summary>
/// Procedural generation for vanilla pawns.
/// </summary>
public static class ProcGen
{
    public static void RandomizeTeam(TeamRebalance teamRebalance)
    {
        var backgrounds = BackstorySolver.TryToSolveWith(teamRebalance);
        var allPassions = teamRebalance.Editor.AllPassions;
        var maxPassionPoints = teamRebalance.Editor.MaxPassionPoints;
        var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, allPassions, maxPassionPoints);
        var fudger = teamRebalance.RuleSet.Fudger;

        using (EditSequence.Apply(fudger.TeamwideGeneratorFudges(teamRebalance.Editor)))
        {
            foreach (var slot in teamRebalance.GetUnlockedSlots())
            {
                if (!finalSkills[slot.Index].HasValue)
                    continue;
                var background = backgrounds[slot.Index];
                var skills = finalSkills[slot.Index].Value;

                var fudging = new Fudging(slot, background, skills);

                fudger.FudgePawnGenerationInTeam(fudging);
            }
        }
    }

    public static void RandomizeCustomPawn(PawnCustomize custom)
    {
        var spawnCategories = custom.Rules.RaceRules.GetBackstoryCategoryFilters();
        var current = BackstorySolver.StaticBackgroundPossibility(custom.Slot.Pawn);
        var specifier = new SelectBackstorySpecifically(spawnCategories, custom.Slot);

        var bio = specifier.GetBestBio(custom.Collector.Weight, custom.Slot.RequiredTraits);
        var skillRanges = EstimateRolling.PossibleSkillRangesOf(bio);

        var background = new BackgroundPossibility(bio, skillRanges, true);
        var maxPassionPoints = custom.Slot.Context.MaxPassionPoints;
        var finalSkills = BackstorySolver.FigureOutPassions(custom, bio, maxPassionPoints);

        var fudger = custom.Rules.Fudger;
        var fudging = new Fudging(custom.Slot, background, finalSkills);
        fudger.FudgeSoloPawnGeneration(fudging);
    }
}
