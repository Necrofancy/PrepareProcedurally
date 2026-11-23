using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Rules;

public class VanillaChoices(
    IReadOnlyList<BackstoryDef> allBackstories,
    IReadOnlyList<TraitDef> allTraits,
    IReadOnlyList<PawnBio> allBios,
    Func<IReadOnlyList<BackstoryCategoryFilter>> getBackstoryFilters) : IPawnChoices
{
    public IEnumerable<TraitRequirement> PossibleTraitsFor(PawnSlot slot)
    {
        var implications = GetImplications(slot);
        var neededTraits = implications.GetEffectiveTraitRequirements();

        var nonSexualityCount = neededTraits.Count(x => !x.def.IsSexualityTrait());
        foreach (var possibleTrait in allTraits)
        {
            if ((possibleTrait.IsSexualityTrait() || nonSexualityCount < Vanilla.MaxNonSexualityTraits)
                && !IsCreepTrait(possibleTrait))
            {
                foreach (var data in possibleTrait.degreeDatas)
                {
                    var requirement = new TraitRequirement { def = possibleTrait, degree = data.degree };
                    if (implications.Allow(requirement))
                        yield return requirement;
                }
            }
        }
    }

    public IEnumerable<BackstoryDef> PossibleChildhoodsFor(PawnSlot slot) => Stories(slot, BackstorySlot.Childhood);

    public IEnumerable<BackstoryDef> PossibleAdulthoodsFor(PawnSlot slot) => Stories(slot, BackstorySlot.Adulthood);

    private IEnumerable<BackstoryDef> Stories(PawnSlot slot, BackstorySlot ignoreCurrentForcedSlot)
    {
        var includeChildhood = ignoreCurrentForcedSlot != BackstorySlot.Childhood;
        var includeAdulthood = ignoreCurrentForcedSlot != BackstorySlot.Adulthood;
        var implications = GetImplications(slot, includeChildhood, includeAdulthood);
        var filters = getBackstoryFilters();
        foreach (var backstoryDef in allBackstories)
        {
            if (backstoryDef.slot != ignoreCurrentForcedSlot)
                continue;
            if (!backstoryDef.shuffleable)
                continue;
            if (!filters.Any(filter => filter.Matches(backstoryDef)))
                continue;
            if (implications.Allow(backstoryDef))
                yield return backstoryDef;
        }
    }

    public IEnumerable<PawnBio> PossibleSolidBiosFor(PawnSlot slot)
    {
        var implications = GetImplications(slot, childhood:false, adulthood:false);
        var filters = getBackstoryFilters();

        foreach (var solidBio in allBios)
        {
            if (filters.Any(filter => filter.Matches(solidBio)) && implications.Allow(solidBio))
                yield return solidBio;
        }
    }

    public IEnumerable<BackstoryPossibility> GetAllPossibilities(PawnSlot slot)
    {
        if (slot.SolidBio is not null)
        {
            yield return new BackstoryPossibility(slot.SolidBio, ThingDefOf.Human);
            yield break;
        }

        var shuffleableChildhoods = slot.ForcedChildhood is null
            ? PossibleChildhoodsFor(slot)
            : [slot.ForcedChildhood];

        var shuffleableAdulthoods = slot.ForcedAdulthood is null
            ? PossibleAdulthoodsFor(slot).ToList()
            : [slot.ForcedAdulthood];

        var possibleSolidBio = slot.ForcedChildhood is null && slot.ForcedAdulthood is null
            ? PossibleSolidBiosFor(slot)
            : Enumerable.Empty<PawnBio>();

        var implications = GetImplications(slot, childhood: false, adulthood: false);
        foreach (var childhood in shuffleableChildhoods)
        {
            var selectedChildhoodImplications = implications.With(childhood);
            foreach (var adulthood in shuffleableAdulthoods.Where(selectedChildhoodImplications.Allow))
                yield return new BackstoryPossibility(childhood, adulthood, ThingDefOf.Human);
        }

        foreach (var solidBio in possibleSolidBio)
            yield return new BackstoryPossibility(solidBio, ThingDefOf.Human);
    }

    private static bool IsCreepTrait(TraitDef def) => def.GetGenderSpecificCommonality(Gender.None) == 0;

    private static ImpliedPawnDetails GetImplications(PawnSlot slot, bool childhood = true, bool adulthood = true)
    {
        var required = WorkTags.None;
        var disabled = WorkTags.None;
        List<BackstoryTrait> backstoryTraits = [];
        List<BackstoryTrait> disallowedTraits = [];

        if (slot.ForcedChildhood is not null && childhood)
        {
            required |= slot.ForcedChildhood.requiredWorkTags;
            disabled |= slot.ForcedChildhood.workDisables;
            foreach (var trait in slot.ForcedChildhood.forcedTraits ?? Enumerable.Empty<BackstoryTrait>())
                backstoryTraits.Add(trait);
            foreach (var trait in slot.ForcedChildhood.disallowedTraits ?? Enumerable.Empty<BackstoryTrait>())
                disallowedTraits.Add(trait);
        }

        if (slot.ForcedAdulthood is not null && adulthood)
        {
            required |= slot.ForcedAdulthood.requiredWorkTags;
            disabled |= slot.ForcedAdulthood.workDisables;
            foreach (var trait in slot.ForcedAdulthood.forcedTraits ?? Enumerable.Empty<BackstoryTrait>())
                backstoryTraits.Add(trait);
            foreach (var trait in slot.ForcedAdulthood.disallowedTraits ?? Enumerable.Empty<BackstoryTrait>())
                disallowedTraits.Add(trait);
        }

        foreach (var backstoryTrait in backstoryTraits)
        {
            required |= backstoryTrait.def.requiredWorkTags;
            disabled |= backstoryTrait.def.disabledWorkTags;
        }

        foreach (var traitRequirement in slot.RequiredTraits)
        {
            required |= traitRequirement.def.requiredWorkTags;
            disabled |= traitRequirement.def.disabledWorkTags;
        }

        return new ImpliedPawnDetails(
            KindDef: Faction.OfPlayer.def.basicMemberKind,
            RequestedTraits: slot.RequiredTraits,
            BackstoryTraits: backstoryTraits,
            DisallowedTraits: disallowedTraits,
            GenderRequirement: slot.Gender,
            RequiredWork: required,
            DisabledWork: disabled,
            MaxTraits: Vanilla.MaxNonSexualityTraits);
    }
}


