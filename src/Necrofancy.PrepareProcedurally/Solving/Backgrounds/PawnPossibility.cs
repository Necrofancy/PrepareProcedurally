namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public readonly record struct PawnPossibility(
    BackstoryPossibility Background,
    IReadOnlyDictionary<SkillDef, IntRange> SkillRanges,
    bool CanChange
)
{
    public PawnBio Bio => Background.Bio;
}
