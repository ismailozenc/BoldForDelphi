// Delphi: Source/FreestandingValueSpace/Core/BoldFreeStandingValueFactories.pas
// Port map: CSharp/port/units/BoldFreeStandingValueFactories.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldFreeStandingValueFactories", Symbol = "initialization", Note = "Creates the factory and registers the free-standing classes from the Bold.ValueSpace module initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldFreeStandingValueFactories", Symbol = "finalization", Note = "Freed the factory; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Creates free-standing elements (object contents and values) by their stream name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFreeStandingElementFactory : TBoldNonRefCountedObject, IDisposable
{
    private TBoldNamedValueList? fClasses;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingElementFactory()
    {
        fClasses = new TBoldNamedValueList();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fClasses);
        GC.SuppressFinalize(this);
    }

    /// <summary>Registers <paramref name="FreeStandingClass"/> (a TBoldFreeStandingElement subclass) under <paramref name="ContentName"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void RegisterFreeStandingClass(string ContentName, TBoldFreeStandingElementClass FreeStandingClass) =>
        fClasses!.AddEntry(ContentName, "", FreeStandingClass);

    /// <summary>A new instance of the class registered under <paramref name="ContentName"/>; raises EBold when none is.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldInterfacedObject CreateInstance(string ContentName)
    {
        if (fClasses!.ObjectByName(ContentName) is TBoldFreeStandingElementClass ElementClass)
        {
            return (TBoldInterfacedObject)Activator.CreateInstance(ElementClass)!;
        }

        throw new EBold(SysUtils.Format(FreeStandingValuesConst.sNoClassregisteredForName, GetType().Name, ContentName));
    }
}

/// <summary>The unit's global factory.</summary>
public static partial class BoldFreeStandingValueFactories
{
    [Delphi(PortStatus.Ported)]
    public static TBoldFreeStandingElementFactory? FreeStandingValueFactory;

    /// <summary>The unit's initialization: creates the factory and registers the free-standing classes.</summary>
    internal static void Initialize()
    {
        FreeStandingValueFactory = new TBoldFreeStandingElementFactory();
        TBoldFreeStandingElementFactory f = FreeStandingValueFactory;
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BOLDOBJECTCONTENTSNAME, typeof(TBoldFreeStandingObjectContents));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_String, typeof(TBFSString));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Currency, typeof(TBFSCurrency));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Float, typeof(TBFSFloat));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Integer, typeof(TBFSInteger));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Boolean, typeof(TBFSBoolean));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Date, typeof(TBFSDate));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Time, typeof(TBFSTime));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_DateTime, typeof(TBFSDateTime));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_Blob, typeof(TBFSBlob));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_TypedBlob, typeof(TBFSTypedBlob));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_ObjectIdRef, typeof(TBFSObjectIDRef));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair, typeof(TBFSObjectIDRefPair));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_ObjectIdListRef, typeof(TBFSObjectIdListref));
        f.RegisterFreeStandingClass(BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair, typeof(TBFSObjectIdListrefPair));
    }
}
