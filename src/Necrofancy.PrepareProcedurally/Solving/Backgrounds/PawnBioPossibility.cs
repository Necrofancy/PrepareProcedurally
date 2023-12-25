using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds
{
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
        private NameTriple Name { get; }
        
        private GenderPossibility Gender { get; }
        
        public bool DisablesWorkType(SkillDef skill)
        {
            var disables = Adulthood.workDisables | Childhood.workDisables;
            var workDisables = Adulthood.DisabledWorkTypes.Concat(Childhood.DisabledWorkTypes);
            return skill.IsDisabled(disables, workDisables);
        }
        
        public void ApplyTo(Pawn pawn)
        {
            var possessions = Find.GameInitData.startingPossessions[pawn];
            if (pawn.story.Adulthood != null)
            {
                var possessionsToRemove = pawn.story.Adulthood.possessions;
                foreach (var itemToRemove in possessionsToRemove)
                {
                    for (var i = possessions.Count - 1; i >= 0; i--)
                    {
                        var item = possessions[i];
                        if (item.ThingDef == itemToRemove.key)
                        {
                            possessions.RemoveAt(i);
                        }
                    }
                }
            }

            foreach (var item in Adulthood.possessions)
            {
                possessions.Add(new ThingDefCount(item.key, Math.Min(item.value, item.key.stackLimit)));
            }
            
            pawn.story.Adulthood = Adulthood;
            pawn.story.Childhood = Childhood;

            if (Gender != GenderPossibility.Either)
            {
                pawn.gender = Gender == GenderPossibility.Female
                    ? Verse.Gender.Female
                    : Verse.Gender.Male;
            }

            pawn.Name = Name ?? PawnBioAndNameGenerator.GeneratePawnName(pawn);
            
            var bodyTypeSetByBiotech = false;
            var isModRacePawn = HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn);

            if (!isModRacePawn)
            {
                if (ModsConfig.BiotechActive)
                {
                    var bodyTypes = pawn.genes.GenesListForReading.Where(g => g.Active && g.def.bodyType != null)
                        .Select(g => g.def.bodyType.Value).Distinct().ToList();

                    if (bodyTypes.Any())
                    {
                        pawn.story.bodyType = GeneUtility.ToBodyType(bodyTypes.RandomElement(), pawn);
                        bodyTypeSetByBiotech = true;
                    }
                }
                
                if (!bodyTypeSetByBiotech && Adulthood.BodyTypeFor(pawn.gender) is {} bodyType)
                {
                    pawn.story.bodyType = bodyType;
                }
            }

            TraitUtilities.FixTraitOverflow(pawn);
            pawn.Notify_DisabledWorkTypesChanged();
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
}