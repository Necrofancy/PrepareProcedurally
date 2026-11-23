namespace Necrofancy.PrepareProcedurally.Rules;

/// <summary>
/// Provides implementations on how to find out how humanoid <see cref="ThingDef"/>s work, as well as how
/// to safely apply changes to them in pawn generation.
/// </summary>
/// <param name="RaceRules">Basic facts about default humanoid characteristics that mods may change.</param>
/// <param name="Choices">An enumerator of choices for backstory options</param>
/// <param name="Fudger">How to "safely" impose conditions on pawn generation</param>
public record RuleSet(IRaceRules RaceRules, IPawnChoices Choices, IPawnFudge Fudger)
{
    public static RuleSet Vanilla { get; } = CreateVanillaSettings();

    private static RuleSet CreateVanillaSettings()
    {
        var backstories = DefDatabase<BackstoryDef>.AllDefsListForReading;
        var traits = DefDatabase<TraitDef>.AllDefsListForReading;
        var bios = SolidBioDatabase.allBios;
        IReadOnlyList<BackstoryCategoryFilter> GetFilters() => Faction.OfPlayer.def.backstoryFilters;

        var rules = new VanillaRules();
        var choices = new VanillaChoices(backstories, traits, bios, GetFilters);
        var fudge =  new VanillaFudge();
        return new RuleSet(rules, choices, fudge);
    }
}
