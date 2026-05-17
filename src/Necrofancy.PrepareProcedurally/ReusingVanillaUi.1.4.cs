#if RW1_4

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public static partial class ReusingVanillaUi
{
    private static float listScrollViewHeight;
    private static Vector2 listScrollPosition;

    public static void DrawPortraitArea(Rect rect, int pawnIndex, bool renderClothes, bool renderHeadgear)
    {
        int tracerBullet = 0;
        try
        {
            var currentPawn = Editor.StartingPawns[pawnIndex];
            Widgets.DrawMenuSection(rect);
            rect = rect.ContractedBy(17f);
            Rect position = new Rect(rect.center.x - Page_ConfigureStartingPawns.PawnPortraitSize.x / 2f, rect.yMin - 24f, Page_ConfigureStartingPawns.PawnPortraitSize.x, Page_ConfigureStartingPawns.PawnPortraitSize.y);
            Pawn curPawn = currentPawn;
            Vector2 pawnPortraitSize = Page_ConfigureStartingPawns.PawnPortraitSize;
            Rot4 south = Rot4.South;
            Vector3 cameraOffset = new Vector3();
            int num1 = renderHeadgear ? 1 : 0;
            int num2 = renderClothes ? 1 : 0;
            
            tracerBullet++; //1
            RenderTexture image = PortraitsCache.Get(curPawn, pawnPortraitSize, south, cameraOffset, renderHeadgear: num1 != 0, renderClothes: num2 != 0, stylingStation: true);
            GUI.DrawTexture(position, image);
            
            Rect rect1 = rect with { width = 500f };
            CharacterCardUtility.DrawCharacterCard(rect1, currentPawn, SelectedPawn.Randomize, rect);
            int num3 = SocialCardUtility.AnyRelations(currentPawn) ? 1 : 0;

            tracerBullet++;
            if (!Find.GameInitData.startingPossessions.TryGetValue(currentPawn, out var startingPossession))
            {
                startingPossession = new List<ThingDefCount>();
            }
            bool flag = startingPossession.Any();
            int num4 = 1;
            if (num3 != 0)
              ++num4;
            if (flag)
              ++num4;
            float height = (float) (rect.height - 100.0 - (4.0 * num4 - 1.0)) / num4;
            Rect rect2 = rect;
            rect2.yMin += 100f;
            rect2.xMin = rect1.xMax + 5f;
            rect2.height = height;
            tracerBullet++; //3
            if (!HealthCardUtility.AnyHediffsDisplayed(currentPawn, true))
              GUI.color = Color.gray;
            Widgets.Label(rect2, "Health".Translate().AsTipTitle());
            GUI.color = Color.white;
            rect2.yMin += 35f;
            HealthCardUtility.DrawHediffListing(rect2, currentPawn, true);
            float y2 = rect2.yMax + 4f;
            if (num3 != 0)
            {
              Rect rect3 = new Rect(rect2.x, y2, rect2.width, height);
              Widgets.Label(rect3, "Relations".Translate().AsTipTitle());
              rect3.yMin += 35f;
              SocialCardUtility.DrawRelationsAndOpinions(rect3, currentPawn);
              y2 = rect3.yMax + 4f;
            }
            tracerBullet++; //4
            if (!flag)
              return;
            Rect rect4 = new Rect(rect2.x, y2, rect2.width, height);
            Widgets.Label(rect4, "Possessions".Translate().AsTipTitle());
            rect4.yMin += 35f;
            DrawPossessions(rect4, currentPawn);
        }
        catch (Exception e)
        {
            Logging.ErrorOnce($"Tracer Bullet is at {tracerBullet}");
            Logging.Error(e.ToString());
        }
    }
    
    private static void DrawPossessions(Rect rect, Pawn pawn)
    {
        GUI.BeginGroup(rect);
        Rect outRect = new Rect(0.0f, 0.0f, rect.width, rect.height);
        Rect viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, listScrollViewHeight);
        Rect rect1 = rect;
        if (viewRect.height > (double) outRect.height)
            rect1.width -= 16f;
        Widgets.BeginScrollView(outRect, ref listScrollPosition, viewRect);
        float y = 0.0f;
        if (Find.GameInitData.startingPossessions.TryGetValue(pawn, out var possessions))
        {
            for (int index = 0; index < possessions.Count; ++index)
            {
                ThingDefCount possession = possessions[index];
                Rect rect2 = new Rect(0.0f, y, Text.LineHeight, Text.LineHeight);
                Widgets.DefIcon(rect2, possession.ThingDef);
                Rect rect3 = new Rect(rect2.xMax + 17f, y, (float) (rect.width - (double) rect2.width - 17.0 - 24.0), Text.LineHeight);
                Widgets.Label(rect3, possession.LabelCap);
                if (Mouse.IsOver(rect3))
                {
                    Widgets.DrawHighlight(rect3);
                    TooltipHandler.TipRegion(rect3, (TipSignal) $"{possession.ThingDef.LabelCap.ToString().Colorize(ColoredText.TipSectionTitleColor)}\n\n{possession.ThingDef.description}");
                }
                Widgets.InfoCardButton(rect3.xMax, y, possession.ThingDef);
                y += Text.LineHeight;
            }
        }
        if (Event.current.type == EventType.Layout)
            listScrollViewHeight = y;
        Widgets.EndScrollView();
        GUI.EndGroup();
    }
}

#endif