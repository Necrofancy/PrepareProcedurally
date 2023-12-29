using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public enum UsabilityRequirement
{
    CanBeOff,
    Usable,
    Minor,
    Major
}

public class EditSpecificPawn : Window
{
    // DrawCharacterCard with a defined Randomize button -must- be this size or bad things happen.
    private const float CardSizeX = 837.5f;
    private const float CardSizeY = 520;
    private const float WindowMargin = 34f;
    private const float YPadding = 30f;

    private static readonly Lazy<Texture2D> Minor = "UI/Icons/PassionMinor".AsTexture();
    private static readonly Lazy<Texture2D> Major = "UI/Icons/PassionMajor".AsTexture();
    private static readonly Lazy<Texture2D> Usable = "UI/Widgets/CheckOn".AsTexture();
    private static readonly Lazy<Texture2D> CanBeOff = "UI/Widgets/CheckPartial".AsTexture();

    private readonly List<(SkillDef Skill, UsabilityRequirement Usability)> reqs;

    private Pawn pawn;

    private readonly bool doRenderClothes = true;
    private readonly bool renderHeadgear = true;

    private float listScrollViewHeight;
    private Vector2 listScrollPosition;

    public override Vector2 InitialSize => new(CardSizeX + WindowMargin, CardSizeY + WindowMargin + YPadding);

