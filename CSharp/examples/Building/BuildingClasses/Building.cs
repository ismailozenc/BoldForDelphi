// The methods and derivations of TBuilding, of the model BuildingsAndOwners.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace BuildingClasses;

public partial class TBuilding
{
    private static readonly string[] Buildings = ["White House", "Eiffel Tower", "Chinese wall", "Balmoral", "Westminister Abbey"];

    /// <summary>A new building gets a famous address and a zip code.</summary>
    protected override partial void CompleteCreate()
    {
        Address = Buildings[Random.Shared.Next(5)];
        ZipCode = Random.Shared.Next(9000) + 1000;
        base.CompleteCreate();
    }
}
