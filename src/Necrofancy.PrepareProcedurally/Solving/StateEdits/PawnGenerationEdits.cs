using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

public static class PawnGenerationEdits
{
    public static void SetFixedAge(this List<PawnGenerationRequest> reqs, float age, int index)
    {
        var request = reqs[index];
        
        request.FixedBiologicalAge = age;
        request.BiologicalAgeRange = null;
        request.ExcludeBiologicalAgeRange = null;
        
        reqs[index] = request;
    }
    
    public static void SetAgeRange(this List<PawnGenerationRequest> reqs, FloatRange age, int index)
    {
        var request = reqs[index];
        
        request.FixedBiologicalAge = null;
        request.BiologicalAgeRange = age;
        request.ExcludeBiologicalAgeRange = null;
        
        reqs[index] = request;
    }

    public static void SetGender(this List<PawnGenerationRequest> reqs, GenderPossibility gender, int index)
    {
        var request = reqs[index];
        
        request.FixedGender = gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            GenderPossibility.Either => null,
            _ => null
        };
        
        reqs[index] = request;
    }
    
    public static void DisableRelations(this List<PawnGenerationRequest> reqs, int index)
    {
        var request = reqs[index];
        request.CanGeneratePawnRelations = false;
        reqs[index] = request;
    }
}