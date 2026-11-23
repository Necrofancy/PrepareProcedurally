using Necrofancy.PrepareProcedurally.Rules;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

public static class HackFactory
{
    private const string SpawnCategoryFilter = "PrepareProcedurally";

    public static Hack AgePropertiesOfRace(RaceProperties race, RaceAgeData curve)
    {
        SimpleCurve Transform(SimpleCurve oldCurve) => EstimateRolling.SubSampleCurve(oldCurve, curve.desiredAgeRange);
        return Hack.FieldTransform<RaceProperties,SimpleCurve>(race, nameof(RaceProperties.ageGenerationCurve), Transform);
    }

    public static IEnumerable<Hack> AgePropertiesForEachRace(IReadOnlyDictionary<ThingDef, RaceAgeData> data)
    {
        foreach (var (thingDef, ageData) in data)
        {
            yield return AgePropertiesOfRace(thingDef.race, ageData);
        }
    }

    public static Hack SelectedMelaninRange(FloatRange range)
    {
        return Hack.FieldWithValue(Faction.OfPlayer.def, nameof(FactionDef.melaninRange), range);
    }

    public static IEnumerable<Hack> ForAdjustingPawnBackstory(Fudging fudged, PawnPossibility possible)
    {
        var slot = fudged.Slot;
        var list = StartingPawnUtilityState.GetStartingPawnRequestList(fudged.Slot.Context.Slots.Count);
        if (slot.Index < 0 || slot.Index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(slot));

        var request = list[slot.Index];
        var kindDef = slot.PinnedKind ?? request.KindDef;

        yield return AdjustPawnKindBackstoryOverrides(kindDef);
        yield return BanConflictingTraits(fudged, kindDef);

        PawnGenerationRequest Get() => list[slot.Index];
        void Set(PawnGenerationRequest toSet) => list[slot.Index] = toSet;
        PawnGenerationRequest Transform(PawnGenerationRequest request)
        {
            var possibility = slot.Gender;

            if (fudged.SolidBio is not null)
            {
                possibility = fudged.SolidBio.gender;
            }

            return request with
            {
                AllowPregnant = slot.Context.AllowPregnancy,
                AllowAddictions = slot.Context.AllowInjuries,
                CanGeneratePawnRelations = slot.Context.AllowRelationships,
                AllowedDevelopmentalStages = DevelopmentalStage.Adult,

                FixedGender = possibility switch
                {
                    GenderPossibility.Male => Gender.Male,
                    GenderPossibility.Female => Gender.Female,
                    _ => null,
                },

                KindDef = kindDef,
            };
        }

        yield return Hack.Function(Get, Transform, Set, $"Starting pawn request {slot.Index}");
    }

    public static Hack ForceRequiredTraits(Fudging fudge, BackstoryDef childhood)
    {
        // forced backstory in PawnGenerationRequest.ForcedTraits means I have to question
        // whether the trait had the forced flag was a Scenario part or a mod. Pass.
        var required = fudge.Slot.RequiredTraits
            .Select(t => new BackstoryTrait { def = t.def, degree = t.degree ?? 0 })
            .ToList();

        List<BackstoryTrait> Transform(List<BackstoryTrait> current) =>
            current is null ? required : [.. current, .. required];

        return Hack.FieldTransform<BackstoryDef, List<BackstoryTrait>>(childhood, nameof(BackstoryDef.forcedTraits), Transform);
    }

    private static Hack BanConflictingTraits(Fudging context, PawnKindDef kind)
    {
        var banned = DefDatabase<TraitDef>.AllDefsListForReading
            .Where(x => x.conflictingPassions is not null)
            .Where(trait => trait.conflictingPassions
                .Any(x => context.Skills.FinalRanges[x].Passion > Passion.None))
            .ToList();

        List<TraitDef> Transform(List<TraitDef> current) => current is null ? banned : [.. current, .. banned];

        return Hack.FieldTransform<PawnKindDef,List<TraitDef>>(kind, nameof(PawnKindDef.disallowedTraits), Transform);
    }

    public static Hack AdjustPawnKindBackstoryOverrides(PawnKindDef def)
    {
        // force only a list of backstories set by the BackstoryDef hack below
        var list = new List<BackstoryCategoryFilter> { new() { categories = SpawnFilter() } };
        return Hack.FieldWithValue(def, nameof(PawnKindDef.backstoryFiltersOverride), list);
    }

    public static Hack AdjustBackstorySpawnCategory(BackstoryDef def)
    {
        // point desired backstories to match the PawnKindDef hack above
        // side-effect of preventing world-pawn relationships from using this backstory
        return Hack.FieldWithValue(def, nameof(BackstoryDef.spawnCategories), SpawnFilter());
    }

    public static Hack MakeBackstoryShuffleable(BackstoryDef def)
    {
        // make solid bios temporarily shuffleable so that even if a roll for shuffled backstories happens,
        // it will be forced to pick the appropriate backstory.
        return Hack.FieldWithValue(def, nameof(BackstoryDef.shuffleable), true);
    }

    public static IEnumerable<Hack> SetBackstory(Pawn pawn, PawnBio bio)
    {
        Gender gender = bio.gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            _ => pawn.gender
        };

        Name name = new NameTriple(bio.name.First, bio.name.Nick, bio.name.Last);

        yield return Hack.FieldWithValue(pawn, nameof(pawn.gender), gender);
        yield return Hack.PropertySet(pawn, nameof(pawn.Name), name);
    }

    private static List<string> SpawnFilter() => [SpawnCategoryFilter];
}
