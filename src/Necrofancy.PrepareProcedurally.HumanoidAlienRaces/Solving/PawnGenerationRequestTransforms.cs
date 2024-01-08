using System.Collections.Generic;
using AlienRace;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

public static class PawnGenerationRequestTransforms
{
    private delegate void EditRequest(ref PawnGenerationRequest request);

    private static readonly List<EditRequest> BuiltUpChanges = [];

    public static void SetBasedOnEditorSettings(int index)
    {
        if (!Editor.AllowBadHeDiffs)
            PreventAddictions();
        if (!Editor.AllowRelationships)
            PreventRelationships();
        if (Editor.GenderRequirements[index] != GenderPossibility.Either)
            FixGender(Editor.GenderRequirements[index]);
        if (!Editor.AllowPregnancy)
            PreventPregnancy();
    }
    
    public static void ApplyChangesTo(ref PawnGenerationRequest request)
    {
        foreach (var transform in BuiltUpChanges)
            transform(ref request);
        
        BuiltUpChanges.Clear();
    }
    
    public static void FixGender(GenderPossibility gender)
    {
        Gender? fixedGender = gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            _ => null
        };

        void EditGenderRequest(ref PawnGenerationRequest request)
        {
            if (request.KindDef.race is ThingDef_AlienRace race)
            {
                var maleProbability = race.alienRace.generalSettings.maleGenderProbability;
                if ((fixedGender == Gender.Male && maleProbability <= 0) 
                    || (fixedGender == Gender.Female && maleProbability >=1 ))
                {
                    return;
                }
            }
            
            request.FixedGender = fixedGender;
        }
        
        BuiltUpChanges.Add(EditGenderRequest);
    }
    
    public static void PreventAddictions()
    {
        void EditAddiction(ref PawnGenerationRequest request) => request.AllowAddictions = false;

        BuiltUpChanges.Add(EditAddiction);
    }
    
    public static void PreventRelationships()
    {
        void Disable(ref PawnGenerationRequest request) => request.CanGeneratePawnRelations = false;

        BuiltUpChanges.Add(Disable);
    }
    
    public static void PreventPregnancy()
    {
        void Disable(ref PawnGenerationRequest request) => request.AllowPregnant = false;

        BuiltUpChanges.Add(Disable);
    }
}