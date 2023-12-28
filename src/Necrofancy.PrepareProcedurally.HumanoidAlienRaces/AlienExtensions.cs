using System;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public static class AlienExtensions
{
    public static bool IsHuman(this Pawn pawn)
    {
        return pawn.def.defName.Equals("Human", StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool IsHuman(this ThingDef def)
    {
        return def.defName.Equals("Human", StringComparison.OrdinalIgnoreCase);
    }
}