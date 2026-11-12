// The methods and derivations of TLoan, of the model Library.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace LibraryClasses;

public partial class TLoan
{
    protected override partial void CompleteCreate()
    {
    }

    protected virtual partial void _Overdue_DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        // Calculate the value, subscribe to what it depends on, and set it:
        // M_Overdue.AsBoolean = ...;
    }
}
