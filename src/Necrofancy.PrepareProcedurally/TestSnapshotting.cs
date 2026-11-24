#if WAYLITH

using System.IO;
using LudeonTK;

namespace Necrofancy.PrepareProcedurally;

using DefRef = ushort;

#if DEBUG
public static class VanillaDataDump
{
    [DebugAction("PrepareProcedurally", "Collect Test Data", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry)]
    public static void Dump()
    {
        var data = VanillaData.FromTestData(VanillaTestRecord.FromGame());
        var json = JsonUtility.ToJson(data);

        var dir = Path.Combine(GenFilePaths.DevOutputFolderPath, "PrepareProcedurally");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"vanilla-{VersionControl.CurrentVersionString}.json");
        File.WriteAllText(path, json);

        Logging.Info($"Dumped minimization of required data for testing to {path}");
    }
}
#endif

file static class NullableListExtensions
{
    public static TResult[] Convert<TSource, TResult>(
        [CanBeNull] this IReadOnlyList<TSource> nullableList,
        Func<TSource, TResult> converter)
    {
        return nullableList is null
            ? []
            : [..nullableList.Select(converter)];
    }

    public static List<TResult> ConvertBack<TSource, TResult>(this TSource[] array, Func<TSource, TResult> converter)
    {
        return array is null
            ? []
            : [.. array.Select(converter)];
    }

    public static List<TResult> ConvertBackOrNull<TSource, TResult>(this TSource[] array, Func<TSource, TResult> converter)
    {
        return array is null || array.Length == 0
            ? null
            : [.. array.Select(converter)];
    }
}

[UsedImplicitly]
public record VanillaTestRecord(
    IReadOnlyList<SkillDef> SkillDefs,
    IReadOnlyList<WorkTypeDef> WorkTypes,
    IReadOnlyList<TraitDef> Traits,
    IReadOnlyList<BackstoryDef> Backstories,
    IReadOnlyList<PawnBio> PawnBios
)
{
    public static VanillaTestRecord FromGame()
    {
        return new VanillaTestRecord(
            DefDatabase<SkillDef>.AllDefsListForReading,
            DefDatabase<WorkTypeDef>.AllDefsListForReading,
            DefDatabase<TraitDef>.AllDefsListForReading,
            DefDatabase<BackstoryDef>.AllDefsListForReading,
            SolidBioDatabase.allBios
            );
    }
}

[Serializable]
public class VanillaData
{
    public const int CurrentSchema = 1;

    public string gameVersion;
    public int schema;
    public SkillDefRecord[] skills;
    public WorkTypeDefRecord[] workTypes;
    public BackstoryDefRecord[] backstories;
    public TraitDefRecord[] traits;
    public SolidBioRecord[] solidBios;

    public static VanillaData FromTestData(VanillaTestRecord data)
    {
        return new VanillaData
        {
            gameVersion = VersionControl.CurrentVersionString,
            schema = CurrentSchema,
            skills = [.. data.SkillDefs.Convert(SkillDefRecord.Create)],
            workTypes = [.. data.WorkTypes.Convert(WorkTypeDefRecord.Create)],
            traits = [.. data.Traits.Convert(TraitDefRecord.Create)],
            backstories = [.. data.Backstories.Convert(BackstoryDefRecord.Create)],
            solidBios = [.. data.PawnBios.Convert(SolidBioRecord.Create)],
        };
    }

