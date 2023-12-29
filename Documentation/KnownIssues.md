Issues can be reported on the Steam page discussions, or by DMing or pinging me (necrofancy) on Discord. In order for me to receive your DM without a friend request you will need to be a part of either the Rimworld unofficial discord, or the official development discord.

# Currently Known Issues

In roughly order of priority, these are summaries of known issues:

1. Trait-related skill point bonuses are not being accounted for in final pawn skills. Some forced passions will also not be accounted for if the trait was randomized or added by request.
2. Skill Ranges aren't manipulated to be -fully- accurate to pawn generation's priority on assigning passions. 
    * As an example, if we want to generate a pawn that has 0~4 shooting, artistic, and social available, it is possible that for choosing a passion of social and no passion for shooting and artistic, a pawn could have 3 in all of them. This is slightly inaccurate to what pawn generation would do; it would give the passion in order of the skill defs.