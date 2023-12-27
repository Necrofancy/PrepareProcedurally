using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting;

/// <summary>
/// Any function that defines a weighting skill for a pawn's backstory.
/// </summary>
public delegate float WeightBackgroundAlgorithm(BioPossibility possibility);