using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Rules;

public interface IPawnChoices
{
    IEnumerable<TraitRequirement> PossibleTraitsFor(PawnSlot slot);
    IEnumerable<BackstoryDef> PossibleChildhoodsFor(PawnSlot slot);
    IEnumerable<BackstoryDef> PossibleAdulthoodsFor(PawnSlot slot);
    IEnumerable<PawnBio> PossibleSolidBiosFor(PawnSlot slot);
    IEnumerable<BackstoryPossibility> GetAllPossibilities(PawnSlot slot);
}
