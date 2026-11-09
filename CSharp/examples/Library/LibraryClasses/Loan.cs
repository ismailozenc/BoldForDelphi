// The methods and derivations of TLoan, of the model Library.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace LibraryClasses;

public partial class TLoan
{
    /// <summary>How long a loan lasts.</summary>
    public static readonly TimeSpan LoanPeriod = TimeSpan.FromDays(14);

    /// <summary>A new loan is due when the loan period is over.</summary>
    protected override partial void CompleteCreate()
    {
        Due = DateTime.Today + LoanPeriod;
        base.CompleteCreate();
    }

    /// <summary>Overdue, derived in code: the due date has passed. It is derived again when the due date changes.</summary>
    protected virtual partial void _Overdue_DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        M_Overdue.AsBoolean = !M_Due.IsNull && Due < DateTime.Today;
        M_Due.DefaultSubscribe(Subscriber);
    }
}
