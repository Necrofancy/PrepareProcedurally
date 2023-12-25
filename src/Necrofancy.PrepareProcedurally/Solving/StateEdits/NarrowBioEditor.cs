using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits
{
    /// <summary>
    /// A system for editing definitions and resources to intentionally bias pawn generation one way or another.
    /// </summary>
    public static class NarrowBioEditor
    {
        public static TemporaryEdit<SimpleCurve> ReplaceAgeGenerationCurve(IntRange ageRange)
        {
            SimpleCurve generationCurve = Faction.OfPlayer.def.basicMemberKind.race.race.ageGenerationCurve;
            var temporaryCurve = EstimateRolling.SubSampleCurve(generationCurve, ageRange);

            void SetCurve(SimpleCurve curve)
            {
                Faction.OfPlayer.def.basicMemberKind.race.race.ageGenerationCurve = curve;
            }
            
            return new TemporaryEdit<SimpleCurve>(generationCurve, temporaryCurve, SetCurve);
        }

        public static TemporaryEdit<FloatRange> MelaninRange(FloatRange temporaryRange)
        {
            var factionDef = Faction.OfPlayer.def;
            var currentRange = factionDef.melaninRange;

            void SetMelaninRange(FloatRange range) => factionDef.melaninRange = range;

            return new TemporaryEdit<FloatRange>(currentRange, temporaryRange, SetMelaninRange);
        }

        public static TemporaryEdit<List<TraitRequirement>> ForceTraits(List<TraitRequirement> required)
        {
            var memberKind = Faction.OfPlayer.def.basicMemberKind;
            var currentList = memberKind.forcedTraits;
            var newList = currentList != null ? currentList.Concat(required).ToList() : required;

            void SetForcedTraits(List<TraitRequirement> traits) => memberKind.forcedTraits = traits;

            return new TemporaryEdit<List<TraitRequirement>>(currentList, newList, SetForcedTraits);
        }

        public static TemporaryEdit<List<TraitDef>> BanTraits(List<TraitDef> banned)
        {
            var memberKind = Faction.OfPlayer.def.basicMemberKind;
            var currentList = memberKind.disallowedTraits;
            var newList = currentList != null ? currentList.Concat(banned).ToList() : banned;  
            
            void SetDisallowedTraits(List<TraitDef> traits) => memberKind.disallowedTraits = traits;

            return new TemporaryEdit<List<TraitDef>>(currentList, newList, SetDisallowedTraits);
        }

    }
}