using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public class BalancingSituation
    {
        public BalancingSituation(string name, string categoryName, int pawns, IReadOnlyList<SkillPassionSelection> skillRequirements)
        {
            Name = name;
            CategoryName = categoryName;
            Pawns = pawns;
            SkillRequirements = skillRequirements;
        }
        
        public string Name { get; }
        public string CategoryName { get; }
        public int Pawns { get; }
        
        public IReadOnlyList<SkillPassionSelection> SkillRequirements { get; }

        public static BalancingSituation CrashLanded(string name, params SkillPassionSelection[] skillRolls)
        {
            return new BalancingSituation(name, "Offworld", 3, skillRolls);
        }
        
        public static BalancingSituation Tribals(string name, params SkillPassionSelection[] skillRolls)
        {
            return new BalancingSituation(name, "Tribal", 5, skillRolls);
        }
        
        public static BalancingSituation Solo(string name, params SkillPassionSelection[] skillRolls)
        {
            return new BalancingSituation(name, "Offworld", 1, skillRolls);
        }
    }
}