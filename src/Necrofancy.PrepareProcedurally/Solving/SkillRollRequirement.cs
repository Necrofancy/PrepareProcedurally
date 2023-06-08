using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public class SkillRollRequirement
    {
        public SkillRollRequirement(SkillDef def)
        {
            Def = def;
        }

        public SkillDef Def { get; }

        public Passion Passion { get; set; }

        public int LevelMinimum { get; set; }
        
        public bool AllPawns { get; set; }
        
        public int Count { get; set; }
    }
}