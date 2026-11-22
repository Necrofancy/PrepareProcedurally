using System.Diagnostics;
using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Editing;

public class EditorContext
{
    private readonly Stopwatch timerForRandomizing = new ();
    private readonly List<Pawn> startingAndOptionalPawns;
    [CanBeNull] private readonly PrepareModSettings settings;
    private readonly Action<EditorContext> randomizeTeam;

    public EditorContext(List<Pawn> startingAndOptionalPawns, int startingPawnCount, Action<EditorContext> randomizeTeam, PrepareModSettings settings)
    {
        if (startingPawnCount <= 0 || startingAndOptionalPawns.Count < startingPawnCount)
            throw new ArgumentOutOfRangeException(nameof(startingPawnCount));

        this.startingAndOptionalPawns = startingAndOptionalPawns;
        var slots = new PawnSlot[startingPawnCount];
        for (var i = 0; i < startingPawnCount; i++)
            slots[i] = new PawnSlot(this, i);

        Slots = slots;

        var (race, ages) = settings?.GetLoadedHumanTypes() ?? PrepareModSettings.DefaultHumanData();
        PawnKindAdultAges = ages;
        SelectedRace = race;

        if (settings is not null)
        {
            // init based on defaults
            MelaninRange = settings.defaultMelaninRange;
            AllowInjuries = settings.allowInjuries;
            AllowRelationships = settings.allowRelationships;
            AllowPregnancy = settings.allowPregnancy;
            MaxPassionPoints = settings.maxPassionPoints;
        }

        DebounceDelay = 1000;
        AutoGenerateTeams = false;
        this.settings = settings;
        this.randomizeTeam = randomizeTeam;
        timerForRandomizing.Reset();
    }

    public IReadOnlyList<PawnSlot> Slots { get; }

    public PawnSlot this[int index] => Slots[index];

    public PawnSlot this[Pawn pawn] => Slots.FirstOrDefault(s => s.Pawn == pawn);

    public bool OverrideSettings { get; set; } = false;

    public int DebounceDelay
    {
        get => OverrideSettings ? field : settings?.debounceDelay ?? field;
        set;
    }

    public bool AutoGenerateTeams
    {
        get => OverrideSettings ? field : settings?.autoGenerate ?? field;
        set;
    }

    public Dictionary<ThingDef, RaceAgeData> PawnKindAdultAges { get; }

    public List<SkillPassionSelection> AllPassions { get; set; } = [];

    public ThingDef SelectedRace
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                SelectedAgeRange = PawnKindAdultAges[value];
                // selection is just a UI selection so don't trigger randomization
            }
        }
    }

    public RaceAgeData SelectedAgeRange
    {
        get;
        set
        {
            if (!field.Equals(value))
            {
                field = value;
                PawnKindAdultAges[SelectedRace] = value;
                CountdownToRandomize();
            }
        }
    }

    public FloatRange MelaninRange
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                CountdownToRandomize();
            }
        }
    } = new(0, 1);

    public bool AllowPregnancy
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                CountdownToRandomize();
            }
        }
    } = true;

    public bool AllowInjuries
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                CountdownToRandomize();
            }
        }
    } = true;

    public bool AllowRelationships
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                CountdownToRandomize();
            }
        }
    } = true;

    public float MaxPassionPoints
    {
        get;
        set
        {
            if (!Mathf.Approximately(field, value))
            {
                field = value;
                CountdownToRandomize();
            }
        }
    }

#if DEBUG
    public bool StressTestMode { get; set; }
#endif

    public void WindowTick()
    {
#if DEBUG
        if (StressTestMode)
        {
            randomizeTeam(this);
            return;
        }
#endif
        if (timerForRandomizing.IsRunning && timerForRandomizing.ElapsedMilliseconds > DebounceDelay)
        {
            Logging.Debug("Randomizing team after debounce");
            timerForRandomizing.Stop();
            randomizeTeam(this);
        }
    }

    public void Randomize() => randomizeTeam(this);

    public Pawn PawnAt(int index) => startingAndOptionalPawns[index];

    public void NotifySlotChanged() => CountdownToRandomize();

    private void CountdownToRandomize()
    {
        if (AutoGenerateTeams)
            timerForRandomizing.Restart();
    }

    public static EditorContext LiveContext()
    {
        var pawnList = Find.GameInitData.startingAndOptionalPawns;
        var pawnCount = Find.GameInitData.startingPawnCount;

        void Randomize(EditorContext editor)
        {
            var teamContext = new TeamRebalance(editor, PrepareMod.Settings.RuleSet);
            ProcGen.RandomizeTeam(teamContext);
        }

        return new EditorContext(pawnList, pawnCount, Randomize, PrepareMod.Settings)
        {
            AllPassions = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(SkillPassionSelection.CreateFromSkill).ToList()
        };
    }

    public static EditorContext MockContext(List<Pawn> pawns, int count, Action<EditorContext> randomizeTeam)
    {
        return new EditorContext(pawns, count, randomizeTeam, settings: null);
    }
}
