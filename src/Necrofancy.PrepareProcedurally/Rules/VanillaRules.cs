namespace Necrofancy.PrepareProcedurally.Rules;

public class VanillaRules : IRaceRules
{
    public IEnumerable<PawnKindDef> PawnKindsFor(FactionDef faction) => [faction.basicMemberKind];

    public IReadOnlyList<BackstoryCategoryFilter> GetBackstoryCategoryFilters()
    {
        return Faction.OfPlayer.def.basicMemberKind.backstoryFilters;
    }

    public int GetMinimumAgeForAdulthood(ThingDef _) => 20;

    public int GetMaximumGeneratedTraits(ThingDef _) => 3;

    public GenderPossibility? GetGenderPossibility(ThingDef _) => GenderPossibility.Either;
}
