using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;

namespace Necrofancy.PrepareProcedurally.Rules;

/// <summary>
/// Provides the required context for Pawn Generation to be steered into giving the correct backstory
/// and passable traits within vanilla pawn generation in one go, as well as touch up skill levels at the end.
/// </summary>
/// <param name="Slot">The slot of starting colonists being randomized</param>
/// <param name="ChosenPossibility">The background that the pawn should have</param>
/// <param name="Skills">Data about what skill ranges that pawn will need</param>
public record Fudging(
    PawnSlot Slot,
    PawnPossibility ChosenPossibility,
    SkillFinalizationResult Skills
)
{
    [CanBeNull] public PawnBio SolidBio => Slot.SolidBio ?? ChosenPossibility.Bio;
}