    public VanillaTestRecord ToVanillaTestRecord()
    {
        List<SkillDef> skillDefs = [..skills.ConvertBack(x => x.ToDef())];
        var skillDefLookup = skillDefs.ToDictionary(x => x.shortHash, x => x);
        List<WorkTypeDef> workTypeDefs = [.. workTypes.ConvertBack(x => x.ToDef(skillDefLookup))];
        List<TraitDef> traitDefs = [.. traits.ConvertBack(x => x.ToDef(skillDefLookup))];
        var traitLookup =  traitDefs.ToDictionary(x => x.shortHash, x => x);
        List<BackstoryDef> backstoryDefs = [.. backstories.ConvertBack(x => x.ToDef(skillDefLookup, traitLookup))];
        var backstoryLookup = backstoryDefs.ToDictionary(x => x.shortHash, x => x);
        List<PawnBio> pawnBios = [.. solidBios.ConvertBack(x => x.ToBio(backstoryLookup))];

        return new VanillaTestRecord(skillDefs, workTypeDefs, traitDefs, backstoryDefs, pawnBios);
    }
}

[Serializable]
public class SkillDefRecord
{
    public string defName;
    public DefRef shortHash;
    public float listOrder;
    public WorkTags disablingWorkTags;

    public static SkillDefRecord Create(SkillDef arg)
    {
        return new SkillDefRecord
        {
            defName = arg.defName,
            shortHash = arg.shortHash,
            listOrder = arg.listOrder,
            disablingWorkTags = arg.disablingWorkTags
        };
    }

    public SkillDef ToDef()
    {
        return new SkillDef()
        {
            defName = defName,
            shortHash = shortHash,
            listOrder = listOrder,
            disablingWorkTags =  disablingWorkTags,
        };
    }
}


[Serializable]
public class BackstoryDefRecord
{
    public string defName;
    public DefRef shortHash;
    public BackstorySlot slot;
    public string[] spawnCategories;
    public WorkTags workDisables;
    public WorkTags requiredWorkTags;
    public SkillGainRecord[] skillGains;
    public BackstoryTraitRecord[] forcedTraits;
    public BackstoryTraitRecord[] disallowedTraits;

    public static BackstoryDefRecord Create(BackstoryDef def)
    {
        return new BackstoryDefRecord
        {
            defName = def.defName,
            shortHash = def.shortHash,
            slot = def.slot,
            spawnCategories = [..def.spawnCategories.Convert(x => x)],
            workDisables = def.workDisables,
            requiredWorkTags = def.requiredWorkTags,
            skillGains = [..def.skillGains.Convert(SkillGainRecord.Create)],
            forcedTraits = [.. def.forcedTraits.Convert(BackstoryTraitRecord.Create)],
            disallowedTraits = [.. def.disallowedTraits.Convert(BackstoryTraitRecord.Create)],
        };
    }

    public BackstoryDef ToDef(Dictionary<ushort, SkillDef> skillLookup, Dictionary<ushort, TraitDef> traitLookup)
    {
        return new BackstoryDef
        {
            defName = defName,
            shortHash = shortHash,
            slot = slot,
            spawnCategories = [..spawnCategories.ConvertBackOrNull(x => x)],
            workDisables = workDisables,
            requiredWorkTags = requiredWorkTags,
            skillGains = [..skillGains.ConvertBackOrNull(x => x.ToGain(skillLookup))],
            forcedTraits = [.. forcedTraits.ConvertBackOrNull(x => x.ToBackstoryTrait(traitLookup))],
            disallowedTraits = [.. disallowedTraits.ConvertBackOrNull(x => x.ToBackstoryTrait(traitLookup))],
        };
    }
}

[Serializable]
public class SkillGainRecord
{
    public DefRef skill;
    public int amount;

    public static SkillGainRecord Create(SkillGain g) => new() { skill = g.skill.shortHash, amount = g.amount};
    public SkillGain ToGain(Dictionary<DefRef, SkillDef> deref) => new() {skill = deref[skill], amount = amount};
}


[Serializable]
public class TraitDegreeRecord
{
    public string label;
    public int degree;
    public SkillGainRecord[] skillGains;

    public static TraitDegreeRecord Create(TraitDegreeData degreeData)
    {
        return new TraitDegreeRecord
        {
            label = degreeData.label,
            degree = degreeData.degree,
            skillGains = [..degreeData.skillGains.Convert(SkillGainRecord.Create)],
        };
    }

