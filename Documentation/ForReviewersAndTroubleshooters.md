This writeup contains relevant information for troubleshooters and other modders that might be interested in what Prepare Procedurally might be doing under the hood. I'm highly aware that mods in this space are pretty fraught for stability, so I've put some meticulous work into patching and working in the minimum area needed specifically for affecting starting characters, and avoiding any changes that could affect the game after creation.

Despite first hitting the workshop in late December of 2023, this mod has been on-and-off development since well before Biotech DLC was released. I figured releasing something publicly in this space needed to be done right if it was done at all.

# Patches and State Edits

There's a couple areas that are worth noting for other modders or developers.
* [Harmony Patches](../src/Necrofancy.PrepareProcedurally/HarmonyPatches.cs) are almost entirely limited to just changes adjacent to the Create Character page (`Page_ConfigureStartingPawns`). It sets some initial state when that page opens, clears that state when the player proceeds from the dialog, and as far as behavioral changes just adds a button to open the prepare procedurally page. All pawn changes are limited to that window.
    * When Humanoid Alien Races is loaded, there is a secondary patch to `StartingPawnUtility` - see the HAR specific documentation in a later section.
* Procedural Generation will make a few edits to defs, or `PawnGenerationRequests`, to force pawn generation to be biased for the duration of pawn randomization. These changes are immediately reverted after procedural generation is done randomizing pawns in-place.
    * The class responsible for those kinds of edits is in [the TemporarilyChange class](../src/Necrofancy.PrepareProcedurally/Solving/StateEdits/TemporarilyChange.cs). 
    * Troubleshooters and testers can confirm that these changes don't persist after procedural generation by hitting "Randomize" at the Create Characters page - this mod does not patch any behavior on the randomize button in `Page_ConfigureStartingPawns`.
    * [Harmony Patches](../src/Necrofancy.PrepareProcedurally/HarmonyPatches.cs)
* After the pawn is generated, procgen's further state edits are limited to skills, traits, and passions. 
    * This mod will not try to change the `ThingDef` of the pawn or any other complex operation. Backstory-solving in [AlienProcGen](../src/Necrofancy.PrepareProcedurally.HumanoidAlienRaces/Solving/AlienProcGen.cs) specifically generates pawns first, and then tries to solve for backstories to prevent having to deal with this.
    * [PostPawnGenerationChanges](../src/Necrofancy.PrepareProcedurally/Solving/StateEdits/PostPawnGenerationChanges.cs) is used for Vanilla.
    * [AlienSpecificPostPawnGenerationChanges](../src/Necrofancy.PrepareProcedurally.HumanoidAlienRaces/Solving/AlienSpecificPostPawnGenerationChanges.cs) is used for Alien Races.
* If, for any reason, procedural generation controlled by the UI throws an exception, we fail loudly and try to rectify/resolve state by running normal pawn randomization on all starting pawns. This is a further safeguard against accidentally corrupting pawns that would lead to a save-ending problem or troubleshooting problem specific to a save later.

# Humanoid Alien Races Compatibility Notes

## Pawn Generation Requests

As of writing, the transpilers to `StartingPawnUtility` in HAR will just blow away whatever's in `StartingPawnUtility.StartingAndOptionalPawnGenerationRequests` with a call to `StartingPawnUtility.DefaultStartingPawnRequest` whenever `StartingPawnUtility.NewGeneratedStartingPawn` is called. So, for any call to randomize a starting pawn, I actually just have no idea what kind of pawn will be generated until after it runs.

### Editing Pawn Generation Requests

My vanilla-affecting methods to temporarily change the contents of `StartingPawnUtility.StartingAndOptionalPawnGenerationRequests` will not work; those contents are blown away on each randomization call.

To handle that, [I specifically postfix to apply my PawnGenerationRequests AFTER the transpiler has blown away the original request](../src/Necrofancy.PrepareProcedurally.HumanoidAlienRaces/HarmonyPatches.cs). This applies a series of built up edits based on Editor state done by [AlienProcGen](../src/Necrofancy.PrepareProcedurally.HumanoidAlienRaces/Solving/AlienProcGen.cs) and will clear the list of edits. Since this list is only ever built-up in my ProcGen workflows moments before randomizing a pawn, and it's cleared afterwards, it should still have the same guarantee that default pawn generation (i.e. the Randomize button in `Page_ConfigureStartingPawns`) is unaffected.

### Other Workarounds

* It's unknowable if the pawn kind is monosex before randomizing it, so a v1.0.0.3 feature to force a gender can't really function sometimes. The gender icon is grayed out and the UI tooltip states it can't be forced for that pawn race if this happens.
* Procgen works backwards for finding available backstories by inspecting the category group of the given pawn's backstory after one has been generated. From there, it will read that pawn's backstories to figure out what category to use, and then finds the best backstory combo from there.
* Instead of specifying the age, ProcGen just applies a temporary edit for the age curves on races accordingly. See [`TemporarilyChange.AgeOnAllRelevantRaceProperties`](../src/Necrofancy.PrepareProcedurally/Solving/StateEdits/TemporarilyChange.cs). This way, no matter what gets generated, it's within the age range interval that the user specified. This can be tested to not have an effect outside of ProcGen by setting a narrow band on a race's age settings in the UI, randomizing, and then randomizing pawns outside of Prepare Procedurally UIs. `Page_ConfigureStartingPawns`'s randomize button is totally unaffected by this change, from testing.