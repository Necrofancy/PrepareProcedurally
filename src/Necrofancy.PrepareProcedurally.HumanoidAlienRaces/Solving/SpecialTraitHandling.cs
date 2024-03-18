using System.Collections.Generic;
using AlienRace;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

public static class SpecialTraitHandling
{
    public static void RemoveBackstoryRelatedTraits(Pawn pawn)
    {
        List<Trait> traitsToRemove = new();
        foreach (var trait in pawn.story.traits.allTraits)
            if (ForcedByBackstory(trait, pawn))
                traitsToRemove.Add(trait);

        foreach (var trait in traitsToRemove) 
            pawn.story.traits.RemoveTrait(trait);
    }

    public static void RerollForBackstoryForcedTraits(Pawn pawn)
    {
        if (pawn.story.Childhood is AlienBackstoryDef childhood)
            foreach (var trait in childhood.forcedTraitsChance)
                if (trait.Approved(pawn))
                    pawn.story.traits.GainTrait(new Trait(trait.defName.def, trait.degree, true));

        if (pawn.story.Adulthood is AlienBackstoryDef adulthood)
            foreach (var trait in adulthood.forcedTraitsChance)
                if (trait.Approved(pawn))
                    pawn.story.traits.GainTrait(new Trait(trait.defName.def, trait.degree, true));
    }

    public static bool ForcedByBackstory(Trait trait, Pawn pawn)
    {
        if (!trait.ScenForced) return false;

        if (pawn.story.Childhood is AlienBackstoryDef childhood)
            if (childhood.forcedTraitsChance.Any(x => x.defName.def == trait.def))
                return true;

        if (pawn.story.Adulthood is AlienBackstoryDef adulthood)
            if (adulthood.forcedTraitsChance.Any(x => x.defName.def == trait.def))
                return true;

        return false;
    }
}