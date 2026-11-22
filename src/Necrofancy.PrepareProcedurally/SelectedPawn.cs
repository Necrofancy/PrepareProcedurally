using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public static class SelectedPawn
{
    public static Pawn Pawn => Slot.Pawn;

    public static PawnSlot Slot => EditorSession.Require().Slots[Index];

    public static bool KeepCosmetics
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            Slot.CosmeticLock = field ? new CosmeticLock(Pawn) : null;

            if (!field)
                return;

            Slot.ForcedChildhood = Pawn.story.Childhood;
            Slot.ForcedAdulthood = Pawn.story.Adulthood;
        }
    }

    public static int Index { get; private set; }

    public static void Select(Pawn pawn)
    {
        Index = StartingPawnUtility.PawnIndex(pawn);
        Slot.Locked = true;
    }

    public static float GetPassionPoints()
    {
        var sum = 0.0f;
        foreach (var item in Slot.CustomRequirements)
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

    public static void Randomize()
    {
        var collect = new CollectSpecificPassions(Slot);
        var rules = PrepareMod.Settings.RuleSet;
        ProcGen.RandomizeCustomPawn(new PawnCustomize(Slot, collect, rules));
    }
}
