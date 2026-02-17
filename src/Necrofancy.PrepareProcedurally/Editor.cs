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
    private static readonly FloatRange DefaultMelaninRange = new(0.0f, 0.9f);
    
    public static Dictionary<ThingDef, RaceAgeData> RaceAgeRanges { get; private set; }

    public static ThingDef SelectedRace
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;

                AllowedAgeRange = RaceAgeRanges[value].AllowedAgeRange;
                AgeRange = RaceAgeRanges[value].AgeRange;
            }
        }
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
                RaceAgeRanges[SelectedRace] = RaceAgeRanges[SelectedRace].WithUpdatedAge(value);
        }
    }

    public static IntRange AllowedAgeRange { get; private set; }

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
        Dirty = false;
        AllowDirtying = false;

        ClearState();

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

        var kind = Faction.OfPlayer.def.basicMemberKind;
        var minimumAdulthoodAge = Compatibility.Layer.GetMinimumAgeForAdulthood(kind);
        var maximumAdulthoodAge = (int)kind.race.race.ageGenerationCurve.Last().x;
        AllowedAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
        AgeRange = new IntRange(minimumAdulthoodAge + 1, Math.Min(maximumAdulthoodAge, minimumAdulthoodAge + 9));

        var biologicalSettings = new RaceAgeData(AgeRange, AllowedAgeRange);
        RaceAgeRanges = new Dictionary<ThingDef, RaceAgeData> { { kind.race, biologicalSettings } };

        SelectedRace = kind.race;

        foreach (var otherKind in Compatibility.Layer.GetPawnKindsThatCanAlsoGenerateFor(Faction.OfPlayer.def))
        {
            minimumAdulthoodAge = Compatibility.Layer.GetMinimumAgeForAdulthood(otherKind);
            maximumAdulthoodAge = (int)otherKind.race.race.ageGenerationCurve.Last().x;
            var allowedAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
            AgeRange = new IntRange(minimumAdulthoodAge + 1, Math.Min(maximumAdulthoodAge, minimumAdulthoodAge + 9));
            RaceAgeRanges[otherKind.race] = new RaceAgeData(AgeRange, allowedAgeRange);
        }

        SkillWeightVariation = 1.5f;
        // TODO: For tribal starts it might be fun to have this be based on latitude of the starting location.
        MelaninRange = DefaultMelaninRange;
        MaxPassionPoints = 7.0f;
        
        AllowDirtying = true;
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