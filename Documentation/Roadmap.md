# Features probably along this order:

* Access features on the table in the [PrepareProcedurally](../src/Necrofancy.PrepareProcedurally/) page inside the [EditSpecificCharacter](../src/Necrofancy.PrepareProcedurally/Interface/Dialogs/EditSpecificPawn.cs) dialog. This is going to involve maintaining a version of the character card UI and adding a few other features outside of that window section.

* Specify work tags required for the colony.
    * e.g. All characters must be capable of Cleaning/Dumb Labor

* Specify skills that should not be satisfied on the same character - biggest example being Shooting and Melee. Some work will need to be done on the UI front for this.
    * Work Tags should also be specified as required for a specific task (e.g. Animals skill/passion requiring Handling worktype)