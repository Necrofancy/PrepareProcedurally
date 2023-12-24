using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits
{
    /// <summary>
    /// A system for editing definitions and resources to intentionally bias pawn generation one way or another.
    /// </summary>
    public static class NarrowBioEditor
    {
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

        public static TemporaryEdit<PawnGenerationRequest> FilterRequestAge(int pawnIndex, int min, int max)
        {
            if (StartingPawnUtilityState.GetGenerationRequestsList() is {} requests && pawnIndex < requests.Count)
            {
                var oldRequest = requests[pawnIndex];
                var newRequest = requests[pawnIndex];
                    
                // you need to remove EXCLUDES 
                newRequest.ExcludeBiologicalAgeRange = null;
                newRequest.BiologicalAgeRange = new FloatRange(min, max);
                void SetAgeRange(PawnGenerationRequest request)
                {
                    requests[pawnIndex] = request;
                }

                return new TemporaryEdit<PawnGenerationRequest>(oldRequest, newRequest, SetAgeRange);
            }

            void DoNothing(PawnGenerationRequest request)
            {
                //does nothing
            }
            return new TemporaryEdit<PawnGenerationRequest>(default, default, DoNothing);
        }

        public static IDisposable MelaninRange(float min, float max)
        {
            return new NarrowMelaninRange(min, max);
        }

        public static IDisposable RestrictTraits(List<TraitRequirement> required, List<TraitDef> banned)
        {
            return new NarrowTraits(required, banned);
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