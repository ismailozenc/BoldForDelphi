// The methods and derivations of TResidential_Building, of the model BuildingsAndOwners.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace BuildingClasses;

public partial class TResidential_Building
{
    private static readonly string[] Streets = ["Bold Drive", "5th Avenue", "Rose Path", "Select Avenue", "Pebble Road"];

    /// <summary>Tells who was evicted, and why (the Delphi example showed a message).</summary>
    public static event Action<string>? Evicted;

    /// <summary>
    /// The residents share the rent, and the owners share it as income; a resident left with negative assets is
    /// evicted.
    /// </summary>
    public partial void ChargeRent()
    {
        if (Residents.Count == 0)
        {
            throw new InvalidOperationException("No residents to pay rent");
        }

        if (Owners.Count == 0)
        {
            throw new InvalidOperationException("No owners to receive payment");
        }

        decimal rent = TotalRent;
        for (int r = Residents.Count - 1; r >= 0; r--)
        {
            TPerson resident = Residents[r]!;
            resident.Assets -= rent / Residents.Count;
            if (resident.Assets < 0)
            {
                resident.Home = null;
                Evicted?.Invoke($"{resident.FirstName} {resident.LastName} has been evicted from {Address} due to lack of funds");
            }
        }

        int owners = Owners.Count;
        for (int o = 0; o < owners; o++)
        {
            Owners[o]!.Assets += rent / owners;
        }
    }

    /// <summary>A new residential building gets a street address and a rent.</summary>
    protected override partial void CompleteCreate()
    {
        base.CompleteCreate();
        Address = Random.Shared.Next(250) + " " + Streets[Random.Shared.Next(5)];
        TotalRent = Random.Shared.Next(100) * 100;
    }
}
