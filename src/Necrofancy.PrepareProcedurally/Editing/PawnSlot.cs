using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Editing;

public sealed class PawnSlot(EditorContext context, int index)
{
    public int Index => index;

    public EditorContext Context => context;

    public Pawn Pawn => context.PawnAt(Index);

    public GenderPossibility Gender
    {
        get;
        set => Set(ref field, value);
    } = GenderPossibility.Either;

    [CanBeNull]
    public PawnKindDef PinnedKind
    {
        get;
        set => Set(ref field, value);
    }

    [CanBeNull]
    public PawnBio SolidBio
    {
        get;
        set
        {
            if (Set(ref field, value) && value != null)
            {
                ForcedChildhood = null;
                ForcedAdulthood = null;
            }
        }
    }

    [CanBeNull]
    public BackstoryDef ForcedChildhood
    {
        get => SolidBio is null ? field : SolidBio.childhood;
        set
        {
            if (Set(ref field, value) && value != null)
                SolidBio = null;
        }
    }

    [CanBeNull]
    public BackstoryDef ForcedAdulthood
    {
        get => SolidBio is null ? field : SolidBio.adulthood;
        set
        {
            if (Set(ref field, value) && value != null)
                SolidBio = null;
        }
    }

    [CanBeNull]
    public CosmeticLock CosmeticLock { get; set; }

    public List<(SkillDef Skill, UsabilityRequirement Usability)> CustomRequirements
    {
        get
        {
            // lazy-init for SelectedPawn workflows
            field ??= CreateCustomRequirements();
            return field;
        }
        private set;
    }

    public IReadOnlyList<TraitRequirement> RequiredTraits { get; private set; } = [];

    public bool Locked { get; set; }

    public bool LockedCosmetics
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            CosmeticLock = value ? new CosmeticLock(Pawn) : null;
        }
    }

    public void AddTrait(TraitRequirement t)
    {
        RequiredTraits = [.. RequiredTraits, t];
        context.NotifySlotChanged();
        Logging.Debug($"Slot {Index} Forcing Trait {t.def}{(t.degree is not null ? $" (degree {t.degree})" : "")}");
    }

    public void RemoveTrait(TraitRequirement t)
    {
        RequiredTraits = [.. RequiredTraits.Where(x => x.def != t.def)];
        context.NotifySlotChanged();
        Logging.Debug($"Slot {Index} Removing Trait {t.def}{(t.degree is not null ? $" (degree {t.degree})" : "")}");
    }

    private bool Set<T>(ref T value, T newValue, [CallerMemberName] string caller = null)
    {
        if (EqualityComparer<T>.Default.Equals(value, newValue))
            return false;

        value = newValue;
        if (!Locked)
            context.NotifySlotChanged();

        Logging.Debug($"Slot Update {Index}.{caller}");
        return true;
    }

    private List<(SkillDef Skill, UsabilityRequirement Usability)> CreateCustomRequirements()
    {
        var reqs = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
        foreach (var skill in Pawn.skills.skills)
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

        return reqs;
    }

    public void ApplyBackstoryOption(BackstoryOption option)
    {
        if (option.ForcedBio is not null)
            SolidBio = option.ForcedBio;
        else if (option.Backstory.slot == BackstorySlot.Childhood)
            ForcedChildhood = option.Backstory;
        else if (option.Backstory.slot == BackstorySlot.Adulthood)
            ForcedAdulthood = option.Backstory;
    }

    public void RemoveBackstory(BackstorySlot slot)
    {
        if (SolidBio is not null)
        {
            SolidBio = null;
            ForcedChildhood = null;
            ForcedAdulthood = null;
        }
        else if (slot == BackstorySlot.Childhood)
            ForcedChildhood = null;
        else if (slot == BackstorySlot.Adulthood)
            ForcedAdulthood = null;
    }
}
