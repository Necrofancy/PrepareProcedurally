using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public static class Editor
{
    public static Dictionary<ThingDef, RaceAgeData> RaceAgeRanges { get; private set; }

    public static ThingDef SelectedRace
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;

                RaceAgeData = RaceAgeRanges[value];
                AgeRange = RaceAgeRanges[value].desiredAgeRange;
            }
        }
    }
    
    public static RaceAgeData RaceAgeData
    {
        get; private set;
    }


    public static HashSet<TraitDef> TraitsThatDisablePassions { get; } = new();
    public static List<TraitRequirement>[] TraitRequirements { get; private set; }
    public static BackstoryDef[] SetChildhoods { get; private set; }
    public static BackstoryDef[] SetAdulthoods { get; private set; }
    public static GenderPossibility[] GenderRequirements { get; set; }
    public static List<SkillPassionSelection> SkillPassions { get; private set; }
    public static Pawn[] StartingPawns { get; set; }
    public static IReadOnlyList<SkillFinalizationResult?> LastResults { get; set; }
    public static HashSet<Pawn> LockedPawns { get; } = new();
    
    public static IntRange AgeRange
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RaceAgeRanges[SelectedRace] = RaceAgeRanges[SelectedRace].WithUpdatedAge(value);
            }
        }
    }
    
    public static float SkillWeightVariation { get; set => SetProperty(ref field, value); }= 1.5f;

    public static FloatRange MelaninRange
    {
        get;
        set => SetProperty(ref field, value);
    }

    public static float MaxPassionPoints
    {
        get;
        set => SetProperty(ref field, value);
    }

    public static bool Dirty { get; set; }

    public static bool AllowDirtying { get; set; }

    public static bool AllowBadHeDiffs
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public static bool AllowRelationships
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public static bool AllowPregnancy
    {
        get;
        set => SetProperty(ref field, value);
    } = true;
    
    public static bool ShowCompatibility { get; set; } = true;

    public static void MakeDirty([CallerMemberName]string caller = null)
    {
        Logging.Debug($"Dirtying editor from '{caller}'");
        if (AllowDirtying) Dirty = true;
    }

    /// <summary>
    /// Set up a clean state based on starting scenario, map tile location, and ideology.
    /// </summary>
    public static void SetCleanState()
    {
        Logging.Debug("Cleaning Editor state");
        Dirty = false;
        AllowDirtying = false;
        
        ClearState();

        var settings = PrepareMod.Settings;
        var (selectedRace, raceAges) = settings.GetLoadedHumanTypes();
        
        RaceAgeRanges = raceAges;

        SelectedRace = selectedRace;

        foreach (var trait in DefDatabase<TraitDef>.AllDefsListForReading)
            if (trait.conflictingPassions?.Any() == true)
                TraitsThatDisablePassions.Add(trait);

        SkillPassions = DefDatabase<SkillDef>.AllDefsListForReading
            .Select(SkillPassionSelection.CreateFromSkill).ToList();
        var pawnCount = Find.GameInitData.startingPawnCount;
        StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToArray();
        TraitRequirements = StartingPawns.Select(_ => new List<TraitRequirement>()).ToArray();
        GenderRequirements = StartingPawns.Select(_ => GenderPossibility.Either).ToArray();
        SetChildhoods = new BackstoryDef[pawnCount];
        SetAdulthoods = new BackstoryDef[pawnCount];
        
        foreach (var otherKind in Compatibility.Layer.GetPawnKindsThatCanAlsoGenerateFor(Faction.OfPlayer.def))
        {
            var minimumAdulthoodAge = Compatibility.Layer.GetMinimumAgeForAdulthood(otherKind);
            var maximumAdulthoodAge = (int)otherKind.race.race.ageGenerationCurve.Last().x;
            var raceAge = settings.GetOrCreateRaceAgeSetting(otherKind.race, minimumAdulthoodAge, maximumAdulthoodAge);
            AgeRange = raceAge.desiredAgeRange;
            RaceAgeRanges[otherKind.race] = raceAge;
        }
        
        AgeRange = RaceAgeRanges[SelectedRace].desiredAgeRange;
        MelaninRange = settings.defaultMelaninRange;
        
        AllowBadHeDiffs = settings.allowInjuries;
        AllowRelationships = settings.allowRelationships;
        AllowPregnancy = settings.allowPregnancy;
        
        SkillWeightVariation = settings.skillWeightVariation;
        MaxPassionPoints = settings.maxPassionPoints;
        
        AllowDirtying = settings.autoGenerate;
    }

    public static void RefreshPawnList()
    {
        var pawnCount = Find.GameInitData.startingPawnCount;
        StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToArray();
        var pawnsToRemove = LockedPawns.Where(pawn => !StartingPawns.Contains(pawn)).ToList();

        foreach (var pawn in pawnsToRemove) LockedPawns.Remove(pawn);
    }

    /// <summary>
    /// Clear out any state that would result in Pawns being held onto.
    /// </summary>
    public static void ClearState()
    {
        LastResults = null;
        StartingPawns = null;
        LockedPawns.Clear();
        TraitsThatDisablePassions.Clear();
    }

    // ReSharper disable once UnusedParameter.Local
    private static bool SetProperty<T>(ref T value, T newValue, [CallerMemberName] string caller = null)
    {
        if (!AllowDirtying)
        {
            value = newValue;
            return false;
        }
        
        if (!newValue?.Equals(value) == true)
        {
            value = newValue;
            Dirty = true;
            Logging.Debug($"Property changed on editor for '{caller}' - Dirty for Procedural Generation");
            return true;
        }

        return false;
    }
}