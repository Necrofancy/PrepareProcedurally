using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits
{
    /// <summary>
    /// A system for editing definitions and resources to intentionally bias pawn generation one way or another.
    /// </summary>
    public class NarrowBioEditor
    {
        private readonly List<PawnBio> _bioList = new List<PawnBio>();
        private readonly List<BackstoryDef> _backstories = new List<BackstoryDef>();
         
        public List<TraitRequirement> ForcedTraits { get; } = new List<TraitRequirement>();
        public List<TraitDef> BannedTraits { get; } = new List<TraitDef>();
        public WorkTags RequiredWork { get; set; } = WorkTags.None;
        
        public string CategoryName { get; set; }
        

        public static TemporaryEdit<IntRange> FilterPawnAges(int min, int max)
        {
            var pawnDef = Faction.OfPlayer.def.basicMemberKind;

            var oldRange = new IntRange(pawnDef.minGenerationAge, pawnDef.maxGenerationAge);
            var newRange = new IntRange(min, max);

            void SetAgeRange(IntRange range)
            {
                pawnDef.minGenerationAge = range.min;
                pawnDef.maxGenerationAge = range.max;
            }

            return new TemporaryEdit<IntRange>(oldRange, newRange, SetAgeRange);
        }
        
        

        public static IDisposable MelaninRange(float min, float max)
        {
            return new NarrowMelaninRange(min, max);
        }

        public static IDisposable RestrictTraits(List<TraitRequirement> required, List<TraitDef> banned)
        {
            return new NarrowTraits(required, banned);
        }

        public IDisposable AdjustTraits()
        {
            return new NarrowTraits(ForcedTraits, BannedTraits);
        }

        private void NarrowDownBioDatabase(string categoryName)
        {
            this._bioList.Clear();
            foreach (var bio in PossibleBackstories.FromCategoryName(categoryName))
            {
                if (AllowedBio(bio))
                    _bioList.Add(bio);
            }
        }

        private void NarrowDownBackstoryDatabase(string categoryName)
        {
            this._backstories.Clear();
            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                bool desiredBackstory = this.MatchingBackstory(backstory) && backstory.shuffleable;
                // Pawn generation does not like if we eliminate other category backstories. Don't do that.
                bool notInCategory = !backstory.spawnCategories.Contains(categoryName);
                if (desiredBackstory || notInCategory)
                    _backstories.Add(backstory);
            }

            Log.Message($"There are {_backstories.Count} relevant backstories");
        }

        private bool AllowedBio(PawnBio bio)
        {
            return (MatchingBackstory(bio.childhood) || MatchingBackstory(bio.adulthood)) && 
                   PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Any(x => Equals(x.Name, bio.name));
        }

        private bool MatchingBackstory(BackstoryDef backstory)
        {
            if (!backstory.spawnCategories.Contains(CategoryName))
                return false;
            
            if (backstory.disallowedTraits != null)
                foreach (var traitNotAllowed in backstory.disallowedTraits)
                    if (ForcedTraits.Any(req => req.def == traitNotAllowed.def))
                        return false;
            
            if (backstory.forcedTraits?.Any(entry => this.BannedTraits.Contains(entry.def)) == true)
                return false;
            
            // if (backstory.DisablesWorkType(RequiredWork))
            //     return false;

            return true;
        }

        public void Clear()
        {
            BannedTraits.Clear();
            ForcedTraits.Clear();
            RequiredWork = WorkTags.None;
        }

        private readonly struct NarrowAge : IDisposable
        {
            public NarrowAge(int min, int max)
            {
                var pawnDef = Faction.OfPlayer.def.basicMemberKind;
                
                PreviousMinAge = pawnDef.minGenerationAge;
                PreviousMaxAge = pawnDef.maxGenerationAge;
                
                pawnDef.minGenerationAge = min;
                pawnDef.maxGenerationAge = max;
            }
            
            private int PreviousMinAge { get; }
            private int PreviousMaxAge { get; }

            public void Dispose()
            {
                var pawnDef = Faction.OfPlayer.def.basicMemberKind;
                pawnDef.minGenerationAge = PreviousMinAge;
                pawnDef.maxGenerationAge = PreviousMaxAge;
            }
        }

        private readonly struct NarrowTraits : IDisposable
        {
            public NarrowTraits(List<TraitRequirement> forcedTraits, List<TraitDef> disallowedTraits)
            {
                var pawnDef = Faction.OfPlayer.def.basicMemberKind;
                PreviouslyForcedTraits = pawnDef.forcedTraits;
                PreviouslyDisallowedTraits = pawnDef.disallowedTraits;

                pawnDef.forcedTraits = forcedTraits;
                pawnDef.disallowedTraits = disallowedTraits;
            }
            
            private List<TraitDef> PreviouslyDisallowedTraits { get; }
            private List<TraitRequirement> PreviouslyForcedTraits { get; }

            public void Dispose()
            {
                var pawnDef = Faction.OfPlayer.def.basicMemberKind;
                pawnDef.forcedTraits = PreviouslyForcedTraits;
                pawnDef.disallowedTraits = PreviouslyDisallowedTraits;
            }
        }
        
        private readonly struct NarrowMelaninRange : IDisposable
        {
            public NarrowMelaninRange(float min, float max)
            {
                float midPoint = (min + max) * 0.5f;
                float variance = Math.Abs(max - min) * 0.5f;
                
                var faction = Faction.OfPlayer;
                OldRange = faction.def.melaninRange;
                
                faction.def.melaninRange = new FloatRange(min, max);
            }
            
            private FloatRange OldRange { get; }
            
            public void Dispose()
            {
                var faction = Faction.OfPlayer;
                faction.def.melaninRange = OldRange;
            }
        }
    }
}