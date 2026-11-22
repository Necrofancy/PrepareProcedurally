namespace Necrofancy.PrepareProcedurally.Rules;

public interface IRaceRules
{
    IEnumerable<PawnKindDef> PawnKindsFor(FactionDef faction);
    IReadOnlyList<BackstoryCategoryFilter> GetBackstoryCategoryFilters();
    int GetMinimumAgeForAdulthood(ThingDef humanoidThingDef);
    int GetMaximumGeneratedTraits(ThingDef humanoidThingDef);
    GenderPossibility? GetGenderPossibility(ThingDef humanoidThingDef);
}
