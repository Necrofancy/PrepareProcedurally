using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class Adulthood : Backstory
    {
        protected override BackstoryDef StoryFrom(Pawn pawn)
        {
            return pawn.story.Adulthood;
        }
    }
}