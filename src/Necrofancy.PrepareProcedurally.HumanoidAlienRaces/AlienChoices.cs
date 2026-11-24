using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public class AlienChoices : IPawnChoices
{
    public IEnumerable<TraitRequirement> PossibleTraitsFor(PawnSlot slot)
    {
        yield break;
    }

    public IEnumerable<BackstoryDef> PossibleChildhoodsFor(PawnSlot slot)
    {
        yield break;
    }

    public IEnumerable<BackstoryDef> PossibleAdulthoodsFor(PawnSlot slot)
    {
        yield break;
    }

    public IEnumerable<PawnBio> PossibleSolidBiosFor(PawnSlot slot)
    {
        yield break;
    }

    public IEnumerable<BackstoryPossibility> GetAllPossibilities(PawnSlot slot)
    {
        yield break;
    }
}
