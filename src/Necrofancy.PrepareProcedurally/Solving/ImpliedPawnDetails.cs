namespace Necrofancy.PrepareProcedurally.Solving;

public record ImpliedPawnDetails(
    PawnKindDef KindDef,
    IReadOnlyCollection<TraitRequirement> RequestedTraits,
    IReadOnlyCollection<BackstoryTrait> BackstoryTraits,
    IReadOnlyCollection<BackstoryTrait> DisallowedTraits,
    GenderPossibility GenderRequirement,
    WorkTags RequiredWork,
    WorkTags DisabledWork,
    int MaxTraits)
{
    public ImpliedPawnDetails With(BackstoryDef backstory)
    {
        var (required, disabled) = backstory.WorkTagsInPractice();
        return this with
        {
            BackstoryTraits = [.. BackstoryTraits, .. backstory.forcedTraits ?? Enumerable.Empty<BackstoryTrait>()],
            DisallowedTraits = [.. DisallowedTraits, .. backstory.disallowedTraits ?? Enumerable.Empty<BackstoryTrait>()],
            RequiredWork = RequiredWork | required,
            DisabledWork = DisabledWork | disabled,
        };
    }

    public bool Allow(BackstoryDef possibleBackstory)
    {
        var (backstoryRequires, backstoryDisables) = possibleBackstory.WorkTagsInPractice();
        if (!WorkAllows(backstoryRequires, backstoryDisables))
            return false;

        if (possibleBackstory.disallowedTraits is {} disallowedTraits)
        {
            foreach (var disallowedTrait in disallowedTraits)
            {
                foreach (var requestedTrait in RequestedTraits)
                    if (Match(requestedTrait, disallowedTrait))
                        return false;
                foreach (var backstoryTrait in BackstoryTraits)
                    if (Match(backstoryTrait, disallowedTrait))
                        return false;
            }
        }

        if (possibleBackstory.forcedTraits is { } forcedTraits)
        {
            foreach (var forcedTrait in forcedTraits)
            {
                foreach (var disallowedTrait in DisallowedTraits)
                    if (Match(forcedTrait, disallowedTrait))
                        return false;
            }
        }

        foreach (var forced in possibleBackstory.forcedTraits ?? Enumerable.Empty<BackstoryTrait>())
        foreach (var requested in RequestedTraits)
            if (!Match(requested, forced) && (requested.def == forced.def || requested.def.ConflictsWith(forced.def)))
                return false;

        if (With(possibleBackstory).GetEffectiveTraitRequirements().Count(x => !x.def.IsSexualityTrait()) > MaxTraits)
            return false;

        return true;
    }

    public bool Allow(PawnBio possibleBio)
    {
        if (GenderRequirement != GenderPossibility.Either
            && possibleBio.gender != GenderRequirement
            && GenderRequirement != possibleBio.gender)
            return false;

        if (!Allow(possibleBio.childhood))
            return false;

        if (!With(possibleBio.childhood).Allow(possibleBio.adulthood))
            return false;

        return true;
    }

    public bool Allow(TraitRequirement possibleReq)
    {
        if (!WorkAllows(possibleReq.def.requiredWorkTags, possibleReq.def.disabledWorkTags))
            return false;

        foreach (var existingReq in RequestedTraits)
            if (existingReq.def.ConflictsWith(possibleReq.def) || existingReq.def == possibleReq.def)
                return false;

        // e.g. adding a Jogger trait requirement to a slot with a Tribal Scout background is fine.
        foreach (var backstoryTrait in BackstoryTraits)
            if (!Match(possibleReq, backstoryTrait) && backstoryTrait.def.ConflictsWith(possibleReq.def))
                return false;

        foreach (var disallowed in DisallowedTraits)
            if (Match(possibleReq, disallowed))
                return false;

        return true;
    }

    public IReadOnlyList<TraitRequirement> GetEffectiveTraitRequirements()
    {
        var list = RequestedTraits.ToList();
        foreach (var trait in BackstoryTraits)
        {
            if (!list.Any(req => Match(req, trait)))
                list.Add(new TraitRequirement { def = trait.def, degree = trait.degree });
        }

        return list;
    }

    private bool WorkAllows(WorkTags requires, WorkTags disabled)
    {
        return !RequiredWork.OverlapsWithOnAnyWorkType(disabled) && !requires.OverlapsWithOnAnyWorkType(DisabledWork);
    }

    private static bool Match(TraitRequirement requirement, BackstoryTrait backstoryTrait)
    {
        return requirement.def == backstoryTrait.def && (requirement.degree ?? 0) == backstoryTrait.degree;
    }

    private static bool Match(BackstoryTrait a, BackstoryTrait b)
    {
        return a.def == b.def && a.degree == b.degree;
    }
}
