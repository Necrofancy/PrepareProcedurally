using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

internal static class TraitUtilities
{
    /// <summary>
    /// If too many traits are requested, then pawn generation will randomly include a fourth or fifth trait.
    /// "Requested" traits are not similar to _forced_ traits - the goal is to simulate rolling them specifically.
    ///
    /// To accomplish that, remove any randomly-rolled traits until the pawn is back to their max number of traits.
    /// </summary>
    internal static void FixTraitOverflow(PawnSlot slot, RuleSet rules)
    {
        var neededTraits = RequiredTraitsForLockedPawn(slot);
        var traitSlots = 0;
        var candidates = new List<Trait>();
        foreach (var trait in slot.Pawn.story.traits.allTraits)
        {
            // Biotech-forced traits won't count towards the limit.
            // Post-1.4, sexuality traits are rolled separately from the 1~3 trait count
            // Scenario-forced traits are weird, and having enough of them will absolutely put you over 3 traits.

            // So best to ignore all of these when seeing what traits 'need' to be cut for normal pawn generation.
            if (trait.sourceGene != null || trait.def.IsSexualityTrait() || trait.ScenForced)
            {
                continue;
            }

            traitSlots++;

            if (neededTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
            {
                continue;
            }

            candidates.Add(trait);
        }

        while (traitSlots > rules.RaceRules.GetMaximumGeneratedTraits(slot.Pawn.def))
        {
            var traitToRemove = candidates.RandomElement();
            slot.Pawn.story.traits.RemoveTrait(traitToRemove);
            candidates.Remove(traitToRemove);
            traitSlots--;
        }
    }

    public static bool IsBackstoryTraitOfPawn(Trait trait, Pawn pawn)
    {
        bool isChildHoodBackstory = pawn.story.Adulthood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true;
        bool isAdulthoodBackstory = pawn.story.Childhood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true;
        return isChildHoodBackstory || isAdulthoodBackstory;
    }

    private static List<TraitRequirement> RequiredTraitsForLockedPawn(PawnSlot slot)
    {
        var requiredTraits = new List<TraitRequirement>();

        foreach (var trait in slot.Pawn.story.traits.allTraits)
        {
            if (IsBackstoryTraitOfPawn(trait, slot.Pawn))
            {
                requiredTraits.Add(new TraitRequirement { def = trait.def, degree = trait.Degree });
            }
        }

        if (slot.RequiredTraits.Any())
            requiredTraits.AddRange(slot.RequiredTraits);

        return requiredTraits;
    }

    private static bool BackstoryMatch(Trait traitInstance, BackstoryTrait backstoryTrait)
    {
        return backstoryTrait.def == traitInstance.def && backstoryTrait.degree == traitInstance.Degree;
    }
}
