using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

/// <summary>
/// Almost all changes made to pawns post-generation as part of procedural generation will be put here.
/// Traits will go in either <see cref="Interface.PawnColumnWorkers.Traits"/> or <see cref="TraitUtilities"/>
/// Because the fixups for those will also apply to 
/// </summary>
public static class PostPawnGenerationChanges
{
    /// <summary>
    /// Fix up possessions, story, and - for humans only - pawn bodytype to match the relevant backstory
    /// of the given <see cref="BioPossibility"/>.
    /// </summary>
    public static void ApplyBackstoryTo(BioPossibility bio, Pawn pawn)
    {
        pawn.story.Childhood = bio.Childhood;

        var possessions = Find.GameInitData.startingPossessions[pawn];
        RemoveBackstoryPossessions(pawn, possessions);
        pawn.story.Adulthood = bio.Adulthood;
        AddBackstoryPossessions(bio, possessions);

        // Respect a Kickstarter NameTriple or just re-generate the name.
        pawn.Name = bio.Name ?? PawnBioAndNameGenerator.GeneratePawnName(pawn);

        ApplyBodyTypeFromBackstory(bio, pawn);

        pawn.Notify_DisabledWorkTypesChanged();
    }

    internal static void ApplyBodyTypeFromBackstory(BioPossibility bio, Pawn pawn)
    {
        var bodyTypeSetByBiotech = false;

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

        if (!bodyTypeSetByBiotech && bio.Adulthood.BodyTypeFor(pawn.gender) is { } bodyType)
            pawn.story.bodyType = bodyType;
    }

    internal static void AddBackstoryPossessions(BioPossibility bio, List<ThingDefCount> possessions)
    {
        foreach (var item in bio.Adulthood.possessions)
            possessions.Add(new ThingDefCount(item.key, item.value.RandomInRange));
    }

    internal static void RemoveBackstoryPossessions(Pawn pawn, List<ThingDefCount> possessions)
    {
        if (pawn.story.Adulthood != null)
        {
            var possessionsToRemove = pawn.story.Adulthood.possessions;
            foreach (var itemToRemove in possessionsToRemove)
                for (var i = possessions.Count - 1; i >= 0; i--)
                {
                    var item = possessions[i];
                    if (item.ThingDef == itemToRemove.key) possessions.RemoveAt(i);
                }
        }
    }

    /// <summary>
    /// Apply simulated skill ranges to the pawn's given stats.
    /// </summary>
    public static void ApplySimulatedSkillsTo(this IReadOnlyDictionary<SkillDef, PassionAndLevel> finalRanges,
        Pawn pawn)
    {
        var passionsModdedByGenes = new Dictionary<SkillDef, PassionMod.PassionModType>();
        foreach (var gene in pawn.genes.GenesListForReading.Where(x => x.Active && x.def.passionMod != null))
            passionsModdedByGenes[gene.def.passionMod.skill] = gene.def.passionMod.modType;
        foreach (var skillRecord in pawn.skills.skills)
        {
            var passionAndLevel = finalRanges[skillRecord.def];
            skillRecord.levelInt = Rand.RangeInclusive(passionAndLevel.Min, passionAndLevel.Max);
            if (passionsModdedByGenes.TryGetValue(skillRecord.def, out var mod))
                switch (mod)
                {
                    case PassionMod.PassionModType.AddOneLevel:
                        skillRecord.passion = passionAndLevel.Passion == Passion.Major
                            ? Passion.Major
                            : passionAndLevel.Passion + 1;
                        break;
                    case PassionMod.PassionModType.DropAll:
                        skillRecord.passion = Passion.None;
                        break;
                    default:
                        skillRecord.passion = passionAndLevel.Passion;
                        break;
                }
            else
                skillRecord.passion = passionAndLevel.Passion;

            skillRecord.Notify_SkillDisablesChanged();
        }
    }

    /// <summary>
    /// Apply requested traits to the pawn. "Requested" is limited to what pawn generation would be capable
    /// of rolling for that given pawn (i.e. it has to respect backstory-forced traits and that said trait is
    /// taking up a slot that could be requested for another one).
    /// </summary>
    public static void ApplyRequestedTraitsTo(this List<TraitRequirement> traits, Pawn pawn)
    {
        List<Trait> conflictingTraits = new List<Trait>();
        foreach (var toAdd in traits)
        {
            var alreadyHave = toAdd.degree is int degree
                ? pawn.story.traits.HasTrait(toAdd.def, degree)
                : pawn.story.traits.HasTrait(toAdd.def);
            
            if (alreadyHave)
            {
                continue;
            }

            foreach (var ownedTrait in pawn.story.traits.allTraits)
            {
                bool conflict = toAdd.def.ConflictsWith(ownedTrait) || toAdd.def == ownedTrait.def;
                if (conflict && !TraitUtilities.IsBackstoryTraitOfPawn(ownedTrait, pawn))
                {
                    conflictingTraits.Add(ownedTrait);
                }
            }

            foreach (var trait in conflictingTraits)
            {
                pawn.story.traits.RemoveTrait(trait);
            }
            
            conflictingTraits.Clear();
            pawn.story.traits.GainTrait(new Trait(toAdd.def, toAdd.degree ?? 0));
        }

        TraitUtilities.FixTraitOverflow(pawn);
    }

    public static void RemoveBadHeDiffs(Pawn pawn)
    {
        var badThings = new List<Hediff>();
        foreach (var diff in pawn.health.hediffSet.hediffs)
        {
            if (diff.def.isBad && diff.def.everCurableByItem)
                badThings.Add(diff);
        }
        
        foreach (var bad in badThings)
            pawn.health.RemoveHediff(bad);
    }
}