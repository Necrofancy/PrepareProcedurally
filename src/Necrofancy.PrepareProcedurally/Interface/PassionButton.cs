using System;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface;

public static class PassionButton
{
    private static readonly Lazy<Texture2D> Minor = "UI/Icons/PassionMinor".AsTexture();
    private static readonly Lazy<Texture2D> Major = "UI/Icons/PassionMajor".AsTexture();
    private static readonly Lazy<Texture2D> Usable = "UI/Widgets/CheckOn".AsTexture();
    private static readonly Lazy<Texture2D> CanBeOff = "UI/Widgets/CheckPartial".AsTexture();
    
    public static void DrawSkillRequirementIcon(float x, float y, SkillDef skill)
        {
            if (SelectedPawn.Pawn == null) return;

            // Values ripped (lovingly) from SkillUI.DrawSkill
            float buttonSpacing = 240f;
            float buttonSize = 24f;

            x += buttonSpacing - 0.5f * buttonSize;
            Rect buttonRect = new Rect(x, y, buttonSize, buttonSize);

            int idx = SelectedPawn.Requirements.FindIndex(requirement => requirement.Skill == skill);
            if (idx == -1) return;

            var (_, req) = SelectedPawn.Requirements[idx];
            var icon = GetIcon(req); 

            GUI.DrawTexture(buttonRect, icon);
            
            if (Mouse.IsOver(buttonRect))
            {
                TooltipHandler.TipRegion(buttonRect, (TipSignal)GetLabel(req, skill));
                Widgets.DrawHighlight(buttonRect);
            }

            if (Widgets.ButtonInvisible(buttonRect))
            {
                var passionPoints = SelectedPawn.GetPassionPoints();
                var remainingPoints = Editor.MaxPassionPoints - passionPoints;
                var canBumpUp = CanIncreaseRequirement(req, remainingPoints);

                if (ModsConfig.BiotechActive && StartingPawnUtilityState.GetStartingPawnRequestList() is { } pawnGenerationRequests)
                {
                    var request = pawnGenerationRequests[SelectedPawn.Index];
                    foreach (var gene in request.ForcedXenotype.genes)
                    {
                        if (gene.passionMod?.modType == PassionMod.PassionModType.DropAll && gene.passionMod.skill == skill)
                        {
                            SelectedPawn.Requirements[idx] = (skill, UsabilityRequirement.CanBeOff);
                            return;
                        }
                    }
                }

                var newReq = canBumpUp ? req + 1 : UsabilityRequirement.CanBeOff;
                SelectedPawn.Requirements[idx] = (skill, newReq);
            }
        }
    
    private static bool CanIncreaseRequirement(UsabilityRequirement req, float remainingPoints)
    {
        switch (req)
        {
            case UsabilityRequirement.Major:
                return false;
            case UsabilityRequirement.Minor:
                // cost of bumping up from minor to major passion
                return remainingPoints > 0.5f;
            case UsabilityRequirement.Usable:
                // cost of bumping up from no passion to minor passion
                return remainingPoints > 1.0f;
            case UsabilityRequirement.CanBeOff:
                return true;
            default:
                return false;
        }
    }

    private static string GetLabel(UsabilityRequirement req, SkillDef def)
    {
        return req switch
        {
            UsabilityRequirement.Major => "Necrofancy.PrepareProcedurally.MajorPassionTooltip".Translate(def.label),
            UsabilityRequirement.Minor => "Necrofancy.PrepareProcedurally.MinorPassionTooltip".Translate(def.label),
            UsabilityRequirement.Usable => "Necrofancy.PrepareProcedurally.UsableTooltip".Translate(def.label),
            _ => "Necrofancy.PrepareProcedurally.NoEmphasisTooltip".Translate(def.label)
        };
    }
    
    private static Texture2D GetIcon(UsabilityRequirement req)
    {
        return req switch
        {
            UsabilityRequirement.Major => Major.Value,
            UsabilityRequirement.Minor => Minor.Value,
            UsabilityRequirement.Usable => Usable.Value,
            _ => CanBeOff.Value
        };
    }
}