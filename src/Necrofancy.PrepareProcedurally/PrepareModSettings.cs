using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class PrepareModSettings : ModSettings
{
    public string selectedRace;
    public Dictionary<string, RaceAgeData> ageRangeByRace;
    public FloatRange defaultMelaninRange = new (0f, 0.9f);
    public bool allowInjuries = true;
    public bool allowRelationships = true;
    public bool allowPregnancy = true;
    public float skillWeightVariation = 1.5f;
    
    public float maxPassionPoints = 7.0f;
    public bool showCompatibility = false;
    public bool preGenerate;
    public bool autoGenerate;
    
    public override void ExposeData()
    {
        Scribe_Values.Look(ref selectedRace, "selectedRace");
        Scribe_Collections.Look(ref ageRangeByRace, "ageRangeByRace", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref defaultMelaninRange, "melaninRange");
        Scribe_Values.Look(ref allowInjuries, "allowInjuries");
        Scribe_Values.Look(ref allowRelationships, "allowRelationships");
        Scribe_Values.Look(ref allowPregnancy, "allowPregnancy");
        Scribe_Values.Look(ref skillWeightVariation, "skillWeightVariation");
        Scribe_Values.Look(ref maxPassionPoints, "defaultPassionPoints");
        Scribe_Values.Look(ref showCompatibility, "showCompatibility");
        Scribe_Values.Look(ref preGenerate, "generateOnOpen");
        Scribe_Values.Look(ref autoGenerate, "autoGenerate");
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
            { selectedRace, new RaceAgeData(new IntRange(21, 25), new IntRange(20, 120)) }
        };
    }

    public RaceAgeData GetOrCreateRaceAgeSetting(ThingDef otherKindRace, int minimumAdulthoodAge, int maximumAdulthoodAge)
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