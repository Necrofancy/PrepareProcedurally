#if SEARCH_MENU_AVAILABLE

using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public class BackstorySearch : Dialog_Search<BackstoryOption>
{
    private static readonly char[] Separators = [',', '，', '、', '،', ';', '；'];
    private static readonly Vector2 CreationOffset = new (0, 40);

    private readonly Pawn referencePawn;
    private readonly Action onSelect;
    private readonly BackstorySlot slot;
    private readonly BackstoryDef existing;
    private bool workflowEnded;
    private readonly HashSet<string> listedBackstories = new();
    private readonly Vector2 position;
    
    private const float RemoveRowHeight = 30f;

    public BackstorySearch(Pawn referencePawn, BackstorySlot slot, BackstoryDef existing, List<BackstoryOption> backstories, Action onSelect)
    {
        this.referencePawn = referencePawn;
        this.slot = slot;
        this.existing = existing;
        this.onSelect = onSelect;
        position = UI.MousePositionOnUIInverted + CreationOffset;
        searchResults = new SortedList<string, BackstoryOption>(StringComparer.InvariantCultureIgnoreCase);
        SearchSet = backstories;
    }
    
    private bool ShowRemove => existing is not null && !searchResults.Any();

    private static WorkTags ExpectedSearchWorkTags => 
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
    
    protected override List<BackstoryOption> SearchSet { get; }

    protected override bool ShouldClose => workflowEnded;

    protected override TaggedString SearchLabel => "Necrofancy.PrepareProcedurally.SearchBackstory".Translate();

    public override void DoWindowContents(Rect inRect)
    {
        if (ShowRemove)
        {
            var removeRect = new Rect(inRect.x, inRect.y, inRect.width, RemoveRowHeight);
            if (Widgets.ButtonText(removeRect, "Remove".Translate()))
            {
                BackstoryOption.Remove(StartingPawnUtility.PawnIndex(referencePawn), slot, onSelect);
                workflowEnded = true;
            }
            inRect.yMin += RemoveRowHeight + 4f;
        }
        
        base.DoWindowContents(inRect);
    }

    protected override void TryAddElement(BackstoryOption option)
    {
        var key = LookupString(option);
        if (listedBackstories.Add(key) && BackstoryOptionMatches(option))
        {
            searchResults.Add(key, option);
            // looks strange in this context, but this is using Dialog_Search<T>'s SetInitialSizeAndPosition
            // and that is overriding off of the dialog implementation which assumed static window size.
            SetInitialSizeAndPosition();
        }
    }

    protected override void TryRemoveElement(BackstoryOption option)
    {
        searchResults.Remove(LookupString(option));
        listedBackstories.Remove(LookupString(option));
    }

    protected override void DoIcon(BackstoryOption element, Rect iconRect)
    {
        // no operation needed
    }

    protected override void SetInitialSizeAndPosition()
    {
        scrollHeight = searchResults.Count * 26f;

        Vector2 size = InitialSize;
        size.y = Mathf.Clamp(size.y + scrollHeight, InitialSize.y, UI.screenHeight - position.y);
        size.y += ShowRemove ? RemoveRowHeight + 4f : 0f;
        
        var centeredX = position.x - size.x / 2;
        var centeredY = position.y - size.y / 2;
        
        windowRect = new Rect(centeredX, centeredY, size.x, size.y).Rounded();
    }

    protected override void DoLabel(BackstoryOption element, Rect labelRect)
    {
        element.DrawLabel(labelRect, referencePawn);
    }

    protected override bool ShouldSkipElement(BackstoryOption option)
    {
        return false;
    }

    protected override void OnHighlightUpdate(BackstoryOption option)
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
    protected override void ClikedOnElement(BackstoryOption option)
    {
        int index = StartingPawnUtility.PawnIndex(referencePawn);
        if (index >= 0)
        {
            option.Apply(index, onSelect);
        }
        workflowEnded = true;
    }
    
    private bool BackstoryOptionMatches(BackstoryOption option)
    {
        return option.ForcedBio is not null
            ? SolidBioMatch(option.ForcedBio)
            : BackstoryMatch(option.Def);
    }
    
    private bool SolidBioMatch(PawnBio bio)
    {
        var search = CommonSearchWidget.filter.Text;
        var split = search.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (var searchPart in split)
        {
            if (!MatchesAgainst(searchPart.Trim(), bio))
            {
                return false;
            }
        }

        return true;
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

    private static bool MatchesAgainst(string search, PawnBio bio)
    {
        // disqualifiers disqualify both, whereas searching positively may refer to either childhood or adulthood
        return !(MatchesADisablingFactor(search, bio.childhood) || MatchesADisablingFactor(search, bio.adulthood))
               && (MatchesADirectSearch(search, bio.childhood) 
                   || MatchesADirectSearch(search, bio.adulthood) 
                   || MatchesFallbackGenericWorkTypes(search)
               );
    }

    private static bool MatchesAgainst(string search, BackstoryDef backstoryDef)
    {
        return
            !MatchesADisablingFactor(search, backstoryDef)
            && (MatchesADirectSearch(search, backstoryDef) || MatchesFallbackGenericWorkTypes(search));
    }
    
    private static bool MatchesADisablingFactor(string search, BackstoryDef backstoryDef)
    {
        return MatchAgainstWorkTags(search, backstoryDef.workDisables)
               || MatchesWorkTypeDefGerund(search, backstoryDef.DisabledWorkTypes)
               || MatchestTraitList(search, backstoryDef.disallowedTraits);
    }

    private static bool MatchesADirectSearch(string search, BackstoryDef backstoryDef)
    {
        return SearchMatch(search, backstoryDef.TitleFor(Gender.Male))
               || SearchMatch(search, backstoryDef.TitleFor(Gender.Female))
               || SearchMatch(search, backstoryDef.TitleShortFor(Gender.Male))
               || SearchMatch(search, backstoryDef.TitleShortFor(Gender.Female))
               || MatchesASkillBonus(search, backstoryDef)
               || MatchestTraitList(search, backstoryDef.forcedTraits);
    }
    
    private static bool MatchesFallbackGenericWorkTypes(string search)
    {
        return MatchAgainstWorkTags(search, ExpectedSearchWorkTags)
               || MatchesWorkTypeDefGerund(search, ExpectedSearchWorkTypes);
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

    private static string LookupString(BackstoryOption option)
    {
        return option.ForcedBio is null
            ? $"Backstory_{option.Def.defName}" // deliberately sorted first
            : $"Bio_{option.ForcedBio.name.ToStringFull}_{option.ForcedBio.childhood.defName}";
    }
}

#endif