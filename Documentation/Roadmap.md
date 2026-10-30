# Features probably along this order:

* Explore an alternative way to Randomize Pawns (hopefully sunsetting or deleting the PostPawnChanges usage)

* More UI clarity in [EditSpecificCharacter](../src/Necrofancy.PrepareProcedurally/Interface/Dialogs/EditSpecificPawn.cs) dialog.
    * Total Passion Indicator (e.g. "4.5 out of 5 Passion Points used")
    * Reset buttons on skills

* Specify work tags required for the colony.
    * e.g. All characters must be capable of Cleaning/Dumb Labor

* Specify skills that should not be satisfied on the same character - biggest example being Shooting and Melee. Some work will need to be done on the UI front for this.
    * Work Tags should also be specified as required for a specific task (e.g. Animals skill/passion requiring Handling worktype)
