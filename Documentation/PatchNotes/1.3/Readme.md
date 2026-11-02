Prepare Procedurally v1.3 is here, with the following:

* A direct 1.7 build! 
* More variety in backstory selection! Backstories are now picked with more randomness across the top choices for a given pawn.
	* It keeps the top N scoring childhood/adulthood combos (default 50)
	* Drops any combo scoring under a certain amount of the best score (default 0.6x score)
	* A **weighted** roll will be done, weighing by the score of all remaining options.
* Mod Settings is now available directly in the Prepare Procedurally window, and has a few more options. **You probably won't need to update these**
	* Debouncing - waits a bit for further edits before doing a team-based randomization
	* Backstory Candidate Selection Size: the maximum amount of backstory combinations to consider for making a weighted roll.
	* Relative Quality Threshold - Filter anything under this score relative to the "best" option for creating the above list.

![[https://github.com/Necrofancy/PrepareProcedurally/blob/main/Documentation/PatchNotes/1.3/Images/Settings.png]]

### Specific Backstory Selection

The following is currently only active when you do NOT have Humanoid Alien Races in the modpack; I'm still needing to figure out how to comprehensively follow the different modder-defined Pawn Generation rules in order to accurately follow what HAR mod authors intend to allow in their races.

The Specific Pawn Customization more prominently shows the ability to select your own backstory:

![[https://github.com/Necrofancy/PrepareProcedurally/blob/main/Documentation/PatchNotes/1.3/Images/BackstoryButtons.png]]

When selecting a backstory in 1.6 or later, you are now able to search for traits, childhood or adulthood story titles (e.g. Bodyguard), or skills and labor in order to find an appropriate backstory for what you are looking for!

![[https://github.com/Necrofancy/PrepareProcedurally/blob/main/Documentation/PatchNotes/1.3/Images/Search.png]]

You can search for the following:
* Traits forced by a backstory
* Skills (e.g. Plants)
* Work capabilities (e.g. Dumb Labor, Hauling, Cleaning, Violent)
	* Do note that MOST backstories are capable of these; you can combine this with other search terms.
Commas and other common language-based separators (',', '，', '、', '،', ';', '；') turn the search into an "AND" operation. As the screenshot shows, I am able to search for a Tough pawn that also has Plants in their backstory by searching for `tough,plants`. Whitespace is trimmed out so you don't have to worry about accidentally adding spaces and ruining a search.

If you're still playing on Rimworld 1.4 or 1.5, the float menu is still there for you.

Picking the second backstory now automatically filters out backstories you would not be able to based on the currently locked-in one. Previously, you were able to select a Childhood that disables violence, and then try to select a backstory that required Violent. This would error out, now it no longer shows up in available options.

## Humanoid Alien Races Support: a Call to Arms

I have a pretty extensive amount of compatibility support with Humanoid Alien Races, but admittedly _I don't play a lot with HAR myself_. This makes it hard to notice issues with HAR specifically as that mod updates independently from myself.

I will likely need callouts specifically for issues related to HAR compatibility. A bunch of changes under the hood have been bugfixing and stabilizing the team generation while HAR is active.

I have made a thread specifically for HAR-Compatibility-Specific bugs [on the steam workshop page](https://steamcommunity.com/workshop/filedetails/discussion/3121493365/567049990495714467/). Please use that or Discord DMs for reporting issues, whatever you are more comfortable with.

Issues found in testing, including running team generation through several thousand cycles:
* Teamwide generation can be slow. The debouncing UI feature was added to mainly combat this. I'm looking into some different ways I can indirectly influence pawn generation to (possibly) unify the logic between influencing pawn generation for HAR.
* Trait rerolling was not handled quite properly, leading to erroring out on races that have AlienChanceEntries for Traits. This has been fixed.
* Load Order has been updated to account for HAR's changes to the Randomize window for Editing Singular Pawns.
* Team generation could hang if a race couldn’t produce a usable backstory; it now gives up after 100 attempts and logs an error. Please upload a log to the bug reports page if you encounter this!

Thank you and enjoy!