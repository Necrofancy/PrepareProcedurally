using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class Traits : PawnColumnWorker
    {
        private const string TraitLockedByBackstoryDescription = "SkillPassionTraitBackstory";
        private const string TraitLockedByPlayerChoiceDescription = "SkillPassionTraitPlayerChosen";
        
        private static List<TraitRequirement> traitOptions = new List<TraitRequirement>();
        
        private const int PaddingBetweenButtons = 4;
        const int AddButtonWidth = 30;
        
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            traitOptions.Clear();

            bool lockedPawn = ProcGen.LockedPawns.Contains(pawn);
            var needs = lockedPawn ? TraitUtilities.RequiredTraitsForLockedPawn(pawn) : TraitUtilities.RequiredTraitsForUnlockedPawn(pawn);
            traitOptions.AddRange(TraitUtilities.GetAvailableTraits(needs));
            
            bool allowPlusButton = traitOptions.Any();

            rect = rect.ContractedBy(PaddingBetweenButtons);

            float x = rect.x;
            float y = rect.y;
            float width;
            float height = rect.height;
            
            foreach (var trait in pawn.story.traits.allTraits)
            {
                width = Text.CalcSize(trait.LabelCap).x + 10f;
                var drawRect = new Rect(x, y, width, height);
                DrawTrait(drawRect, trait, pawn);
                x += width + PaddingBetweenButtons;
            }

            if (allowPlusButton && Widgets.ButtonText(new Rect(x, y, AddButtonWidth, height), "+"))
            {
                var options = new List<FloatMenuOption>();
                
                foreach (var option in traitOptions)
                {
                    var str = new Trait(option.def, option.degree ?? 0).LabelCap;
                    void Add()
                    {
                        if (lockedPawn)
                        {
                            AddTraitToLockedPawn(pawn, option);
                        }
                        else
                        {
                            AddTraitToUnlockedPawn(pawn, option);
                        }
                    }

                    options.Add(new FloatMenuOption(str, Add));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void AddTraitToLockedPawn(Pawn pawn, TraitRequirement option)
        {
            var index = StartingPawnUtility.PawnIndex(pawn);
            ProcGen.TraitRequirements[index].Add(option);

            foreach (var trait in pawn.story.traits.allTraits)
            {
                if (option.AllowsTrait(trait.def))
                {
                    pawn.story.traits.RemoveTrait(trait);
                    break;
                }
            }
            
            if (option.def.IsSexualityTrait())
            {
                pawn.story.traits.GainTrait(new Trait(option.def, option.degree ?? 0));
                return;
            }
            
            pawn.story.traits.GainTrait(new Trait(option.def, option.degree ?? 0));
            TraitUtilities.FixTraitOverflow(pawn);
        }

        private static void AddTraitToUnlockedPawn(Pawn pawn, TraitRequirement option)
        {
            var index = StartingPawnUtility.PawnIndex(pawn);
            ProcGen.TraitRequirements[index].Add(option);
            ProcGen.MakeDirty();
        }

        public override int GetMinWidth(PawnTable table)
        {
            float maxWidth = 0;
            foreach (var pawn in table.PawnsListForReading)
            {
                var width = pawn.story.traits.allTraits.Sum(trait => Text.CalcSize(trait.LabelCap).x + 10f + 2 * PaddingBetweenButtons);
                if (pawn.story.traits.allTraits.Count < 4)
                {
                    width += 2 * PaddingBetweenButtons + AddButtonWidth;
                }

                maxWidth = Math.Max(maxWidth, width);
            }

            return (int)maxWidth;
        }

        private static void DrawTrait(Rect rect, Trait trait, Pawn pawn)
        {
            Color color = GUI.color;
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = color;
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            GUI.color = GetColor(pawn, trait);
            Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height), trait.LabelCap);
            GUI.color = Color.white;
            if (!Mouse.IsOver(rect)) return;
            Trait trLocal = trait;
            TipSignal tip = new TipSignal(() => TraitDescriptionWithAdditionalTips(trait, pawn), (int) rect.y * 37);
            TooltipHandler.TipRegion(rect, tip);
            if (Widgets.ButtonInvisible(rect, doMouseoverSound: true))
            {
                int index = StartingPawnUtility.PawnIndex(pawn);
                var requiredTraits = ProcGen.TraitRequirements[index];
                foreach (var required in requiredTraits)
                {
                    if (trait.def == required.def && trait.Degree == required.degree)
                    {
                        requiredTraits.Remove(required);
                        break;
                    }
                }
            }
        }

        private static string TraitDescriptionWithAdditionalTips(Trait trait, Pawn pawn)
        {
            StringBuilder builder = new StringBuilder(trait.TipString(pawn));
            
            if (TraitUtilities.IsBackstoryTraitOfPawn(trait, pawn))
            {
                builder.AppendLine().AppendLine().AppendLine(TraitLockedByBackstoryDescription.Translate());
            }
            
            var index = StartingPawnUtility.PawnIndex(pawn);
            var forcedTraits = ProcGen.TraitRequirements[index];
            if (forcedTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
            {
                builder.AppendLine().AppendLine().AppendLine(TraitLockedByPlayerChoiceDescription.Translate());
            }

            return builder.ToString();
        }

        private static Color GetColor(Pawn pawn, Trait trait)
        {
            if (TraitUtilities.IsBackstoryTraitOfPawn(trait, pawn))
            {
                return ColorLibrary.Turquoise;
            }
            
            if (trait.Suppressed)
            {
                return ColoredText.SubtleGrayColor;
            }
            
            if (trait.sourceGene != null)
            {
                return ColoredText.GeneColor;
            }

            var index = StartingPawnUtility.PawnIndex(pawn);
            var forcedTraits = ProcGen.TraitRequirements[index];
            if (forcedTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
            {
                return ColoredText.ImpactColor;
            }

            return Color.white;
        }
    }
}