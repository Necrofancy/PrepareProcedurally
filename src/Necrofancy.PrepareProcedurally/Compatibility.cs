using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class Compatibility
{
    public static Compatibility Layer { get; protected set; } = new Compatibility();
        
    public virtual int GetMaximumGeneratedTraits(Pawn pawn) => 3;
        
    public virtual IEnumerable<TraitRequirement> GetExtraTraitRequirements(Pawn pawn) => Enumerable.Empty<TraitRequirement>();
        
    public virtual int GetMinimumAgeForAdulthood(PawnKindDef kind) => 20;
        
    public virtual IEnumerable<PawnKindDef> GetPawnKindsThatCanAlsoGenerateFor(FactionDef def) => Enumerable.Empty<PawnKindDef>();
        
    public virtual bool AllowEditingBodyType(Pawn pawn) => true;

    public virtual void RandomizeForTeam(BalancingSituation situation)
    {
        ProcGen.RandomizeForTeam(situation);
    }

    public virtual Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector,
        List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
    {
        return ProcGen.RandomizeSingularPawn(pawn, collector, reqs);
    }
}