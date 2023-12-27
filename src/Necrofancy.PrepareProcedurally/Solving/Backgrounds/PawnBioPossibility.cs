using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public readonly struct BioPossibility
{
    public BioPossibility(PawnBio bio)
    {
        Gender = bio.gender;
        Name = bio.name;

        Childhood = bio.childhood;
        Adulthood = bio.adulthood;
        Traits = new List<TraitRequirement>();
        AddIfAny(Childhood);
        AddIfAny(Adulthood);
    }
        
    public BioPossibility(BackstoryDef childhood, BackstoryDef adulthood)
    {
        Childhood = childhood;
        Adulthood = adulthood;

        Name = null;
        Gender = GenderPossibility.Either;
        Traits = new List<TraitRequirement>();
        AddIfAny(Childhood);
        AddIfAny(Adulthood);
    }

    public BackstoryDef Childhood { get; }
        
    public BackstoryDef Adulthood { get; }
        
    public List<TraitRequirement> Traits { get; }

    [CanBeNull]
    public NameTriple Name { get; }
        
    public GenderPossibility Gender { get; }
        
    public bool DisablesWorkType(SkillDef skill)
    {
        var disables = Adulthood.workDisables | Childhood.workDisables;
        var workDisables = Adulthood.DisabledWorkTypes.Concat(Childhood.DisabledWorkTypes);
        return skill.IsDisabled(disables, workDisables);
    }

    public bool HasForcedPassionByTrait(SkillDef def)
    {
        foreach (var trait in Traits.Where(x => x.def.forcedPassions != null))
        {
            if (trait.def.forcedPassions?.Contains(def) == true)
                return true;
        }

        return false;
    }
        
    public bool PassionConflictsWith(SkillDef def)
    {
        foreach (var trait in Traits.Where(x => x.def.conflictingPassions != null))
        {
            if (trait.def.conflictingPassions?.Contains(def) == true)
                return true;
        }

        return false;
    }
        
    public bool HasConflictingPassions(SkillDef def)
    {
        foreach (var trait in Traits.Where(x => x.def.conflictingPassions != null))
        {
            if (trait.def.conflictingPassions?.Contains(def) == true)
                return true;
        }

        return false;
    }
        
    private void AddIfAny(BackstoryDef story)
    {
        if (story.forcedTraits != null)
        {
            foreach (var trait in story.forcedTraits)
            {
                if (!Traits.Any(req => req.def == trait.def && req.degree == trait.degree))
                {
                    Traits.Add(new TraitRequirement{def = trait.def, degree = trait.degree});
                }
            }
        }
    }
}