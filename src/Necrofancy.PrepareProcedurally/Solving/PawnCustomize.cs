using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Solving;

public record PawnCustomize(PawnSlot Slot, CollectSpecificPassions Collector, RuleSet Rules);
