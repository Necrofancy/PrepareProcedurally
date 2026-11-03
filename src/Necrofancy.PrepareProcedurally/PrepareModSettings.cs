using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class PrepareModSettings : ModSettings
{
    // the actual possible range of human ages includes newborns, teenagers, and geriatrics
    // we need adulthoods for pawn generation, and most users probably don't want to play around dementia/frail.
    private static readonly IntRange PossibleAdultHumanAgeRange = new(21, 120);

    private static readonly IntRange DefaultedSelectedAdultHumanRange = new(21, 50);

    // otherwise the default settings should match vanilla generation
    private static readonly FloatRange DefaultMelaninRange = new(0f, 1f);
    private const bool DefaultAllowInjuries = true;
    private const bool DefaultAllowRelationships = true;

    private const bool DefaultAllowPregnancy = true;

    // starting educated guess values that users will hopefully never have to change
    private const float DefaultSkillWeightVariation = 1.5f;
    private const int DefaultCandidatePoolSize = 50;

    private const float DefaultMinimumRatioOfBest = 0.6f;

    // have the upper bound be a decently passionate pawn (top 2% of total passions)
    private const float DefaultMaxPassionPoints = 7.0f;

    // starting with good relationships being likely is both very strong and very overpowered
    private const bool DefaultShowCompatibility = false;

    // pre-generating can make it super simple to start off fast but prevents someone from randomizing a pawn
    // and then locking in everything else to build around them.
    private const bool DefaultPreGenerate = false;

    // debouncing settings as a mercy for HAR players; vanilla generation is fast enough to not need debouncing
    private const bool DefaultAutoGenerate = true;
    private const int DefaultDebounceDelay = 250;

    public string selectedRace;
    public Dictionary<string, RaceAgeData> ageRangeByRace;
    public FloatRange defaultMelaninRange = DefaultMelaninRange;
    public bool allowInjuries = DefaultAllowInjuries;
    public bool allowRelationships = DefaultAllowRelationships;
    public bool allowPregnancy = DefaultAllowPregnancy;
    public float skillWeightVariation = DefaultSkillWeightVariation;
    public int candidatePoolSize = DefaultCandidatePoolSize;
    public float minimumRatioOfBest = DefaultMinimumRatioOfBest;

    public float maxPassionPoints = DefaultMaxPassionPoints;
    public bool showCompatibility = DefaultShowCompatibility;
    public bool preGenerate = DefaultPreGenerate;
    public bool autoGenerate = DefaultAutoGenerate;
    public int debounceDelay = DefaultDebounceDelay;

    public bool AlienRacesEnabled => ModsConfig.IsActive("erdelf.humanoidalienraces");

    public override void ExposeData()
    {
        Scribe_Values.Look(ref selectedRace, "selectedRace");
        Scribe_Collections.Look(ref ageRangeByRace, "ageRangeByRace", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref defaultMelaninRange, "melaninRange", DefaultMelaninRange);
        Scribe_Values.Look(ref allowInjuries, "allowInjuries", DefaultAllowInjuries);
        Scribe_Values.Look(ref allowRelationships, "allowRelationships", DefaultAllowRelationships);
        Scribe_Values.Look(ref allowPregnancy, "allowPregnancy", DefaultAllowPregnancy);
        Scribe_Values.Look(ref skillWeightVariation, "skillWeightVariation", DefaultSkillWeightVariation);
        Scribe_Values.Look(ref candidatePoolSize, "candidatePoolSize", DefaultCandidatePoolSize);
        Scribe_Values.Look(ref minimumRatioOfBest, "minimumRatioOfBest", DefaultMinimumRatioOfBest);
        Scribe_Values.Look(ref maxPassionPoints, "defaultPassionPoints", DefaultMaxPassionPoints);
        Scribe_Values.Look(ref showCompatibility, "showCompatibility", DefaultShowCompatibility);
        Scribe_Values.Look(ref preGenerate, "generateOnOpen", DefaultPreGenerate);
        Scribe_Values.Look(ref autoGenerate, "autoGenerate", DefaultAutoGenerate);
        Scribe_Values.Look(ref debounceDelay, "debounceDelay", DefaultDebounceDelay);
    }

    public (ThingDef humanTarget, Dictionary<ThingDef, RaceAgeData> ageRangesByHumanType) GetLoadedHumanTypes()
    {
        EnsureInitialized();

        ThingDef humanTarget = null;
        Dictionary<ThingDef, RaceAgeData> ageRangesByHumanType = new();

        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.defName.Equals(selectedRace))
            {
                humanTarget = def;
            }

            if (ageRangeByRace.TryGetValue(def.defName, out var ageRangeForHarType))
            {
                ageRangesByHumanType.Add(def, ageRangeForHarType);
            }
        }

        return (humanTarget ?? ThingDefOf.Human, ageRangesByHumanType);
    }

    public bool UpdateAgeData(ThingDef currentlySelectedRace, Dictionary<ThingDef, RaceAgeData> data)
    {
        bool dirty = false;
        EnsureInitialized();
        selectedRace = currentlySelectedRace.defName;
        foreach (var (race, ageData) in data)
        {
            if (ageRangeByRace.TryGetValue(race.defName, out var existingData)
                && existingData.Equals(ageData))
            {
                continue;
            }

            dirty = true;
            ageRangeByRace[race.defName] = ageData;
        }

        return dirty;
    }

    private void EnsureInitialized()
    {
        if (!string.IsNullOrEmpty(selectedRace) && ageRangeByRace is not null)
        {
            return;
        }

        // def database is not guaranteed to be loaded when mod loads.
        selectedRace = ThingDefOf.Human.defName;
        ageRangeByRace = new Dictionary<string, RaceAgeData>
        {
            // this is based on the default age curve for the starting rimworld colony
            { selectedRace, new RaceAgeData(DefaultedSelectedAdultHumanRange, PossibleAdultHumanAgeRange) }
        };
    }

    public RaceAgeData GetOrCreateRaceAgeSetting(
        ThingDef otherKindRace,
        int minimumAdulthoodAge,
        int maximumAdulthoodAge)
    {
        EnsureInitialized();
        if (!ageRangeByRace.TryGetValue(otherKindRace.defName, out var raceAgeData))
        {
            var range = (maximumAdulthoodAge - minimumAdulthoodAge) / 5;
            var defaultSettings = new IntRange(minimumAdulthoodAge, minimumAdulthoodAge + range);
            var maxRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
            raceAgeData = new RaceAgeData(defaultSettings, maxRange);
        }
        else
        {
            // update just incase an underlying HAR mod changed.
            raceAgeData.possibleAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
        }

        ageRangeByRace[otherKindRace.defName] = raceAgeData;

        return raceAgeData;
    }
}
