using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public record struct BackstoryOption(BackstoryDef Def, GenderPossibility ForcedGender = GenderPossibility.Either, PawnBio ForcedBio = null);