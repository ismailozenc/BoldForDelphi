// Delphi: Source/ObjectSpace/Ocl/BoldSSYaccU.pas
// Port map: CSharp/port/units/BoldSSYaccU.md
// The Sandstone table-driven LALR parser engine. As in BoldSSLexU, the table is a byte array read at offsets with the
// Delphi layout; reference counts on lexemes and stack elements are kept, but nothing is freed by hand.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSArrayOfLongint", Note = "SSSetOfLongint keeps a List<int>.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSArrayOfLongint", Note = "No pointers; SSSetOfLongint keeps a List<int>.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccPairTable", Note = "Read from the table bytes: TableType 0, Prods 4, States 8, RowOffset 12, ProdOffset 16.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccPairTableRow", Note = "Read from the table bytes: Flags 0, Gotos 4, Actions 8, then the entries.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccPairTableEntry", Note = "Read from the table bytes: Entry 0, Token 4 (SSYaccTable.EntryAt).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccPairTableProd", Note = "Read from the table bytes: Size 0, Leftside 4 (SSYaccTable.ProdSize, ProdLeftside).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccPairTable", Note = "No pointers: the table is a byte array.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccPairTableRow", Note = "No pointers: rows are byte offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccPairTableProd", Note = "No pointers: productions are read by index.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccPairTableEntry", Note = "No pointers: entries are byte offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccEntryArray", Note = "Read from the table bytes after each row header.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccEntryArray", Note = "No pointers: byte offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccProdArray", Note = "Read from the table bytes at ProdOffset.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccProdArray", Note = "No pointers: the offset PProdArray.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccRowArray", Note = "An int[] of row offsets (SSYaccTable.PPRowArray).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "SSYaccRowOffsetArray", Note = "Read from the table bytes after the table header.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccRowArray", Note = "No pointers: an int[] of row offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSYaccU", Symbol = "PSSYaccRowOffsetArray", Note = "No pointers: a byte offset.")]

namespace Bold;

/// <summary>The parser engine's constants.</summary>
public static partial class BoldSSYaccU
{
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableMaxSize = 65528;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableId = 0x53535943;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableRowFlagsSync = 0x0001;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableRowFlagsError = 0x0002;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableRowFlagsSyncAll = 0x0004;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableRowFlagsDuplicate = 0x0008;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableRowFlagsSyncPossible = 0x0005;
    [Delphi(PortStatus.Ported)] public const uint SSYaccPairTableEntryMask = 0xf8000000;
    [Delphi(PortStatus.Ported)] public const uint SSYaccPairTableEntrySync = 0x80000000;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableEntryShift = 0x40000000;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableEntryReduce = 0x20000000;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableEntryAccept = 0x10000000;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableEntryConflict = 0x08000000;
    [Delphi(PortStatus.Ported)] public const int SSYaccPairTableEntryMax = 0x07ffffff;
    [Delphi(PortStatus.Ported)] public const string SSYaccEofString = "eof";
    [Delphi(PortStatus.Ported)] public const string SSYaccErrorString = "error";
    [Delphi(PortStatus.Ported)] public const uint SSYaccEofToken = 0xffffffff;
    [Delphi(PortStatus.Ported)] public const uint SSYaccErrorToken = 0xfffffffe;
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgFileOpen = "SSYacc0101e: Open file failed, %s";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgParse = "SSYacc0102e: Parse failed, invalid table";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgEof = "SSYacc0103e: Attempted read past eof";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgMissingTable = "SSYacc0109e: Table missing, required";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgMissingLexer = "SSYacc0110e: Lexer missing, required";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgRead = "SSYacc0111e: Error reading table, %s";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgTableSize = "SSYacc0112e: Table too big";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgElement = "SSYacc0108e: Invalid index on ElementFromxxx";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgSyncErrToken = "SSYacc0105e: SyncErr failed, no valid token";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgSyncErrEof = "SSYacc0106e: SyncErr failed, eof";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgFindResource = "SSYacc0107e: Unable to locate resource, %s";
    [Delphi(PortStatus.Ported)] public const string SSYaccMsgBadResource = "SSYacc0108e: Invalid resource, %s";
}

