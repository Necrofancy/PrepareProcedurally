using AlienRace;
using Necrofancy.PrepareProcedurally.Rules;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public class AlienRules : IRaceRules
{
    public IEnumerable<PawnKindDef> PawnKindsFor(FactionDef faction)
    {
        return (from alienSettings in DefDatabase<RaceSettings>.AllDefsListForReading
            from factionPawnKind in alienSettings.pawnKindSettings.startingColonists.Where(x => x.factionDefs.Contains(faction))
            from pawnKind in factionPawnKind.pawnKindEntries.SelectMany(e => e.kindDefs)
            select pawnKind).Distinct();
    }

    public int GetMinimumAgeForAdulthood(ThingDef humanoidThingDef)
    {
        return humanoidThingDef is ThingDef_AlienRace race
            ? (int)race.alienRace.generalSettings.minAgeForAdulthood
            : 20;
    }

    public int GetMaximumGeneratedTraits(ThingDef humanoidThingDef)
    {
        return humanoidThingDef is ThingDef_AlienRace alienDef
            ? alienDef.alienRace.generalSettings.additionalTraits.max + Vanilla.MaxNonSexualityTraits
            : Vanilla.MaxNonSexualityTraits;
    }

    public GenderPossibility? GetGenderPossibility(ThingDef humanoidThingDef)
    {
        if (humanoidThingDef is not ThingDef_AlienRace race)
            return GenderPossibility.Either; // human baseline is either

        if (!humanoidThingDef.race.hasGenders)
            return null;

        var maleProbability = race.alienRace.generalSettings.maleGenderProbability;
        return maleProbability switch
        {
            <= 0 => GenderPossibility.Female,
            >= 1 => GenderPossibility.Male,
            _ => GenderPossibility.Either,
        };
    }
}
