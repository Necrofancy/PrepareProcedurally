namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public readonly struct AlienCategories
{
    public AlienCategories(string childhoodCategory, string adulthoodCategory)
    {
        ChildhoodCategory = childhoodCategory;
        AdulthoodCategory = adulthoodCategory;
    }

    public string ChildhoodCategory { get; }
    public string AdulthoodCategory { get; }
}