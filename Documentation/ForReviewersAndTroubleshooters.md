The space that this mod is working in are, to say the least, fraught.

# Patches and State Edits

There's a couple areas that are worth noting for other modders or developers.
* [Harmony Patches](../src/Necrofancy.PrepareProcedurally/HarmonyPatches.cs) are limited to just changes adjacent to the Create Character page (`Page_ConfigureStartingPawns`). It sets some initial state when that page opens, clears that state when the player proceeds from the dialog, and as far as behavioral changes just adds a button to open the prepare procedurally page. All pawn changes are limited to that window.
* Procedural Generation will make a few edits to defs, or `PawnGenerationRequests`, to force pawn generation to be biased for the duration of pawn randomization. These changes are immediately reverted after procedural generation is done editing.
    * The class responsible for those kinds of edits is in [the TemporarilyChange class](../src/Necrofancy.PrepareProcedurally/Solving/StateEdits/TemporarilyChange.cs). You can confirm that these changes don't persist after procedural generation by hitting "Randomize" at the Create Characters page - this mod does not patch any behavior on that.
* After the pawn is generated, procgen's further state edits are limited to skills, traits, and passions. We don't try to change the `ThingDef` of the pawn or any other complex operation.
    * [PostPawnGenerationChanges](../src/Necrofancy.PrepareProcedurally/Solving/StateEdits/PostPawnGenerationChanges.cs) is used for Vanilla.
    * [AlienSpecificPostPawnGenerationChanges](../src/Necrofancy.PrepareProcedurally.HumanoidAlienRaces/Solving/AlienSpecificPostPawnGenerationChanges.cs) is used for Alien Races.
* If, for any reason, procedural generation controlled by the UI throws an exception, we fail loudly and try to rectify/resolve state by running normal pawn randomization on the existing pawns. This is a further safeguard against accidentally corrupting pawns that would lead to a save-ending problem or troubleshooting problem specific to a save later.