using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Rules;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

[StaticConstructorOnStartup, UsedImplicitly]
public static class SettingsChange
{
    static SettingsChange()
    {
        PrepareMod.Settings.RuleSet = new RuleSet(new AlienRules(), new AlienChoices(), new AlienFudge());
    }
}