    public TraitDegreeData ToDegreeData(Dictionary<DefRef, SkillDef> skillLookup)
    {
        return new TraitDegreeData
        {
            label = label,
            degree = degree,
            skillGains = [.. skillGains.ConvertBackOrNull(x => x.ToGain(skillLookup))],
        };
    }
}

[Serializable]
public class TraitDefRecord
{
    public string defName;
    public DefRef shortHash;
    public WorkTags workDisables;
    public WorkTags requiredWorkTags;
    public DefRef[] conflictingPassions;
    public DefRef[] forcedPassions;
    public TraitDegreeRecord[] degreeDatas;

    public static TraitDefRecord Create(TraitDef arg)
    {
        return new TraitDefRecord
        {
            defName = arg.defName,
            shortHash = arg.shortHash,
            workDisables = arg.disabledWorkTags,
            requiredWorkTags = arg.requiredWorkTags,
            conflictingPassions = [.. arg.conflictingPassions.Convert(x => x.shortHash)],
            forcedPassions = [.. arg.forcedPassions.Convert(x => x.shortHash)],
            degreeDatas = [.. arg.degreeDatas.Convert(TraitDegreeRecord.Create)],
        };
    }

    public TraitDef ToDef(Dictionary<DefRef, SkillDef> skillLookup)
    {
        return new TraitDef
        {
            defName = defName,
            shortHash = shortHash,
            disabledWorkTags = workDisables,
            requiredWorkTags = requiredWorkTags,
            conflictingPassions = [..conflictingPassions.ConvertBackOrNull(x => skillLookup[x])],
            forcedPassions = [.. forcedPassions.ConvertBackOrNull(x => skillLookup[x])],
            degreeDatas = [.. degreeDatas.ConvertBackOrNull(x => x.ToDegreeData(skillLookup))],
        };
    }
}


[Serializable]
public class BackstoryTraitRecord
{
    public DefRef def;
    public int degree;

    public static BackstoryTraitRecord Create(BackstoryTrait backstoryTrait) => new() { def = backstoryTrait.def.shortHash, degree = backstoryTrait.degree };
    public BackstoryTrait ToBackstoryTrait(Dictionary<DefRef, TraitDef> defs) => new(){ def = defs[def], degree = degree };
}


[Serializable]
public class SolidBioRecord
{
    public DefRef childhood;
    public DefRef adulthood;

    public string first;
    public string nick;
    public string last;

    public GenderPossibility gender;

    public static SolidBioRecord Create(PawnBio arg)
    {
        return new SolidBioRecord
        {
            childhood = arg.childhood.shortHash,
            adulthood = arg.adulthood.shortHash,
            first = arg.name.First,
            nick = arg.name.Nick,
            last = arg.name.Last,
            gender = arg.gender,
        };
    }

    public PawnBio ToBio(Dictionary<DefRef, BackstoryDef> derefBackstories)
    {
        return new PawnBio
        {
            childhood = derefBackstories[childhood],
            adulthood = derefBackstories[adulthood],
            name = new NameTriple(first, nick, last),
            gender = gender,
        };
    }
}

[Serializable]
public class WorkTypeDefRecord
{
    public string defName;
    public DefRef shortHash;
    public WorkTags workTags;
    public DefRef[] relevantSkills;

    public static WorkTypeDefRecord Create(WorkTypeDef def)
    {
        return new WorkTypeDefRecord
        {
            defName = def.defName,
            shortHash = def.shortHash,
            workTags = def.workTags,
            relevantSkills = [.. def.relevantSkills.Convert(x => x.shortHash)],
        };
    }

    public WorkTypeDef ToDef(Dictionary<DefRef, SkillDef> skillLookup)
    {
        return new WorkTypeDef
        {
            defName = defName,
            shortHash = shortHash,
            workTags = workTags,
            relevantSkills = [.. relevantSkills.ConvertBackOrNull(x => skillLookup[x])],
        };
    }
}

#endif
