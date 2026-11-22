namespace Necrofancy.PrepareProcedurally.Rules;

/// <summary>
/// Provides implementations on how to find out how humanoid <see cref="ThingDef"/>s work, as well as how
/// to safely apply changes to them in pawn generation.
/// </summary>
/// <param name="RaceRules">Basic facts about default humanoid characteristics that mods may change.</param>
/// <param name="Fudger">How to "safely" impose conditions on pawn generation</param>
public record RuleSet(IRaceRules RaceRules, IPawnFudge Fudger)
{
    public static RuleSet Vanilla { get; } = new(new VanillaRules(), new VanillaFudge());
}