/// <summary>A set of integers kept in insertion order.</summary>
[Delphi(PortStatus.Ported)]
public class SSSetOfLongint : TBoldMemoryManagedObject
{
    public int Incr;
    public int Size;
    public int Count;
    public readonly List<int> PArray = [];

    [Delphi(PortStatus.Ported)]
    public SSSetOfLongint(int TheSize, int TheInc)
    {
        Size = TheSize;
        Incr = TheInc;
        Count = 0;
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSSetOfLongint.Destroy", Note = "Freed the array; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public bool Insert(int TheItem)
    {
        if (PArray.Contains(TheItem))
        {
            return false;
        }

        if (Count >= Size)
        {
            Size += Incr;
        }

        PArray.Add(TheItem);
        Count++;
        return true;
    }

    [Delphi(PortStatus.Ported)]
    public bool Contains(int TheItem) => PArray.Contains(TheItem);
}

/// <summary>What the parser does next.</summary>
[Delphi(PortStatus.Ported)]
public enum SSYaccAction
{
    ShiftAction,
    ErrorAction,
    ReduceAction,
    AcceptAction,
    ConflictAction,
}

/// <summary>An element of the parse stack: a state and the lexeme shifted (or the result of a reduction, in subclasses).</summary>
[Delphi(PortStatus.Ported)]
public class SSYaccStackElement : TBoldMemoryManagedObject
{
    public SSLexLexeme? Lexeme;
    public int State;
    public int Use;

    [Delphi(PortStatus.Ported)]
    public SSYaccStackElement()
    {
        Lexeme = null;
        State = 0;
        Use = 0;
    }

    [Delphi(PortStatus.Ported, Symbol = "SSYaccStackElement.Destroy", Note = "Releases the lexeme's reference; nothing is freed by hand in .NET.")]
    public virtual void Release() => Lexeme?.RefDec();

    [Delphi(PortStatus.Ported)] public void RefInc() => Use++;

    [Delphi(PortStatus.Ported)]
    public bool RefDec()
    {
        Use--;
        return Use == 0;
    }

    [Delphi(PortStatus.Ported)]
    public void SetLexeme(SSLexLexeme? TheLexeme)
    {
        TheLexeme?.RefInc();
        Lexeme?.RefDec();
        Lexeme = TheLexeme;
    }
}

/// <summary>The parse stack; pushing takes a reference.</summary>
[Delphi(PortStatus.Ported)]
public class SSYaccStack : SSStack
{
    [Delphi(PortStatus.Ported)] public new SSYaccStackElement Top() => (SSYaccStackElement)base.Top()!;

    [Delphi(PortStatus.Ported)]
    public void Push(SSYaccStackElement TheElement)
    {
        TheElement.RefInc();
        base.Push(TheElement);
    }

    [Delphi(PortStatus.Ported, Symbol = "SSYaccStack.Destroy", Note = "Releases the elements' references; nothing is freed by hand in .NET.")]
    public void Release()
    {
        for (int i = 0; i < TopOfStack; i++)
        {
            var Element = (SSYaccStackElement)PArray[i]!;
            if (Element.RefDec())
            {
                Element.Release();
            }
        }
    }
}

/// <summary>A parser table: rows of actions and gotos per state, the productions, and the lookahead tables for conflicts.</summary>
[Delphi(PortStatus.Ported)]
public class SSYaccTable : TBoldMemoryManagedObject
{
    private const int TableHeaderSize = 20;
    private const int RowHeaderSize = 12;
    private const int EntrySize = 8;
    private const int ProdSize_ = 8;

    public List<SSLexExpressionList>? LarTableList;
    public int Size;
    public int[] PPRowArray = [];
    public byte[] PTable = [];
    public int PProdArray;

    /// <summary>Reads the table from a file; raises when it is missing, not a parser table, or too big.</summary>
    [Delphi(PortStatus.Ported)]
    public SSYaccTable(string FileName)
    {
        BoldSSLexU.SSAux();
        if (!File.Exists(FileName))
        {
            throw new Exception(SysUtils.Format(BoldCoreConsts.sFileDoesNotExist, FileName));
        }

        Load(File.ReadAllBytes(FileName), FileName, SSExceptionError.SSExceptionYaccRead, BoldSSYaccU.SSYaccMsgRead);
    }

