using RimWorld.Planet;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving;

/// <summary>
/// Pawn generation will try to generate invisible world pawns in the background.
/// This is a problem for mass-generating pawns, as this doesn't get cleaned up.
/// </summary>
public static class WorldPawnDestruction
{
    /// <summary>
    /// Clean up the world pawns related to this pawn before randomization.
    /// </summary>
    public static void DestroyRelationships(Pawn pawn)
    {
        foreach (var related in pawn.relations.RelatedPawns)
        {
            if (related.IsWorldPawn() && related.Faction?.leader != related)
            {
                Find.WorldPawns.RemoveAndDiscardPawnViaGC(related);
            }
        }
    }
}