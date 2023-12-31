Issues can be reported on the Steam page discussions, or by DMing or pinging me (necrofancy) on Discord. In order for me to receive your DM without a friend request you will need to be a part of either the Rimworld unofficial discord, or the official development discord.

# Currently Known Issues

In roughly order of priority, these are summaries of known issues:

* "Animals" can be set to be a major or minor passion on a character that, due to their backstory, will have "Handling" worktype disabled.
    * This is because Handling and Hunting are both animals-relevant works. A Hunter will benefit from the skill regardless of their ability to Handle, so the game will display in that case.
    * This is probably not be what players are wanting when they specify that they want a pawn that has a strong passion in Animals.
* Adding or removing a trait that affects skills should cause a reroll of pawn stats and passions.
* Traits affecting pawn's final skills will lead to going over the maximum passion points. With the default of 7.0, this is still roughly fine.

# Mulled Ideas

* Leveraging traits to influence pawn's final skills and passions in procgen
    * As an example, if procgen wants a pawn to be good at Melee, but not Shooting, give it the ability to choose Brawler as a requested trait.
    * Limitations to resolve:
        * There would need to be a setting to _not_ leverage this with certain skills. Gourmand gives a bonus to Cooking, Sickly gives a bonus to Medical, and Tortured Artist forces an Artistic as the highest-priority in passions. Players generally hate those traits. 