    private SSYaccTable()
    {
    }

    /// <summary>Reads the table from a resource's bytes.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the resource's bytes (a manifest resource here) where Delphi takes a module handle and resource names.")]
    public static SSYaccTable CreateResource(byte[]? Resource, string TheName, string TheType)
    {
        BoldSSLexU.SSAux();
        if (Resource is null)
        {
            throw SSException.CreateName(SSExceptionError.SSExceptionYaccFindResource, BoldSSYaccU.SSYaccMsgFindResource, TheType);
        }

        var result = new SSYaccTable();
        result.Load(Resource, TheType, SSExceptionError.SSExceptionYaccBadResource, BoldSSYaccU.SSYaccMsgBadResource);
        return result;
    }

    private void Load(byte[] Data, string Name, SSExceptionError BadId, string BadMessage)
    {
        Size = Data.Length;
        PTable = Data;
        if (BoldSSLexU.ReadInt(PTable, 0) != BoldSSYaccU.SSYaccPairTableId)
        {
            PTable = [];
            throw SSException.CreateName(BadId, BadMessage, Name);
        }

        int States = BoldSSLexU.ReadInt(PTable, 8);
        int BaseSize = TableHeaderSize + ProdSize_ * Productions() + 4 * States;
        if (BaseSize > BoldSSYaccU.SSYaccPairTableMaxSize)
        {
            PTable = [];
            throw new SSException(SSExceptionError.SSExceptionYaccTableSize, BoldSSYaccU.SSYaccMsgTableSize);
        }

        PProdArray = BoldSSLexU.ReadInt(PTable, 16);
        PPRowArray = new int[States];
        int PLarTables = BoldSSLexU.ReadInt(PTable, 12);
        for (int i = 0; i < States; i++)
        {
            PPRowArray[i] = BoldSSLexU.ReadInt(PTable, TableHeaderSize + i * 4);
            PLarTables = PPRowArray[i] + RowSize(PPRowArray[i]);
        }

        if (LarTables() > 0)
        {
            GetLarTables(PLarTables, LarTables(), Name);
        }
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSYaccTable.Destroy", Note = "Freed the table; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public int LarTables() => (int)((uint)BoldSSLexU.ReadInt(PTable, 4) >> 16);

    [Delphi(PortStatus.Ported)]
    public int Productions() => BoldSSLexU.ReadInt(PTable, 4) & 0x0000ffff;

    /// <summary>The bytes of a row: its header and its entries (one more for an error row).</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the row's offset.")]
    public int RowSize(int TheRow)
    {
        int Entries = RowGotos(TheRow) + RowActions(TheRow);
        if ((RowFlags(TheRow) & BoldSSYaccU.SSYaccPairTableRowFlagsError) != 0)
        {
            Entries++;
        }

        return EntrySize * Entries + RowHeaderSize;
    }

    [Delphi(PortStatus.Ported, Note = "Takes the offset of the first lookahead table.")]
    public void GetLarTables(int TheBuffer, int TheNumber, string TheFile)
    {
        LarTableList = [];
        for (int i = 0; i < TheNumber; i++)
        {
            SSLexExpressionList ExprList = SSLexExpressionList.CreateBuffer(PTable, TheBuffer, TheFile);
            LarTableList.Insert(i, ExprList);
            TheBuffer += ExprList.Size;
        }
    }

    internal int RowFlags(int Row) => BoldSSLexU.ReadInt(PTable, Row);
    internal int RowGotos(int Row) => BoldSSLexU.ReadInt(PTable, Row + 4);
    internal int RowActions(int Row) => BoldSSLexU.ReadInt(PTable, Row + 8);

    /// <summary>The entry (action bits and target) and token of a row's entry.</summary>
    internal (int Entry, int Token) EntryAt(int Row, int Index)
    {
        int offset = Row + RowHeaderSize + Index * EntrySize;
        return (BoldSSLexU.ReadInt(PTable, offset), BoldSSLexU.ReadInt(PTable, offset + 4));
    }

    internal int ProdSize(int Production) => BoldSSLexU.ReadInt(PTable, PProdArray + Production * ProdSize_);
    internal int ProdLeftside(int Production) => BoldSSLexU.ReadInt(PTable, PProdArray + Production * ProdSize_ + 4);
}

/// <summary>The lexemes read ahead to resolve a conflict.</summary>
[Delphi(PortStatus.Ported, Note = "A List where Delphi derives from TList.")]
public class SSYaccLexemeCache : List<SSLexLexeme>
{
    [Delphi(PortStatus.Ported)]
    public SSYaccLexemeCache()
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSYaccLexemeCache.Destroy", Note = "Empty in Delphi.")]
    [Delphi(PortStatus.Ported, Note = "Takes the first lexeme; Delphi's line reading it is commented out, which leaves the result undefined.")]
    public SSLexLexeme? Dequeue()
    {
        if (Count == 0)
        {
            return null;
        }

        SSLexLexeme result = this[0];
        result.RefDec();
        RemoveAt(0);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void Enqueue(SSLexLexeme TheLexeme)
    {
        Add(TheLexeme);
        TheLexeme.RefInc();
    }

    [Delphi(PortStatus.Ported, Note = "Null beyond the end, where Delphi's TList raises.")]
    public SSLexLexeme? Get(int TheIndex) => Count == 0 || TheIndex == int.MaxValue || TheIndex >= Count ? null : this[TheIndex];
}

/// <summary>
/// The LALR parser: reads lexemes from the lexer and shifts, reduces and recovers from errors by the table.
/// Subclasses build the result in Reduce and report errors in Error.
/// </summary>
[Delphi(PortStatus.Ported)]
public class SSYacc : TBoldMemoryManagedObject
{
    public SSLex? Lex;
    public bool ErrorInd;
    public bool AbortInd;
    public bool EndOfInput;
    public int Cache;
    public int State;
    public int ShiftedSinceError;
    public int Leftside;
    public int Production;
    public int ProductionSize;
    public SSYaccTable Table;
    public SSLexLexeme? Lookahead;
    public SSLexLexeme? LarLookahead;
    public SSYaccStack Stack;
    public SSLexLexeme? EndLexeme;
    public SSYaccAction Action;
    public SSYaccLexemeCache LexemeCache;
    public SSYaccStackElement? Element;
    public SSLexExpressionList? ExprList;