    public EditSpecificPawn(Pawn pawn)
    {
        Editor.LockedPawns.Add(pawn);
        this.pawn = pawn;
        doCloseX = true;

        var pawnIndex = StartingPawnUtility.PawnIndex(pawn);
        reqs = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
        if (Editor.LastResults?.Count > pawnIndex - 1
            && Editor.LastResults[pawnIndex] is { } existing)
            foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                var result = existing.FinalRanges[skill];
                var usability = result.Passion switch
                {
                    Passion.Major => UsabilityRequirement.Major,
                    Passion.Minor => UsabilityRequirement.Minor,
                    _ => UsabilityRequirement.CanBeOff
                };
                reqs.Add((skill, usability));
            }
        else
            foreach (var skill in pawn.skills.skills)
            {
                var req = skill.PermanentlyDisabled
                    ? UsabilityRequirement.CanBeOff
                    : skill.passion == Passion.Major
                        ? UsabilityRequirement.Major
                        : skill.passion == Passion.Minor
                            ? UsabilityRequirement.Minor
                            : UsabilityRequirement.Usable;
                reqs.Add((skill.def, req));
            }
    }

    public override void DoWindowContents(Rect rect)
    {
        if (pawn is null || pawn.Destroyed || pawn.Discarded)
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        var titleRect = new Rect(rect.x, rect.y, rect.width, YPadding);
        Widgets.Label(titleRect, "CustomizePawnTitle".Translate());
        Text.Font = GameFont.Small;

        rect.y += YPadding;
        rect.height -= YPadding;

        DrawPortraitArea(rect);
        DrawButtons(rect);
    }

    private void Randomize()
    {
        var dict = new Dictionary<SkillDef, int>();
        var requiredSkills = new List<SkillDef>();
        var requiredWorkTags = WorkTags.None;

        var variation = new IntRange(10, (int)(Editor.SkillWeightVariation * 10));
        foreach (var (skill, usability) in reqs)
            switch (usability)
            {
                case UsabilityRequirement.Major:
                    dict[skill] = 20 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Minor:
                    dict[skill] = 10 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Usable:
                    dict[skill] = 5 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                default:
                    dict[skill] = -5 * variation.RandomInRange;
                    break;
            }

        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (requiredSkills.Any(requiredSkills.Contains))
                requiredWorkTags |= workType.workTags;

        var collectSpecificPassions = new CollectSpecificPassions(dict, requiredWorkTags);

        pawn = Compatibility.Layer.RandomizeSingularPawn(pawn, collectSpecificPassions, reqs);
    }

    private void DrawButtons(Rect rect)
    {
        const float inwardsX = 497, inwardsY = 147;
        const float buttonDimensions = 24f;
        const float offsetForSkills = 27f;

        var buttonRect = new Rect(rect.x + inwardsX, rect.y + inwardsY, buttonDimensions, buttonDimensions);

        for (var i = 0; i < reqs.Count; i++)
        {
            var (skill, req) = reqs[i];
            var icon = GetIcon(req);
            GUI.DrawTexture(buttonRect, icon);
            if (Mouse.IsOver(buttonRect))
            {
                TooltipHandler.TipRegion(buttonRect, (TipSignal)GetLabel(req, skill));
                Widgets.DrawHighlight(buttonRect);
            }

            if (Widgets.ButtonInvisible(buttonRect, icon))
            {
                var passionPoints = GetPassionPoints();
                var remainingPoints = Editor.MaxPassionPoints - passionPoints;
                var canBumpUp = CanIncreaseRequirement(req, remainingPoints);

                if (ModsConfig.BiotechActive
                    && StartingPawnUtilityState.GetStartingPawnRequestList() is { } pawnGenerationRequests)
                {
                    var index = StartingPawnUtility.PawnIndex(pawn);
                    var request = pawnGenerationRequests[index];
                    foreach (var gene in request.ForcedXenotype.genes)
                        if (gene.passionMod?.modType == PassionMod.PassionModType.DropAll &&
                            gene.passionMod.skill == skill)
                            reqs[i] = (skill, UsabilityRequirement.CanBeOff);
                }

                var newReq = canBumpUp ? req + 1 : UsabilityRequirement.CanBeOff;
                reqs[i] = (skill, newReq);
            }

            buttonRect.y += offsetForSkills;
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

    private static string GetLabel(UsabilityRequirement req, SkillDef def)
    {
        return req switch
        {
            UsabilityRequirement.Major => "CustomizePawnMajorPassion".Translate(def.label),
            UsabilityRequirement.Minor => "CustomizePawnMinorPassion".Translate(def.label),
            UsabilityRequirement.Usable => "CustomizePawnUsable".Translate(def.label),
            _ => "CustomizePawnNoWeight".Translate(def.label)
        };
    }

    private float GetPassionPoints()
    {
        var sum = 0.0f;
        foreach (var item in reqs)
            switch (item.Usability)
            {
                case UsabilityRequirement.Major:
                    sum += 1.5f;
                    break;
                case UsabilityRequirement.Minor:
                    sum += 1.0f;
                    break;
            }

        return sum;
    }

    /// <summary>
    /// Mucking with <see cref="Page_ConfigureStartingPawns.DrawPortraitArea"/> and injecting my own UI elements.
    /// </summary>
    private void DrawPortraitArea(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        rect = rect.ContractedBy(WindowMargin / 2, WindowMargin);

        // draw pawn portrait
        var pawnPortraitRect = new Rect(rect.center.x - Page_ConfigureStartingPawns.PawnPortraitSize.x / 2f,
            rect.yMin - 24f, Page_ConfigureStartingPawns.PawnPortraitSize.x,
            Page_ConfigureStartingPawns.PawnPortraitSize.y);
        var pawnPortraitSize = Page_ConfigureStartingPawns.PawnPortraitSize;
        var south = Rot4.South;
        var cameraOffset = new Vector3();
        var renderClothes = doRenderClothes;
        var num1 = renderHeadgear ? 1 : 0;
        var num2 = renderClothes ? 1 : 0;
        var image = PortraitsCache.Get(pawn, pawnPortraitSize, south, cameraOffset,
            renderHeadgear: num1 != 0, renderClothes: num2 != 0, stylingStation: true);
        GUI.DrawTexture(pawnPortraitRect, image);


        var rect1 = new Rect(rect.x, rect.y, 500, rect.height);
        CharacterCardUtility.DrawCharacterCard(rect1, pawn, Randomize, rect);
        var hasRelationships = SocialCardUtility.AnyRelations(pawn) ? 1 : 0;
        var startingPossession = Find.GameInitData.startingPossessions[pawn];
        var hasPossesions = startingPossession.Any();
        var subTables = 1;
        if (hasRelationships != 0)
            ++subTables;
        if (hasPossesions)
            ++subTables;
        var height = (float)(rect.height - 100.0 - (4.0 * subTables - 1.0)) /
                     subTables;
        var rect2 = rect;
        rect2.yMin += 100f;
        rect2.xMin = rect1.xMax + 5f;
        rect2.height = height;
        if (!HealthCardUtility.AnyHediffsDisplayed(pawn, true))
            GUI.color = Color.gray;
        Widgets.Label(rect2, "Health".Translate().AsTipTitle());
        GUI.color = Color.white;
        rect2.yMin += 35f;
        HealthCardUtility.DrawHediffListing(rect2, pawn, true);
        var y2 = rect2.yMax + 4f;
        if (hasRelationships != 0)
        {
            var rect3 = new Rect(rect2.x, y2, rect2.width, height);
            Widgets.Label(rect3, "Relations".Translate().AsTipTitle());
            rect3.yMin += 35f;
            SocialCardUtility.DrawRelationsAndOpinions(rect3, pawn);
            y2 = rect3.yMax + 4f;
        }

        if (!hasPossesions)
            return;
        var rect4 = new Rect(rect2.x, y2, rect2.width, height);
        Widgets.Label(rect4, "Possessions".Translate().AsTipTitle());
        rect4.yMin += 35f;
        DrawPossessions(rect4, pawn, startingPossession);
    }

    private void DrawPossessions(Rect rect, Pawn selPawn, List<ThingDefCount> possessions)
    {
        GUI.BeginGroup(rect);
        var outRect = new Rect(0.0f, 0.0f, rect.width, rect.height);
        var viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, listScrollViewHeight);
        var rect1 = rect;
        if (viewRect.height > (double)outRect.height)
            rect1.width -= 16f;
        Widgets.BeginScrollView(outRect, ref listScrollPosition, viewRect);
        var y = 0.0f;
        if (Find.GameInitData.startingPossessions.ContainsKey(selPawn))
            foreach (var possession in possessions)
            {
                var rect2 = new Rect(0.0f, y, Text.LineHeight, Text.LineHeight);
                Widgets.DefIcon(rect2, possession.ThingDef);
                var rect3 = new Rect(rect2.xMax + 17f, y,
                    (float)(rect.width - (double)rect2.width - 17.0 - 24.0), Text.LineHeight);
                Widgets.Label(rect3, possession.LabelCap);
                if (Mouse.IsOver(rect3))
                {
                    Widgets.DrawHighlight(rect3);
                    TooltipHandler.TipRegion(rect3,
                        (TipSignal)(possession.ThingDef.LabelCap.ToString()
                                        .Colorize(ColoredText.TipSectionTitleColor) + "\n\n" +
                                    possession.ThingDef.description));
                }

                Widgets.InfoCardButton(rect3.xMax, y, possession.ThingDef);
                y += Text.LineHeight;
            }

        if (Event.current.type == EventType.Layout)
            listScrollViewHeight = y;
        Widgets.EndScrollView();
        GUI.EndGroup();
    }
}