using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

using static PostPawnGenerationChanges;

public static class AlienSpecificPostPawnGenerationChanges
{
    /// <summary>
    /// Fix up possessions, story, and - for humans only - pawn bodytype to match the relevant backstory
    /// of the given <see cref="BioPossibility"/>.
    /// </summary>
    public static void ApplyBackstoryTo(BioPossibility bio, Pawn pawn)
    {
        if (pawn.IsHuman())
        {
            PostPawnGenerationChanges.ApplyBackstoryTo(bio, pawn);
            return;
        }
        
        SpecialTraitHandling.RemoveBackstoryRelatedTraits(pawn);
        
        pawn.story.Childhood = bio.Childhood;
        var possessions = Find.GameInitData.startingPossessions[pawn];
        RemoveBackstoryPossessions(pawn, possessions);
        pawn.story.Adulthood = bio.Adulthood;
        AddBackstoryPossessions(bio, possessions);
        
        SpecialTraitHandling.RerollForBackstoryForcedTraits(pawn);
        
        // Respect a Kickstarter NameTriple or just re-generate the name.
        pawn.Name = bio.Name ?? PawnBioAndNameGenerator.GeneratePawnName(pawn);
        
        // do NOT change the bodytype of a HAR pawn.
        
        pawn.Notify_DisabledWorkTypesChanged();
    }
}