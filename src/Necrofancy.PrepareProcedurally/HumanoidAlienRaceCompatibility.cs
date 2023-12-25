using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally
{
    public static class HumanoidAlienRaceCompatibility
    {
        public static bool IsHumanoidAlienRacePawn(Pawn pawn)
        {
            return !pawn.def.defName.Equals("human");
        }
        
        public static bool IsHumanoidAlienRaceThingDef(ThingDef def)
        {
            return !def.defName.Equals("human");
        }

        public static int GetMaxTraits(Pawn harPawn)
        {
            var traits = harPawn.def
                ?.FieldUnder("alienRace")
                ?.FieldUnder("generalSettings")
                ?.FieldUnder("additionalTraits");

            return traits is IntRange range ? range.RandomInRange : 0;
        }

        public static List<TraitRequirement> GetTraitRequirements(Pawn harPawn)
        {
            List<TraitRequirement> requirements = new List<TraitRequirement>();
            object forcedRaceTraitEntries = harPawn.def
                ?.FieldUnder("alienRace")
                ?.FieldUnder("generalSettings")
                ?.FieldUnder("forcedRaceTraitEntries");
            
            if (forcedRaceTraitEntries is IEnumerable traits)
            {
                foreach (object entry in traits)
                {
                    float commonalityMale = entry.FieldAs<float>("commonalityMale");
                    float commonalityFemale = entry.FieldAs<float>("commonalityFemale");
                    float chance = entry.FieldAs<float>("chance");
                    int degree = entry.FieldAs<int>("degree");
                    
                    object supposedlyATraitDefName = entry.FieldUnder("defName");
                    TraitDef actualDef;
                    switch (supposedlyATraitDefName)
                    {
                        case string itWasActuallyTheDefName:
                            actualDef = DefDatabase<TraitDef>.GetNamed(itWasActuallyTheDefName);
                            break;
                        case TraitDef itWasActuallyJustTheDef:
                            actualDef = itWasActuallyJustTheDef;
                            break;
                        default:
                            // don't care what this is anymore, let's just move on and hope for the best...
                            continue;
                    }
                    
                    float commonality = harPawn.gender switch
                    {
                        Gender.Male => commonalityMale,
                        Gender.Female => commonalityFemale,
                        _ => -1f
                    };
                    
                    if (chance >= 100 && commonality < 0)
                    {
                        requirements.Add(new TraitRequirement{def = actualDef, degree = degree});
                    }
                }
            }

            return requirements;
        }
        
        public static int GetAgeForAdulthoodBackstories(PawnKindDef memberKind)
        {
            object minAgeForAdulthood = memberKind
                ?.FieldUnder("alienRace")
                ?.FieldUnder("generalSettings")
                ?.FieldUnder("minAgeForAdulthood");

            return minAgeForAdulthood is float adulthoodAge && adulthoodAge < 0 ? (int)adulthoodAge : 20;
        }

        private static object FieldUnder(this object obj, string property)
        {
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.Name.Equals(property) && field.GetValue(obj) is { } returnable)
                {
                    return returnable;
                }
            }

            return null;
        }

        private static T FieldAs<T>(this object obj, string property)
        {
            object item = obj.FieldUnder(property);
            if (item is T successfulCast)
            {
                return successfulCast;
            }

            string error = $"Cannot cast {obj.GetType().Name}.{property} to type {typeof(T).Name} - it is {item?.GetType().Name ?? "null"}";
            throw new InvalidOperationException(error);
        }
    }
}