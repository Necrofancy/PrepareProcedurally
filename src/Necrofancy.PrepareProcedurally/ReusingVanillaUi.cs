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

public static class ReusingVanillaUi
{
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
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(vanillaDrawCharacterCard))
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

            var vanillaDoLeft = AccessMethod(typeof(CharacterCardUtility), nameof(ReusingVanillaUi.DoLeftSection));
            var vanillaDrawSkills = AccessMethod(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(vanillaDoLeft))
                {
                    Logging.Debug("Swapping call for DoLeftSection");
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ReusingVanillaUi), nameof(ReusingVanillaUi.DoLeftSection)));
                }
                else if (instruction.Calls(vanillaDrawSkills))
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

        _ = Transpiler(null);
    }

    internal static void DoLeftSection(Rect rect, Rect leftRect, Pawn pawn)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();
            
            foreach (var instruction in instructions)
            {
                yield return instruction;
            }
            
            EndTranspile();
        }

        _ = Transpiler(null);
    }
    
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

        _ = Transpiler(null);
    }

    [Conditional("DEBUG")]
    private static void StartTranspile([CallerMemberName] string name = "") => Logging.Debug($"Start transpiling {name}");
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

    private static bool CallMatches(CodeInstruction instr, Type type, string methodName)
    {
        return (instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) 
               && instr.operand is MethodBase mb 
               && mb.Name == methodName 
               && mb.DeclaringType == type;
    }
}