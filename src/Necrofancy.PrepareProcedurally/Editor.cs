using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally
{
    public static class Editor
    {
        private static IntRange ageRange = new IntRange(21, 30);
        private static float skillWeightVariation = 1.5f;
        private static FloatRange melaninRange = new FloatRange(0.0f, 0.9f);
        private static float maxPassionPoints = 7.0f;
        private static ThingDef selectedRace;

        public static Dictionary<ThingDef, RaceAgeData> RaceAgeRanges { get; set; }

        public static ThingDef SelectedRace
        {
            get => selectedRace;
            set
            {
                if (selectedRace != value)
                {
                    selectedRace = value;
                    
                    AllowedAgeRange = RaceAgeRanges[value].AllowedAgeRange;
                    AgeRange = RaceAgeRanges[value].AgeRange;
                }
            }
        }

        public static List<List<TraitRequirement>> TraitRequirements { get; set; }
        public static List<SkillPassionSelection> SkillPassions { get; set; }
        public static List<Pawn> StartingPawns { get; set; }
        public static IReadOnlyList<SkillFinalizationResult?> LastResults { get; set; }
        public static HashSet<Pawn> LockedPawns { get; } = new HashSet<Pawn>();
        
        public static IntRange AgeRange
        {
            get => ageRange;
            set => SetProperty(ref ageRange, value);
        }
        
        public static IntRange AllowedAgeRange { get; set; }

        public static float SkillWeightVariation
        {
            get => skillWeightVariation;
            set => SetProperty(ref skillWeightVariation, value);
        }

        public static FloatRange MelaninRange
        {
            get => melaninRange;
            set => SetProperty(ref melaninRange, value);
        }

        public static float MaxPassionPoints
        {
            get => maxPassionPoints;
            set => SetProperty(ref maxPassionPoints, value);
        }

        internal static bool Dirty { get; set; }
        
        internal static bool AllowDirtying { get; set; }

        public static void MakeDirty()
        {
            if (AllowDirtying)
            {
                Dirty = true;
            }
        }
        
        /// <summary>
        /// Set up a clean state based on starting scenario, map tile location, and ideology.
        /// </summary>
        public static void SetCleanState()
        {
            Dirty = false;
            AllowDirtying = false;
            
            ClearState();
            
            SkillPassions = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(SkillPassionSelection.CreateFromSkill).ToList();
            var pawnCount = Find.GameInitData.startingPawnCount;
            StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
            TraitRequirements = StartingPawns.Select(x => new List<TraitRequirement>()).ToList();

            var kind = Faction.OfPlayer.def.basicMemberKind;
            int minimumAdulthoodAge = Compatibility.Layer.GetMinimumAgeForAdulthood(kind);
            int maximumAdulthoodAge = (int)kind.race.race.ageGenerationCurve.Last().x;
            melaninRange = new FloatRange(0.0f, 1f);
            AllowedAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
            ageRange = new IntRange(minimumAdulthoodAge + 1, Math.Min(maximumAdulthoodAge, minimumAdulthoodAge + 9));

            var biologicalSettings = new RaceAgeData(ageRange, AllowedAgeRange);
            RaceAgeRanges = new Dictionary<ThingDef, RaceAgeData> { { kind.race, biologicalSettings } };

            SelectedRace = kind.race;
            
            foreach (var otherKind in Compatibility.Layer.GetPawnKindsThatCanAlsoGenerateFor(Faction.OfPlayer.def))
            {
                minimumAdulthoodAge = Compatibility.Layer.GetMinimumAgeForAdulthood(otherKind);
                maximumAdulthoodAge = (int)otherKind.race.race.ageGenerationCurve.Last().x;
                var allowedAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
                melaninRange = new FloatRange(0.0f, 0.9f);
                ageRange = new IntRange(minimumAdulthoodAge + 1, Math.Min(maximumAdulthoodAge, minimumAdulthoodAge + 9));
                RaceAgeRanges[otherKind.race] = new RaceAgeData(ageRange, allowedAgeRange);
            }

            skillWeightVariation = 1.5f;
            // TODO: For tribal starts it might be fun to have this be based on latitude of the starting location.
            melaninRange = new FloatRange(0.0f, 0.9f);
            maxPassionPoints = 7.0f;
        }
        
        /// <summary>
        /// Clear out any state that would result in Pawns being held onto.
        /// </summary>
        public static void ClearState()
        {
            LastResults = null;
            StartingPawns = null;
            LockedPawns.Clear();
        }

        private static void SetProperty<T>(ref T value, T newValue, [CallerMemberName] string caller = null)
        {
            if (!newValue?.Equals(value) == true)
            {
                value = newValue;
                if (AllowDirtying)
                {
                    Dirty = true;
                    Log.Message($"Property changed on editor for '{caller}' - Dirty for Procedural Generation");
                }
            }
        }
    }
}