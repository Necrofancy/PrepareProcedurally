using System.Reflection;
using HarmonyLib;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.Skills;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

/// <summary>
/// A system for editing definitions and resources to intentionally bias pawn generation one way or another.
///
/// These changes are either changes to factors not within <see cref="PawnWeaponGenerator"/> or required
/// a workaround in some other way to account for mod compatibilities. The <see cref="IDisposable"/> implementation
/// and usage of <see cref="TemporaryEdit{T}"/> means that everything should get changed back after each change.
///
/// While this is not thread-safe, these changes also don't happen when the game and simulation have actually
/// _started_. It's only meant to run while my UI screens are open, and they are only available next to
/// the <see cref="Page_ConfigureStartingPawns"/> window is open. By the time these UIs finish updating on the main
/// thread, all values should be set back to what they were beforehand.
/// </summary>
public static class TemporarilyChange
{
    private static readonly FieldInfo ScenarioPartsField = AccessTools.Field(typeof(Scenario), "parts");
    private static readonly FieldInfo ForcedTraitsTraitField = AccessTools.Field(typeof(ScenPart_ForcedTrait),"trait");
    private static readonly FieldInfo ForcedTraitsDegreeField = AccessTools.Field(typeof(ScenPart_ForcedTrait), "degree");

    public static TemporaryEdit<PawnGenerationRequest> SetSpecificRequest(
        int pawnIndex,
        float age,
        GenderPossibility gender,
        bool allowAddictions,
        bool allowRelationships,
        bool allowPregnant)
    {
        var startingPawnRequests = StartingPawnUtilityState.GetStartingPawnRequestList();

        var currentRequest = startingPawnRequests[pawnIndex];
        var editedRequest = startingPawnRequests[pawnIndex];

        editedRequest.FixedBiologicalAge = age;
        editedRequest.BiologicalAgeRange = null;
        editedRequest.ExcludeBiologicalAgeRange = null;

        editedRequest.AllowAddictions = allowAddictions;
        editedRequest.CanGeneratePawnRelations = allowRelationships;
        editedRequest.AllowPregnant = allowPregnant;

        editedRequest.FixedGender = gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            GenderPossibility.Either => null,
            _ => null
        };

        void SetRequest(PawnGenerationRequest req)
        {
            startingPawnRequests[pawnIndex] = req;
        }

        return new TemporaryEdit<PawnGenerationRequest>(currentRequest, editedRequest, SetRequest);
    }

    public static IDisposable StartingPawnRequest(int pawnIndex, Func<PawnGenerationRequest, PawnGenerationRequest> edit)
    {
        var startingPawnRequests = StartingPawnUtilityState.GetStartingPawnRequestList();

        var oldRequest = startingPawnRequests[pawnIndex];
        var newRequest = edit(oldRequest);

        void SetRequest(PawnGenerationRequest req)
        {
            startingPawnRequests[pawnIndex] = req;
        }

        return new TemporaryEdit<PawnGenerationRequest>(oldRequest, newRequest, SetRequest);
    }

    public static IDisposable AgeOnAllRelevantRaceProperties(Dictionary<ThingDef, RaceAgeData> settings)
    {
        var temporaryEdits = new Stack<TemporaryEdit<SimpleCurve>>();

        foreach (var setting in settings)
        {
            var race = setting.Key.race;
            var ageRange = setting.Value;

            var generationCurve = race.ageGenerationCurve;
            var temporaryCurve = EstimateRolling.SubSampleCurve(generationCurve, ageRange.desiredAgeRange);

            void SetCurve(SimpleCurve curve)
            {
                race.ageGenerationCurve = curve;
            }

            temporaryEdits.Push(new TemporaryEdit<SimpleCurve>(generationCurve, temporaryCurve, SetCurve));
        }

        return TemporaryEdit.Many(temporaryEdits);
    }

    public static TemporaryEdit<FloatRange> PlayerFactionMelaninRange(FloatRange temporaryRange)
    {
        var factionDef = Faction.OfPlayer.def;
        var currentRange = factionDef.melaninRange;

        void SetMelaninRange(FloatRange range)
        {
            factionDef.melaninRange = range;
        }

        return new TemporaryEdit<FloatRange>(currentRange, temporaryRange, SetMelaninRange);
    }

    public static TemporaryEdit<List<TraitDef>> MemberKindDisallowedTraits(List<TraitDef> banned)
    {
        var memberKind = Faction.OfPlayer.def.basicMemberKind;
        var currentList = memberKind.disallowedTraits;
        var newList = currentList != null ? currentList.Concat(banned).ToList() : banned;

        void SetDisallowedTraits(List<TraitDef> traits)
        {
            memberKind.disallowedTraits = traits;
        }

        return new TemporaryEdit<List<TraitDef>>(currentList, newList, SetDisallowedTraits);
    }

    public static IDisposable ForcedTraitsInScenarioParts(IEnumerable<TraitRequirement> traits)
    {
        if (ScenarioPartsField is null || ForcedTraitsTraitField is null || ForcedTraitsDegreeField is null)
        {
            Logging.ErrorOnce("Reflection failed on prerequisites to forcing traits through scenario edits");
            return TemporaryEdit.NullEdit;
        }

        var oldList = (List<ScenPart>)ScenarioPartsField.GetValue(Find.Scenario);
        var newList = oldList.ListFullCopy();

        foreach (var trait in traits)
        {
            var forceTrait = new ScenPart_ForcedTrait();
            ForcedTraitsTraitField.SetValue(forceTrait, trait.def);
            ForcedTraitsDegreeField.SetValue(forceTrait, trait.degree);
            newList.Add(forceTrait);
        }

        void Set(List<ScenPart> parts)
        {
            ScenarioPartsField.SetValue(Find.Scenario, parts);
        }

        return new TemporaryEdit<List<ScenPart>>(oldList, newList, Set);
    }
}
