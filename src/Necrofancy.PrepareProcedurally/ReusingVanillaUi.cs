using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Necrofancy.PrepareProcedurally.Interface;
using RimWorld;
using UnityEngine;
using Verse;

#pragma warning disable CS8321 // Local function is declared but never used

// ReSharper disable UnusedMember.Local

namespace Necrofancy.PrepareProcedurally;

public static partial class ReusingVanillaUi
{
#if !RW1_4
    internal static void DrawPortraitArea(
        Rect rect,
        int pawnIndex,
        bool renderClothes,
        bool renderHeadgear)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();

            var vanillaDrawCharacterCard = AccessTools.Method(typeof(CharacterCardUtility),
                nameof(CharacterCardUtility.DrawCharacterCard));
            
            var randomizeMethod = AccessTools.Method(typeof(SelectedPawn), nameof(SelectedPawn.Randomize));
            var vanillaRandomize = AccessTools.Method(typeof(StartingPawnUtility), nameof(StartingPawnUtility.RandomizePawn));

            foreach (var instruction in instructions)
            {
                // Replace the Action being loaded in 
                if (instruction.opcode == OpCodes.Ldftn
                    && instruction.operand is MethodInfo lambda
                    && IsLambdaCallingMethod(lambda, vanillaRandomize))
                {
                    Logging.Debug("Loading SelectedPawn.Randomize instead of vanilla version");
                    yield return new CodeInstruction(OpCodes.Ldftn, randomizeMethod);
                }
                else if (instruction.Calls(vanillaDrawCharacterCard))
                {
                    Logging.Debug("Swapping call for DrawCharacterCard");
                    
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ReusingVanillaUi), nameof(ReusingVanillaUi.DrawCharacterCard)));
                }
                else
                {
                    yield return instruction;
                }
            }

            EndTranspile();
        }

        _ = Transpiler(null);
    }
#endif
    internal static void DrawCharacterCard(
        Rect rect,
        Pawn pawn,
        Action randomizeCallback = null,
        Rect creationRect = default(Rect),
        bool showName = true)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();

            var vanillaDrawSkills = AccessMethod(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(vanillaDrawSkills))
                {
                    Logging.Debug("Swapping call for DrawSkillsOf");
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ReusingVanillaUi), nameof(ReusingVanillaUi.DrawSkillsOf)));
                }
                else
                {
                    yield return instruction;
                }
            }

            EndTranspile();
        }
    }
    
    /// <summary>
    /// Similar to <see cref="SkillUI.DrawSkillsOf"/> but adding character controls for UI
    /// </summary>
    internal static void DrawSkillsOf(Pawn p, Vector2 offset, SkillUI.SkillDrawMode mode, Rect container)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();
            var drawButton = AccessMethod(typeof(PassionButton), nameof(PassionButton.DrawSkillRequirementIcon));

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (CallMatches(instruction, typeof(SkillUI), "DrawSkill"))
                {
                    Logging.Debug("Adding additional call for selectable passions");
                    // Load x 
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector2), nameof(Vector2.x)));
                    // Load y
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    // Load SkillDef
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);
                    // call PassionButton.DrawSkillRequirementIcon(float,float,SkillDef)
                    yield return new CodeInstruction(OpCodes.Call, drawButton);
                }
            }

            EndTranspile();
        }
    }

    [Conditional("DEBUG")]
    private static void StartTranspile([CallerMemberName] string name = "") => Logging.Debug($"Start transpiling {name}");
    
    [Conditional("DEBUG")]
    private static void EndTranspile([CallerMemberName] string name = "") => Logging.Debug($"End transpiling {name}");

    private static MethodInfo AccessMethod(Type type, string methodName, Type[] parameters = null, Type[] generics = null)
    {
        var info = AccessTools.Method(type, methodName, parameters, generics);
        if (info is null)
        {
            Logging.Error($"Error resolving '{type.Name}.{methodName}'.");
        }
        
        return info;
    }

    private static CodeMatch LooseMatch(Type type, string methodName)
    {
        return new CodeMatch(instruction => CallMatches(instruction, type, methodName));
    }
    
    private static bool CallMatches(CodeInstruction instr, Type type, string methodName)
    {
        return (instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) 
               && instr.operand is MethodBase mb 
               && mb.Name == methodName 
               && mb.DeclaringType == type;
    }
    
    private static bool IsLambdaCallingMethod(MethodInfo lambda, MethodInfo target)
    {
        if (lambda == null || target == null) return false;

        try 
        {
            var instructions = PatchProcessor.GetOriginalInstructions(lambda);
            return instructions.Any(ins => 
                (ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt) && 
                ins.operand is MethodInfo mi && mi == target);
        }
        catch
        {
            return false;
        }
    }
}