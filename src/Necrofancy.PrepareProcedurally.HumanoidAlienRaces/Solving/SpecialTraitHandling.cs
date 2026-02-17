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

        foreach (var trait in traitsToRemove) pawn.story.traits.RemoveTrait(trait);
    }

    public static void RerollForBackstoryForcedTraits(Pawn pawn)
    {
        void GainTrait(AlienChanceEntry<TraitWithDegree> chanceEntry)
        {
#if RW1_4 || RW1_5
            var trait = new Trait(chanceEntry.defName.def, chanceEntry.defName.degree, true);
#else
            var trait = new Trait(chanceEntry.entry.def, chanceEntry.entry.degree, true);
#endif
            pawn.story.traits.GainTrait(trait);
        }
        
        if (pawn.story.Childhood is AlienBackstoryDef childhood)
            foreach (var trait in childhood.forcedTraitsChance)
                if (trait.Approved(pawn))
                    GainTrait(trait);
        if (pawn.story.Adulthood is AlienBackstoryDef adulthood)
            foreach (var trait in adulthood.forcedTraitsChance)
                if (trait.Approved(pawn))
                    GainTrait(trait);
    }

    public static bool ForcedByBackstory(Trait trait, Pawn pawn)
    {
        if (!trait.ScenForced) return false;
        bool Match(AlienChanceEntry<TraitWithDegree> chanceEntry)
        {
#if RW1_4 || RW1_5
            return chanceEntry.defName.def == trait.def;
#else
            return chanceEntry.entry.def == trait.def;
#endif
        }

        if (pawn.story.Childhood is AlienBackstoryDef childhood)
            if (childhood.forcedTraitsChance.Any(Match))
                return true;

        if (pawn.story.Adulthood is AlienBackstoryDef adulthood)
            if (adulthood.forcedTraitsChance.Any(Match))
                return true;

        return false;
    }
}