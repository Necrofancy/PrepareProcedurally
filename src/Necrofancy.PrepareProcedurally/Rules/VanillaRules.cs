namespace Necrofancy.PrepareProcedurally.Rules;

public class VanillaRules : IRaceRules
{
    public IEnumerable<PawnKindDef> PawnKindsFor(FactionDef faction) => [faction.basicMemberKind];

    public int GetMinimumAgeForAdulthood(ThingDef _) => 20;

    public int GetMaximumGeneratedTraits(ThingDef _) => Vanilla.MaxNonSexualityTraits;

    public GenderPossibility? GetGenderPossibility(ThingDef _) => GenderPossibility.Either;
}
