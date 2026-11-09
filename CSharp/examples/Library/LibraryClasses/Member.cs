// The methods and derivations of TMember, of the model Library.
// Bold.CodeGen adds the methods the model declares when they are missing, and changes nothing else here.
using Bold;

namespace LibraryClasses;

public partial class TMember
{
    /// <summary>Lends a copy of the book to this member: a new loan, due in two weeks (TLoan.CompleteCreate); null when no copy is left.</summary>
    public partial TLoan? Borrow(TBook? Book)
    {
        if (Book is null || Book.Available <= 0)
        {
            return null;
        }

        return new TLoan(BoldSystem) { Book = Book, Member = this };
    }
}
