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
        foreach (var backstory in pawn.story.AllBackstories)
        {
            if (backstory is not AlienBackstoryDef alienBackstory || alienBackstory.forcedTraitsChance.NullOrEmpty())
                continue;

            foreach (var chanceEntry in alienBackstory.forcedTraitsChance)
            {
#if RW1_4
                if (!chanceEntry.Approved(pawn) || chanceEntry.defName?.def is null)
                    continue;
                TryGainForcedTrait(pawn, chanceEntry.defName.def, chanceEntry.defName.degree);
#else
                // for readers: AlienChoiceEntry<T>.Select(Pawn) rolls for the traits here
                // LINQ is -NOT- being used here if you're wondering WTF happened to rolling
                foreach (var traitWithDegree in chanceEntry.Select(pawn))
                    if (traitWithDegree?.def is not null)
                        TryGainForcedTrait(pawn, traitWithDegree.def, traitWithDegree.degree);
#endif
            }
        }
    }

    private static void TryGainForcedTrait(Pawn pawn, TraitDef def, int degree)
    {
        if (!pawn.story.traits.HasTrait(def))
            pawn.story.traits.GainTrait(new Trait(def, degree, true));
    }

    public static bool ForcedByBackstory(Trait trait, Pawn pawn)
    {
        if (!trait.ScenForced) return false;

        foreach (var backstory in pawn.story.AllBackstories)
            if (backstory is AlienBackstoryDef alienBackstory && !alienBackstory.forcedTraitsChance.NullOrEmpty())
                foreach (var chanceEntry in alienBackstory.forcedTraitsChance)
                    if (CouldForce(chanceEntry, trait.def))
                        return true;

        return false;
    }

    private static bool CouldForce(AlienChanceEntry<TraitWithDegree> chanceEntry, TraitDef def)
    {
        if (chanceEntry is null) return false;
#if RW1_4
        return chanceEntry.defName?.def == def;
#else
        if (chanceEntry.entry?.def == def) return true;
        if (chanceEntry.options is null) return false;
        foreach (var option in chanceEntry.options)
            if (CouldForce(option, def))
                return true;
        return false;
#endif
    }
}