    /// <summary>A parser reading from the lexer with the table (required); the stack starts with one element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "SSYacc.CreateLex")]
    public SSYacc(SSLex? TheLexer, SSYaccTable? TheTable)
    {
        EndLexeme = null;
        Lex = null;
        EndOfInput = false;
        ErrorInd = false;
        ShiftedSinceError = 0;
        AbortInd = false;
        if (TheTable is null)
        {
            throw new SSException(SSExceptionError.SSExceptionYaccMissingTable, BoldSSYaccU.SSYaccMsgMissingTable);
        }

        Lex = TheLexer;
        Table = TheTable;
        Stack = new SSYaccStack();
        Element = StackElement();
        Element.RefInc();
        Stack.Push(Element);
        LexemeCache = new SSYaccLexemeCache();
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSYacc.Destroy", Note = "Released the lexemes, stack and cache; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void SetEof()
    {
        EndOfInput = true;
        EndLexeme = new SSLexLexeme(BoldSSYaccU.SSYaccEofString, BoldSSYaccU.SSYaccEofString.Length, 0, 0) { Token = BoldSSYaccU.SSYaccEofToken };
        EndLexeme.RefInc();
    }

    /// <summary>Recovers from a syntax error: skips input to a token some state on the stack can sync on, then pops to that state.</summary>
    [Delphi(PortStatus.Ported)]
    public void SyncErr()
    {
        var SetOfToken = new SSSetOfLongint(16, 16);
        for (int i = 0; i < Stack.TopOfStack; i++)
        {
            var AnElement = (SSYaccStackElement)Stack.PArray[i]!;
            int PRow = Table.PPRowArray[AnElement.State];
            int flags = Table.RowFlags(PRow);
            if ((flags & BoldSSYaccU.SSYaccPairTableRowFlagsSyncPossible) != 0)
            {
                for (int j = 0; j < Table.RowActions(PRow); j++)
                {
                    (int entry, int token) = Table.EntryAt(PRow, j);
                    if (((uint)flags & BoldSSYaccU.SSYaccPairTableRowFlagsSyncAll) != 0 || ((uint)entry & BoldSSYaccU.SSYaccPairTableEntrySync) != 0)
                    {
                        SetOfToken.Insert(token);
                    }
                }
            }

            if ((flags & BoldSSYaccU.SSYaccPairTableRowFlagsError) != 0)
            {
                (int errorEntry, _) = Table.EntryAt(PRow, Table.RowActions(PRow) + Table.RowGotos(PRow));
                int PErrorRow = Table.PPRowArray[errorEntry & BoldSSYaccU.SSYaccPairTableEntryMax];
                for (int j = 0; j < Table.RowActions(PRow); j++)
                {
                    SetOfToken.Insert(Table.EntryAt(PErrorRow, j).Token);
                }
            }
        }

        if (SetOfToken.Count == 0)
        {
            throw new SSException(SSExceptionError.SSExceptionYaccSyncErrToken, BoldSSYaccU.SSYaccMsgSyncErrToken);
        }

        while (!SetOfToken.Contains(unchecked((int)Lookahead!.Token)))
        {
            if (DoGetLexeme(false))
            {
                throw new SSException(SSExceptionError.SSExceptionYaccSyncErrEof, BoldSSYaccU.SSYaccMsgSyncErrEof);
            }
        }

        while (true)
        {
            int PRow = Table.PPRowArray[State];
            int flags = Table.RowFlags(PRow);
            if ((flags & BoldSSYaccU.SSYaccPairTableRowFlagsError) != 0)
            {
                (int errorEntry, _) = Table.EntryAt(PRow, Table.RowActions(PRow) + Table.RowGotos(PRow));
                State = errorEntry & BoldSSYaccU.SSYaccPairTableEntryMax;
                LookupAction(unchecked((int)Lookahead!.Token));
                if (Action != SSYaccAction.ErrorAction)
                {
                    var ErrorLexeme = new SSLexLexeme(BoldSSYaccU.SSYaccErrorString, BoldSSYaccU.SSYaccErrorString.Length, 0, 0) { Token = BoldSSYaccU.SSYaccErrorToken };
                    SetElement(StackElement());
                    Element!.Lexeme = ErrorLexeme;
                    Element.State = State;
                    Stack.Push(Element);
                    break;
                }
            }

            if ((flags & BoldSSYaccU.SSYaccPairTableRowFlagsSyncPossible) != 0)
            {
                LookupAction(unchecked((int)Lookahead!.Token));
                if (Action != SSYaccAction.ErrorAction)
                {
                    break;
                }
            }

            Pop(1);
        }
    }

    /// <summary>Parses the input; true when it failed (an error, or aborted).</summary>
    [Delphi(PortStatus.Ported)]
    public bool Parse()
    {
        bool result = DoGetLexeme(true);
        if (result)
        {
            return result;
        }

        while (true)
        {
            if (AbortInd)
            {
                break;
            }

            switch (Action)
            {
                case SSYaccAction.ShiftAction:
                    result = DoShift();
                    if (result)
                    {
                        return result;
                    }

                    break;
                case SSYaccAction.ReduceAction:
                    result = DoReduce();
                    if (result)
                    {
                        return result;
                    }

                    break;
                case SSYaccAction.ErrorAction:
                    ErrorInd = true;
                    if (Error(State, Lookahead!))
                    {
                        return true;
                    }

                    ShiftedSinceError = 0;
                    break;
                case SSYaccAction.ConflictAction:
                    result = DoConflict();
                    if (result)
                    {
                        return result;
                    }

                    break;
                case SSYaccAction.AcceptAction:
                    return ErrorInd;
                default:
                    throw new SSException(SSExceptionError.SSExceptionYaccParse, BoldSSYaccU.SSYaccMsgParse);
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool DoShift()
    {
        SetElement(Shift());
        if (Element is null)
        {
            return true;
        }

        Element.SetLexeme(Lookahead);
        Element.State = State;
        Stack.Push(Element);
        bool result = DoGetLexeme(true);
        if (!result)
        {
            ShiftedSinceError++;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool DoReduce()
    {
        SetElement(Reduce(Production, ProductionSize));
        if (Element is null)
        {
            return true;
        }

        Pop(ProductionSize);
        DoGoto(Leftside);
        return false;
    }

    /// <summary>Resolves a conflict by reading ahead through the lookahead table; an error when it cannot.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DoConflict()
    {
        Cache = 0;
        int LarState = ExprList!.LookupState(unchecked((int)Lookahead!.Token), 0);
        SetLarLookahead(GetLexemeCache());
        while (LarLookahead is not null)
        {
            LarState = ExprList.LookupState(unchecked((int)LarLookahead.Token), LarState);
            if (State == BoldSSLexU.SSLexStateInvalid)
            {
                break;
            }

            SSLexFinalState PFinal = ExprList.LookupFinal(LarState);
            if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsFinal) != 0)
            {
                if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsReduce) != 0)
                {
                    Production = PFinal.Token;
                    Leftside = Table.ProdLeftside(Production);
                    ProductionSize = Table.ProdSize(Production);
                    return DoReduce();
                }

                State = PFinal.Token;
                return DoShift();
            }

            SetLarLookahead(GetLexemeCache());
        }

        return DoLarError();
    }

    [Delphi(PortStatus.Ported)]
    public bool DoLarError()
    {
        ErrorInd = true;
        bool result = LarError(State, Lookahead!, LarLookahead);
        if (!result)
        {
            ShiftedSinceError = 0;
        }

        return result;
    }

    /// <summary>Pops the elements of a production and takes the state below them.</summary>
    [Delphi(PortStatus.Ported)]
    public void Pop(int TheNumber)
    {
        for (int i = 0; i < TheNumber; i++)
        {
            SSYaccStackElement TopElement = Stack.Top();
            Stack.Pop();
            if (TopElement.RefDec())
            {
                TopElement.Release();
            }
        }

        State = Stack.Top().State;
    }

    [Delphi(PortStatus.Ported)]
    public void DoGoto(int TheGoto)
    {
        LookupGoto(Leftside);
        Element!.State = State;
        Stack.Push(Element);
        LookupAction(unchecked((int)Lookahead!.Token));
    }

    [Delphi(PortStatus.Ported)]
    public SSLexLexeme GetLexemeCache()
    {
        SSLexLexeme? result = null;
        if (Cache != int.MaxValue)
        {
            result = LexemeCache.Get(Cache);
            Cache++;
        }

        if (result is null)
        {
            Cache = int.MaxValue;
            result = NextLexeme();
            if (result is null)
            {
                SetEof();
                result = EndLexeme!;
            }
        }

        LexemeCache.Enqueue(result);
        return result;
    }

    /// <summary>The goto state for the nonterminal; raises when the table has none.</summary>
    [Delphi(PortStatus.Ported)]
    public void LookupGoto(int TheGoto)
    {
        int PCurrentRow = Table.PPRowArray[State];
        int actions = Table.RowActions(PCurrentRow);
        int EndIndex = actions + Table.RowGotos(PCurrentRow) - 1;
        for (int i = actions; i <= EndIndex; i++)
        {
            (int entry, int token) = Table.EntryAt(PCurrentRow, i);
            if (token == TheGoto)
            {
                State = entry & BoldSSYaccU.SSYaccPairTableEntryMax;
                return;
            }
        }

        throw new SSException(SSExceptionError.SSExceptionYaccParse, BoldSSYaccU.SSYaccMsgParse);
    }

    /// <summary>The action for the token in the current state, with its target state, production or lookahead table.</summary>
    [Delphi(PortStatus.Ported)]
    public void LookupAction(int TheToken)
    {
        int PCurrentRow = Table.PPRowArray[State];
        int actions = Table.RowActions(PCurrentRow);
        for (int i = 0; i < actions; i++)
        {
            (int entry, int token) = Table.EntryAt(PCurrentRow, i);
            if (token == TheToken)
            {
                Action = GetAction(entry);
                switch (Action)
                {
                    case SSYaccAction.ShiftAction:
                        State = entry & BoldSSYaccU.SSYaccPairTableEntryMax;
                        break;
                    case SSYaccAction.ReduceAction:
                        Production = entry & BoldSSYaccU.SSYaccPairTableEntryMax;
                        Leftside = Table.ProdLeftside(Production);
                        ProductionSize = Table.ProdSize(Production);
                        break;
                    case SSYaccAction.ConflictAction:
                        ExprList = Table.LarTableList![entry & BoldSSYaccU.SSYaccPairTableEntryMax];
                        break;
                }

                return;
            }
        }

        Action = SSYaccAction.ErrorAction;
    }

    [Delphi(PortStatus.Ported)]
    public bool GetLexeme(bool Look)
    {
        if (EndOfInput)
        {
            throw new SSException(SSExceptionError.SSExceptionYaccEof, BoldSSYaccU.SSYaccMsgEof);
        }

        SetLookahead(NextLexeme());
        if (Lookahead is null)
        {
            SetEof();
            SetLookahead(EndLexeme);
        }

        if (Look)
        {
            LookupAction(unchecked((int)Lookahead!.Token));
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    public bool DoGetLexeme(bool Look)
    {
        SetLookahead(LexemeCache.Dequeue());
        if (Lookahead is not null)
        {
            bool result = LarLook(Lookahead);
            if (!result && Look)
            {
                LookupAction(unchecked((int)Lookahead.Token));
            }

            return result;
        }

        return GetLexeme(Look);
    }

    [Delphi(PortStatus.Ported)]
    public void SetLookahead(SSLexLexeme? TheLexeme)
    {
        TheLexeme?.RefInc();
        Lookahead?.RefDec();
        Lookahead = TheLexeme;
    }

    [Delphi(PortStatus.Ported)]
    public void SetLarLookahead(SSLexLexeme? TheLexeme)
    {
        TheLexeme?.RefInc();
        LarLookahead?.RefDec();
        LarLookahead = TheLexeme;
    }

    [Delphi(PortStatus.Ported)]
    public void SetElement(SSYaccStackElement? TheElement)
    {
        TheElement?.RefInc();
        if (Element is not null && Element.RefDec())
        {
            Element.Release();
        }

        Element = TheElement;
    }

    [Delphi(PortStatus.Ported, Note = "Takes the entry's action bits where Delphi takes a pointer to the entry.")]
    public SSYaccAction GetAction(int PEntry) =>
        (PEntry & BoldSSYaccU.SSYaccPairTableEntryShift) != 0 ? SSYaccAction.ShiftAction
        : (PEntry & BoldSSYaccU.SSYaccPairTableEntryReduce) != 0 ? SSYaccAction.ReduceAction
        : (PEntry & BoldSSYaccU.SSYaccPairTableEntryAccept) != 0 ? SSYaccAction.AcceptAction
        : (PEntry & BoldSSYaccU.SSYaccPairTableEntryConflict) != 0 ? SSYaccAction.ConflictAction
        : SSYaccAction.ErrorAction;

    /// <summary>The element of the production being reduced (0 is its first symbol).</summary>
    [Delphi(PortStatus.Ported)]
    public SSYaccStackElement ElementFromProduction(int TheIndex)
    {
        TheIndex = Stack.TopOfStack - ProductionSize + TheIndex;
        if (TheIndex < 0 || TheIndex >= Stack.TopOfStack)
        {
            throw new SSException(SSExceptionError.SSExceptionYaccElement, BoldSSYaccU.SSYaccMsgElement);
        }

        return (SSYaccStackElement)Stack.PArray[TheIndex]!;
    }

    [Delphi(PortStatus.Ported)]
    public virtual SSLexLexeme? NextLexeme() =>
        Lex is null ? throw new SSException(SSExceptionError.SSExceptionYaccMissingLexer, BoldSSYaccU.SSYaccMsgMissingLexer) : Lex.Next();

    [Delphi(PortStatus.Ported)] public virtual SSYaccStackElement? Shift() => StackElement();
    [Delphi(PortStatus.Ported)] public virtual SSYaccStackElement StackElement() => new();
    [Delphi(PortStatus.Ported)] public virtual bool LarLook(SSLexLexeme TheLexeme) => false;

    /// <summary>Recovers (SyncErr) and goes on; subclasses report the error and may stop.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool Error(int TheState, SSLexLexeme TheLookahead)
    {
        SyncErr();
        return false;
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool LarError(int TheState, SSLexLexeme TheLookahead, SSLexLexeme? TheLarLookahead) => Error(TheState, TheLookahead);

    /// <summary>The element for a reduction; null (stop) here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual SSYaccStackElement? Reduce(int TheProduction, int TheProductionSize) => null;
}
