// The methods and derivations of TPerson, of the model BuildingsAndOwners.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace BuildingClasses;

public partial class TPerson
{
    private static readonly string[] FirstNames = ["Harry", "Peter", "David", "John", "James"];
    private static readonly string[] LastNames = ["Jones", "Flint", "McGregor", "Gates", "Clinton"];

    /// <summary>A new person gets a name and some money.</summary>
    protected override partial void CompleteCreate()
    {
        FirstName = FirstNames[Random.Shared.Next(5)];
        LastName = LastNames[Random.Shared.Next(5)];
        Assets = 5000 + Random.Shared.Next(10000);
        base.CompleteCreate();
    }

    /// <summary>Moves the amount from the lender's assets to this person's.</summary>
    public partial void BorrowFrom(TPerson? Lender, int Amount)
    {
        if (Lender is not null)
        {
            Assets += Amount;
            Lender.Assets -= Amount;
        }
    }
}
