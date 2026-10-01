#if !RW1_4 && !RW1_5

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public abstract class BackstorySearch : Dialog_Search<BackstoryDef>
{
    private static readonly char[] Separators = [',', '，', '、', '،', ';', '；'];
    private static readonly Vector2 CreationOffset = new (0, 40);

    private readonly Pawn referencePawn;
    private bool workflowEnded;
    private readonly HashSet<string> listedBackstories = new();
    private readonly Vector2 position;

    protected BackstorySearch(Pawn referencePawn, List<BackstoryDef> backstories)
    {
        this.referencePawn = referencePawn;
        position = UI.MousePositionOnUIInverted + CreationOffset;
        searchResults = new SortedList<string, BackstoryDef>(StringComparer.InvariantCultureIgnoreCase);
        SearchSet = backstories;
    }

    private static WorkTags ExpectedSearchTraits => 
        WorkTags.ManualDumb 
        | WorkTags.ManualSkilled 
        | WorkTags.Violent                                                    
        | WorkTags.Caring
        | WorkTags.Cleaning 
        | WorkTags.Hauling 
        | WorkTags.Firefighting;

    private static List<WorkTypeDef> ExpectedSearchWorkTypes
    {
        get
        {
            return field ??=
            [
                WorkTypeDefOf.Hunting,
                WorkTypeDefOf.Firefighter,
                WorkTypeDefOf.Doctor,
                WorkTypeDefOf.Warden,
                WorkTypeDefOf.Research,
                WorkTypeDefOf.Childcare,
            ];
        }
    }
    
    protected override List<BackstoryDef> SearchSet { get; }

    protected override bool ShouldClose => workflowEnded;

    protected override TaggedString SearchLabel => "Necrofancy.PrepareProcedurally.SearchBackstory".Translate();
    
    protected abstract void BackstorySelected(Pawn referencePawn, BackstoryDef element);

    protected override void TryAddElement(BackstoryDef def)
    {
        if (listedBackstories.Add(def.defName) && BackstoryMatch(def))
        {
            searchResults.Add(def.defName, def);
            // looks strange in this context, but this is using Dialog_Search<T>'s SetInitialSizeAndPosition
            // and that is overriding off of the dialog implementation which assumed static window size.
            SetInitialSizeAndPosition();
        }
    }

    protected override void TryRemoveElement(BackstoryDef def)
    {
        searchResults.Remove(def.defName);
        listedBackstories.Remove(def.defName);
    }

    protected override void DoIcon(BackstoryDef element, Rect iconRect)
    {
        // no operation needed
    }

    protected override void SetInitialSizeAndPosition()
    {
        scrollHeight = searchResults.Count * 26f;
        Vector2 size = InitialSize;
        size.y = Mathf.Clamp(size.y + scrollHeight, InitialSize.y, UI.screenHeight - position.y);

        var centeredX = position.x - size.x / 2;
        var centeredY = position.y - size.y / 2;

        windowRect = new Rect(centeredX, centeredY, size.x, size.y).Rounded();
    }

    protected override void DoLabel(BackstoryDef element, Rect labelRect)
    {
        var text = element.titleFemale is not null
            ? $"{element.TitleCapFor(Gender.Male)} / {element.TitleCapFor(Gender.Female)}"
            : element.TitleCapFor(Gender.Male);

        Widgets.Label(labelRect, text);
        TooltipHandler.TipRegion(labelRect, element.FullDescriptionFor(referencePawn));
    }

    protected override bool ShouldSkipElement(BackstoryDef element)
    {
        return false;
    }

    protected override void OnHighlightUpdate(BackstoryDef element)
    {
        // no operation needed
    }

    public override void Notify_CommonSearchChanged()
    {
        base.Notify_CommonSearchChanged();
        listedBackstories.Clear();
    }

    public override void Notify_ClickOutsideWindow()
    {
        workflowEnded = true;
    }

    // cliked
    protected override void ClikedOnElement(BackstoryDef element)
    {
        BackstorySelected(referencePawn, element);
        workflowEnded = true;
    }

    private bool BackstoryMatch(BackstoryDef backstoryDef)
    {
        var search = CommonSearchWidget.filter.Text;
        var split = search.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (var searchPart in split)
        {
            if (!MatchesAgainst(searchPart.Trim(), backstoryDef))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesAgainst(string search, BackstoryDef backstoryDef)
    {
        return
                  // disqualify invalidating matches
                  !MatchAgainstWorkTags(search, backstoryDef.workDisables)
               && !MatchesWorkTypeDefGerund(search, backstoryDef.DisabledWorkTypes)
               && !MatchestTraitList(search, backstoryDef.disallowedTraits)
                  && 
                  (
                      // qualify direct matches
                      SearchMatch(search, backstoryDef.TitleFor(Gender.Male))
                   || SearchMatch(search, backstoryDef.TitleFor(Gender.Female))
                   || SearchMatch(search, backstoryDef.TitleShortFor(Gender.Male))
                   || SearchMatch(search, backstoryDef.TitleShortFor(Gender.Female))
                   || MatchesASkillBonus(search, backstoryDef)
                   || MatchestTraitList(search, backstoryDef.forcedTraits)
                      // qualify against generic work types
                   || MatchAgainstWorkTags(search, ExpectedSearchTraits) 
                   || MatchesWorkTypeDefGerund(search, ExpectedSearchWorkTypes)
                  );
    }

    private static bool MatchesASkillBonus(string search, BackstoryDef backstoryDef)
    {
        if (backstoryDef.skillGains is not null)
        {
            foreach (var gain in backstoryDef.skillGains)
            {
                if (gain is null)
                    continue;
                if (gain.amount > 0 && SearchMatch(search, gain.skill.LabelCap))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchestTraitList(string search, List<BackstoryTrait> traitList)
    {
        if (traitList is not null)
        {
            foreach (var backstoryTrait in traitList)
            {
                var traitDef = backstoryTrait.def;
                var degree = backstoryTrait.degree;
                var degreeData = traitDef.DataAtDegree(degree);
                var traitLabel = degreeData?.label;
                if (SearchMatch(search, traitLabel))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesWorkTypeDefGerund(string search, IReadOnlyCollection<WorkTypeDef> workTypes)
    {
        foreach (var workType in workTypes)
        {
            if (workType is null) 
                continue;
            
            if (SearchMatch(search,workType.gerundLabel))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchAgainstWorkTags(string search, WorkTags workTagsWhereSearchIsExpected)
    {
        foreach (WorkTags tag in workTagsWhereSearchIsExpected.GetAllSelectedItems<WorkTags>())
        {
            if (tag == WorkTags.None)
                continue;

            if (SearchMatch(search, tag.LabelTranslated()))
                return true;
        }

        return false;
    }

    private static bool SearchMatch(string searchFor, string withinText)
    {
        return !withinText.NullOrEmpty() && withinText.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }
}

#endif