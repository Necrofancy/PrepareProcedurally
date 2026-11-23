using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

using static PostPawnGenerationChanges;

public static class AlienSpecificPostPawnGenerationChanges
{
    /// <summary>
    /// Fix up possessions, story, and - for humans only - pawn bodytype to match the relevant backstory
    /// of the given <see cref="BackstoryPossibility"/>.
    /// </summary>
    public static void ApplyBackstoryTo(BackstoryPossibility backstory, Pawn pawn)
    {
        if (pawn.IsHuman())
        {
            PostPawnGenerationChanges.ApplyBackstoryTo(backstory, pawn);
            return;
        }

        SpecialTraitHandling.RemoveBackstoryRelatedTraits(pawn);

        pawn.story.Childhood = backstory.Childhood;
        var possessions = Find.GameInitData.startingPossessions[pawn];
        RemoveBackstoryPossessions(pawn, possessions);
        pawn.story.Adulthood = backstory.Adulthood;
        AddBackstoryPossessions(backstory, possessions);

        SpecialTraitHandling.RerollForBackstoryForcedTraits(pawn);

        // Respect a Kickstarter NameTriple or just re-generate the name.
        pawn.Name = backstory.Name ?? PawnBioAndNameGenerator.GeneratePawnName(pawn);

        // do NOT change the bodytype of a HAR pawn.

        pawn.Notify_DisabledWorkTypesChanged();
    }
}
