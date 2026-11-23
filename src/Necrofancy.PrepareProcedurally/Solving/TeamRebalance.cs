using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;

namespace Necrofancy.PrepareProcedurally.Solving;

/// <summary>
/// Provides the required context for Procedural Generation to rebalance the unlocked slots on a starting colony list.
/// </summary>
/// <param name="Editor">Editor context for slot-specific constraints and changes</param>
/// <param name="RuleSet">Details on how humans work and how to influence pawn generation</param>
public record TeamRebalance(EditorContext Editor, RuleSet RuleSet, CandidateSelectionParameters StorySelection)
{
    public IEnumerable<PawnSlot> GetLockedSlots() => Editor.Slots.Where(x => x.Locked);
    public IEnumerable<PawnSlot> GetUnlockedSlots() => Editor.Slots.Where(x => !x.Locked);
}
