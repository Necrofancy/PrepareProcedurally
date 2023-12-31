﻿using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

using static BackstorySolver;

public class AlienBackstorySolver
{
    public static IReadOnlyList<BackgroundPossibility> TryToSolveWith(AlienBalancingSituation situation,
        IntRange variation)
    {
        var currentBackgrounds = new List<BackgroundPossibility>(situation.Pawns);
        var categories = new List<string>(2);

        var weights = new Dictionary<SkillPassionSelection, int>();
        for (var i = currentBackgrounds.Count; i < situation.Pawns; i++)
        {
            // Evil static state here
            var existingPawn = Editor.StartingPawns[i];
            if (Editor.LockedPawns.Contains(existingPawn))
            {
                currentBackgrounds.Add(StaticBackgroundPossibility(existingPawn));
                continue;
            }

            foreach (var requirement in situation.SkillRequirements)
            {
                var moddedByVariation = requirement.GetWeights(currentBackgrounds) * variation.RandomInRange;
                weights[requirement] = moddedByVariation;
            }

            var age = Editor.StartingPawns[i].ageTracker.AgeBiologicalYearsFloat;
            categories.Clear();
            var category = situation.BackstoryCategories[i];
            categories.Add(category.ChildhoodCategory);
            categories.Add(category.AdulthoodCategory);

            var skillWeightingSystem = new SpecificSkillWeights(weights);
            var specifier = new SelectBackstorySpecifically(categories, Editor.GenderRequirements[i]);
            for (var j = 0; j < i; j++) specifier.AlreadyUsedBackstory(currentBackgrounds[j].Background);
            var bio = specifier.GetBestBio(skillWeightingSystem.Weight, Editor.TraitRequirements[i]);
            var skillRanges = EstimateRolling.PossibleSkillRangesOf(age, bio);
            currentBackgrounds.Add(new BackgroundPossibility(bio, skillRanges, age, true));
        }

        return currentBackgrounds;
    }
}