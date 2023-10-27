using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs
{
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
        private const float CardSizeY = 480;
        private const float WindowMargin = 34f;
        
        private static Lazy<Texture2D> Minor = "UI/Icons/PassionMinor".AsTexture();
        private static Lazy<Texture2D> Major = "UI/Icons/PassionMajor".AsTexture();
        private static Lazy<Texture2D> Usable = "UI/Widgets/CheckOn".AsTexture();
        private static Lazy<Texture2D> CanBeOff = "UI/Widgets/CheckPartial".AsTexture();
        
        private readonly List<(SkillDef Skill, UsabilityRequirement Usability)> reqs;
        
        private Pawn pawn;
        
        private bool renderClothes = true;
        private bool renderHeadgear = true;

        private float listScrollViewHeight;
        private Vector2 listScrollPosition;

        public override Vector2 InitialSize => new Vector2(CardSizeX + WindowMargin, CardSizeY + WindowMargin);

        public EditSpecificPawn(Pawn pawn)
        {
            ProcGen.LockedPawns.Add(pawn);
            this.pawn = pawn;
            closeOnClickedOutside = true;
            
            var pawnIndex = StartingPawnUtility.PawnIndex(pawn);
            reqs = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
            if (ProcGen.LastResults?.Count > pawnIndex - 1
                && ProcGen.LastResults[pawnIndex] is { } existing)
            {
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
            }
            else
            {
                foreach (var skill in pawn.skills.skills)
                {
                    UsabilityRequirement req = skill.PermanentlyDisabled
                        ? UsabilityRequirement.CanBeOff
                        : skill.passion == Passion.Major
                            ? UsabilityRequirement.Major
                            : skill.passion == Passion.Minor
                                ? UsabilityRequirement.Minor
                                : UsabilityRequirement.Usable;
                    reqs.Add((skill.def, req));
                }
            }
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            if (pawn is null || pawn.Destroyed || pawn.Discarded)
            {
                this.Close();
                return;
            }
            DrawPortraitArea(inRect);
            DrawButtons(inRect);
        }

        public void Randomize()
        {
            var dict = new Dictionary<SkillDef, int>();
            var requiredSkills = new List<SkillDef>();
            var requiredWorkTags = WorkTags.None;
            foreach (var (skill, usability) in reqs)
            {
                switch (usability)
                {
                    case UsabilityRequirement.Major:
                        dict[skill] = 20 * ProcGen.JobVariation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    case UsabilityRequirement.Minor:
                        dict[skill] = 10 * ProcGen.JobVariation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    case UsabilityRequirement.Usable:
                        dict[skill] = 5 * ProcGen.JobVariation.RandomInRange;
                        requiredWorkTags |= skill.disablingWorkTags;
                        requiredSkills.Add(skill);
                        break;
                    default:
                        dict[skill] = 0;
                        break;
                }
            }

            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (requiredSkills.Any(requiredSkills.Contains))
                {
                    requiredWorkTags |= workType.workTags;
                }
            }
            
            string backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
            var collectSpecificPassions = new CollectSpecificPassions(dict, requiredWorkTags);
            var specifier = new SelectBackstorySpecifically(backstoryCategory);
            var bio = specifier.GetBestBio(collectSpecificPassions.Weight);
            var traits = bio.Traits;
            var empty = new List<TraitDef>();

            PawnBuilder builder = new PawnBuilder(bio);
            foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
            {
                if (usability == UsabilityRequirement.Major)
                    builder.TryLockInPassion(skill, Passion.Major);
                else if (usability == UsabilityRequirement.Minor)
                    builder.TryLockInPassion(skill, Passion.Minor);
            }

            bool addBackToLocked = false;
            if (ProcGen.LockedPawns.Contains(pawn))
            {
                ProcGen.LockedPawns.Remove(pawn);
                addBackToLocked = true;
            }

            using (NarrowBioEditor.MelaninRange(ProcGen.MelaninRange.min, ProcGen.MelaninRange.max))
            using (NarrowBioEditor.FilterPawnAges(ProcGen.AgeRange.min, ProcGen.AgeRange.max))
            using (NarrowBioEditor.RestrictTraits(traits, empty))
            {
                pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                ProcGen.OnPawnChanged(pawn);
                bio.ApplyTo(pawn);
                builder.Build().ApplyTo(pawn);
            }

            if (addBackToLocked)
            {
                ProcGen.LockedPawns.Add(pawn);
            }
        }

        private void DrawButtons(Rect rect)
        {
            const float inwardsX = 500, inwardsY = 135;
            const float buttonDimensions = 15f;
            const float offsetForSkills = 26.8f;
            
            var buttonRect = new Rect(rect.x + inwardsX, rect.y + inwardsY, buttonDimensions, buttonDimensions);
            
            for (int i = 0; i < reqs.Count; i++)
            {
                (SkillDef skill, UsabilityRequirement req) = reqs[i];
                Texture2D icon = GetIcon(req);
                GUI.DrawTexture(buttonRect, icon);
                if (Widgets.ButtonInvisible(buttonRect, icon))
                {
                    var newReq =
                        req == UsabilityRequirement.Major
                            ? UsabilityRequirement.CanBeOff
                            : req + 1;
                    reqs[i] = (skill, newReq);
                }
                
                buttonRect.y += offsetForSkills;
            }
        }

        private static Texture2D GetIcon(UsabilityRequirement req)
        {
            switch (req)
            {
                case UsabilityRequirement.Major:
                    return Major.Value;
                case UsabilityRequirement.Minor:
                    return Minor.Value;
                case UsabilityRequirement.Usable:
                    return Usable.Value;
                default:
                    return CanBeOff.Value;
            }
        }

        /// <summary>
        /// Mucking with <see cref="Page_ConfigureStartingPawns.DrawPortraitArea"/> and injecting my own UI elements.
        /// </summary>
        private void DrawPortraitArea(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            rect = rect.ContractedBy(WindowMargin / 2);

            // draw pawn portrait
            Rect pawnPortraitRect = new Rect(rect.center.x - Page_ConfigureStartingPawns.PawnPortraitSize.x / 2f,
                rect.yMin - 24f, Page_ConfigureStartingPawns.PawnPortraitSize.x,
                Page_ConfigureStartingPawns.PawnPortraitSize.y);
            Vector2 pawnPortraitSize = Page_ConfigureStartingPawns.PawnPortraitSize;
            Rot4 south = Rot4.South;
            Vector3 cameraOffset = new Vector3();
            bool renderClothes = this.renderClothes;
            int num1 = this.renderHeadgear ? 1 : 0;
            int num2 = renderClothes ? 1 : 0;
            Color? overrideHairColor = new Color?();
            PawnHealthState? healthStateOverride = new PawnHealthState?();
            RenderTexture image = PortraitsCache.Get(this.pawn, pawnPortraitSize, south, cameraOffset,
                renderHeadgear: (num1 != 0), renderClothes: (num2 != 0), overrideHairColor: overrideHairColor,
                stylingStation: true, healthStateOverride: healthStateOverride);
            GUI.DrawTexture(pawnPortraitRect, image);


            Rect rect1 = new Rect(rect.x, rect.y, 500, rect.height);
            CharacterCardUtility.DrawCharacterCard(rect1, this.pawn, this.Randomize, rect);
            int hasRelationships = SocialCardUtility.AnyRelations(this.pawn) ? 1 : 0;
            List<ThingDefCount> startingPossession = Find.GameInitData.startingPossessions[this.pawn];
            bool hasPossesions = startingPossession.Any<ThingDefCount>();
            int subTables = 1;
            if (hasRelationships != 0)
                ++subTables;
            if (hasPossesions)
                ++subTables;
            float height = (float) (rect.height - 100.0 - (4.0 * subTables - 1.0)) /
                           subTables;
            float y1 = rect.y;
            Rect rect2 = rect;
            rect2.yMin += 100f;
            rect2.xMin = rect1.xMax + 5f;
            rect2.height = height;
            if (!HealthCardUtility.AnyHediffsDisplayed(this.pawn, true))
                GUI.color = Color.gray;
            Widgets.Label(rect2, "Health".Translate().AsTipTitle());
            GUI.color = Color.white;
            rect2.yMin += 35f;
            HealthCardUtility.DrawHediffListing(rect2, this.pawn, true);
            float y2 = rect2.yMax + 4f;
            if (hasRelationships != 0)
            {
                Rect rect3 = new Rect(rect2.x, y2, rect2.width, height);
                Widgets.Label(rect3, "Relations".Translate().AsTipTitle());
                rect3.yMin += 35f;
                SocialCardUtility.DrawRelationsAndOpinions(rect3, this.pawn);
                y2 = rect3.yMax + 4f;
            }

            if (!hasPossesions)
                return;
            Rect rect4 = new Rect(rect2.x, y2, rect2.width, height);
            Widgets.Label(rect4, "Possessions".Translate().AsTipTitle());
            rect4.yMin += 35f;
            this.DrawPossessions(rect4, this.pawn, startingPossession);
        }

        private void DrawPossessions(Rect rect, Pawn selPawn, List<ThingDefCount> possessions)
        {
            GUI.BeginGroup(rect);
            Rect outRect = new Rect(0.0f, 0.0f, rect.width, rect.height);
            Rect viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, this.listScrollViewHeight);
            Rect rect1 = rect;
            if (viewRect.height > (double) outRect.height)
                rect1.width -= 16f;
            Widgets.BeginScrollView(outRect, ref this.listScrollPosition, viewRect);
            float y = 0.0f;
            Vector2 listScrollPosition1 = this.listScrollPosition;
            Vector2 listScrollPosition2 = this.listScrollPosition;
            double height = outRect.height;
            if (Find.GameInitData.startingPossessions.ContainsKey(selPawn))
            {
                for (int index = 0; index < possessions.Count; ++index)
                {
                    ThingDefCount possession = possessions[index];
                    Rect rect2 = new Rect(0.0f, y, Verse.Text.LineHeight, Verse.Text.LineHeight);
                    Widgets.DefIcon(rect2, possession.ThingDef);
                    Rect rect3 = new Rect(rect2.xMax + 17f, y,
                        (float) (rect.width - (double) rect2.width - 17.0 - 24.0), Verse.Text.LineHeight);
                    Widgets.Label(rect3, possession.LabelCap);
                    if (Mouse.IsOver(rect3))
                    {
                        Widgets.DrawHighlight(rect3);
                        TooltipHandler.TipRegion(rect3,
                            (TipSignal) (possession.ThingDef.LabelCap.ToString()
                                             .Colorize(ColoredText.TipSectionTitleColor) + "\n\n" +
                                         possession.ThingDef.description));
                    }

                    Widgets.InfoCardButton(rect3.xMax, y, possession.ThingDef);
                    y += Text.LineHeight;
                }
            }

            if (Event.current.type == EventType.Layout)
                this.listScrollViewHeight = y;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }
    }
}