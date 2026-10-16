// Delphi: Source/ObjectSpace/Ocl/BoldSSLexU.pas
// Port map: CSharp/port/units/BoldSSLexU.md
// The Sandstone table-driven lexer engine. Delphi reads the generated tables into memory and walks them with
// pointers; here a table is a byte array read little-endian at offsets, with the same layout.

using System.Buffers.Binary;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSArrayOfPointer", Note = "SSStack keeps a List<object?>.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSArrayOfPointer", Note = "No pointers; SSStack keeps a List<object?>.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexTableBase", Note = "Read from the table bytes: Size at 0, TableType at 4, 36 bytes in all (SSLexTable.BaseSize).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexTableHeader", Note = "Read from the table bytes: TableType 0, Size 4, Rows 40, Push 44, Index 48, Final 52 (SSLexExpressionList).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexTableRow", Note = "Read from the table bytes: a row is its entry count followed by the entries.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexTableRowEntry", Note = "Read from the table bytes: StartPoint, EndPoint, State (12 bytes).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexTableIndex", Note = "Read from the table bytes as the row offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexTableBase", Note = "No pointers: byte offsets into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexTableHeader", Note = "No pointers: byte offsets into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexTableRow", Note = "No pointers: byte offsets into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexTableRowEntry", Note = "No pointers: byte offsets into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexTableIndex", Note = "No pointers: byte offsets into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexFinalState", Note = "LookupFinal returns the SSLexFinalState struct read from the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexRowArray", Note = "An int[] of row offsets (SSLexExpressionList.PPRows).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexFinalArray", Note = "Read from the table bytes at PFinalStates.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexRowOffsetArray", Note = "Read from the table bytes at Index.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "SSLexRowEntryArray", Note = "Read from the table bytes after each row's count.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexRowArray", Note = "No pointers: an int[] of row offsets.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexFinalArray", Note = "No pointers: a byte offset into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexRowOffsetArray", Note = "No pointers: a byte offset into the table.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSSLexU", Symbol = "PSSLexRowEntryArray", Note = "No pointers: a byte offset into the table.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSSLexU", Symbol = "SSLexFileConsumer", Note = "Its methods are commented out in Delphi; it adds only a Handle field.")]

namespace Bold;

/// <summary>The lexer engine's constants and helpers.</summary>
public static partial class BoldSSLexU
{
    [Delphi(PortStatus.Ported)] public const int SSHugeIncVal = 8;
    [Delphi(PortStatus.Ported)] public const int SSLexMaxRead = 0x7fffffff;
    [Delphi(PortStatus.Ported)] public const int SSLexMaxCopy = 0x7fffffff;
    [Delphi(PortStatus.Ported)] public const int SSLexConsumerBof = -1;
    [Delphi(PortStatus.Ported)] public const int SSLexMaxBlock = 0x7fffffff - 1;
    [Delphi(PortStatus.Ported)] public const int SSLexStateInvalid = -1;
    [Delphi(PortStatus.Ported)] public const int SSLexBufferMax = 0x7fffffff;
    [Delphi(PortStatus.Ported)] public const int SSLexThreshold = 65536;
    [Delphi(PortStatus.Ported)] public const int SSLexStructMax = 65520;
    [Delphi(PortStatus.Ported)] public const int SSLexPairTable = 0x53534C58;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsContextStart = 0x01;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsStartOfLine = 0x02;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsPop = 0x08;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsFinal = 0x10;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsPush = 0x20;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsIgnore = 0x40;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsContextEnd = 0x80;
    [Delphi(PortStatus.Ported)] public const int SSLexFinalStateFlagsReduce = 0x100;
    [Delphi(PortStatus.Ported)] public const string SSLexMsgFileOpen = "SSLex0101e: Error opening file, %s";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgFileRead = "SSLex0102e: Invalid file length or read error, %s";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgLexemeLength = "SSLex0103e: Lexeme too long, %ld";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgBadTable = "SSLex0104e: Invalid table, %s";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgError = "SSLex0105e: Invalid token, Line %ld, Offset %ld, %s";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgBadList = "SSLex0106e: Invalid expression list index, %ld";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgMissingPart = "SSLex0107e: Table or consumer missing, required";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgFindResource = "SSLex0108e: Unable to locate resource, %s";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgBadResource = "SSLex0109e: Bad resource format, %s";
    [Delphi(PortStatus.Ported)] public const string SSLifoMsgStackTop = "SSLifo0002e: No top, stack empty";
    [Delphi(PortStatus.Ported)] public const string SSLifoMsgStackPop = "SSLifo0004e: Cannot pop empty stack";
    [Delphi(PortStatus.Ported)] public const string SSLexMsgOutOfMemory = "SSLex1000e: Out of memory";

    /// <summary>A hook for auxiliary code (SSAUX); empty.</summary>
    [Delphi(PortStatus.Ported)]
    public static void SSAux()
    {
    }

    /// <summary>A pointer plus an offset; here the offset added to a table offset.</summary>
    [Delphi(PortStatus.Ported, Note = "Offsets into a byte array stand for pointers.")]
    public static int SSHugeInc(int ThePointer, int TheInc) => ThePointer + TheInc;

    [Delphi(PortStatus.Ported, Note = "Copies between byte arrays at offsets.")]
    public static void SSHugeCopy(byte[] TheCopyTo, int ToOffset, byte[] TheCopyFrom, int FromOffset, int TheLength) =>
        Buffer.BlockCopy(TheCopyFrom, FromOffset, TheCopyTo, ToOffset, TheLength);

    /// <summary>A little-endian 32-bit integer of a table.</summary>
    internal static int ReadInt(byte[] Buffer, int Offset) => BinaryPrimitives.ReadInt32LittleEndian(Buffer.AsSpan(Offset, 4));
}

/// <summary>A growing stack of objects.</summary>
[Delphi(PortStatus.Ported)]
public class SSStack : TBoldMemoryManagedObject
{
    public int Incr;
    public int Size;
    public int TopOfStack;
    public readonly List<object?> PArray = [];

    [Delphi(PortStatus.Ported)]
    public SSStack()
    {
        Size = 32;
        Incr = 32;
        TopOfStack = 0;
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSStack.Destroy", Note = "Freed the pointer array; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void Pop()
    {
        if (TopOfStack == 0)
        {
            throw new SSException(SSExceptionError.SSExceptionStackPop, BoldSSLexU.SSLifoMsgStackPop);
        }

        TopOfStack--;
        PArray.RemoveAt(TopOfStack);
    }

    [Delphi(PortStatus.Ported)]
    public object? Top() => TopOfStack == 0 ? throw new SSException(SSExceptionError.SSExceptionStackTop, BoldSSLexU.SSLifoMsgStackTop) : PArray[TopOfStack - 1];

    [Delphi(PortStatus.Ported)]
    public void Push(object? ThePointer)
    {
        if (TopOfStack >= Size)
        {
            Size += Incr;
        }

        PArray.Add(ThePointer);
        TopOfStack++;
    }
}

/// <summary>What a final state of the lexer automaton says: the token, the list to push and the flags.</summary>
[Delphi(PortStatus.Ported)]
public readonly record struct SSLexFinalState(int Token, int Push, int Flags);

/// <summary>One expression list (start condition) of a lexer table: the automaton's rows and final states.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexExpressionList : TBoldMemoryManagedObject
{
    public byte[]? Buffer;
    public int Size;
    public int Index;
    public int Final;
    public int Rows;
    public int[] PPRows = [];
    public int PFinalStates;

    internal const int HeaderSize = 88;
    internal const int FinalStateSize = 28;

    [Delphi(PortStatus.Ported)]
    public SSLexExpressionList()
    {
        Buffer = null;
    }

    /// <summary>The expression list whose header starts at the offset in the bytes.</summary>
    [Delphi(PortStatus.Ported, Symbol = "SSLexExpressionList.CreateBuffer")]
    public static SSLexExpressionList CreateBuffer(byte[] TheHeader, int HeaderOffset, string TheFile)
    {
        if (BoldSSLexU.ReadInt(TheHeader, HeaderOffset) != BoldSSLexU.SSLexPairTable)
        {
            throw SSException.CreateName(SSExceptionError.SSExceptionLexBadTable, BoldSSLexU.SSLexMsgBadTable, TheFile);
        }

        var result = new SSLexExpressionList();
        result.ReadHeader(TheHeader, HeaderOffset);
        result.Open();
        return result;
    }

    /// <summary>Copies the list (its size from the header) and reads the header fields.</summary>
    internal void ReadHeader(byte[] Source, int HeaderOffset)
    {
        Size = BoldSSLexU.ReadInt(Source, HeaderOffset + 4);
        Rows = BoldSSLexU.ReadInt(Source, HeaderOffset + 40);
        Index = BoldSSLexU.ReadInt(Source, HeaderOffset + 48);
        Final = BoldSSLexU.ReadInt(Source, HeaderOffset + 52);
        Buffer = new byte[Size];
        BoldSSLexU.SSHugeCopy(Buffer, 0, Source, HeaderOffset, Size);
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSLexExpressionList.Destroy", Note = "Freed the buffer; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void Open()
    {
        PFinalStates = Final;
        PPRows = new int[Rows];
        for (int i = 0; i < Rows; i++)
        {
            PPRows[i] = BoldSSLexU.ReadInt(Buffer!, Index + i * 4);
        }
    }

    /// <summary>The next state for the character from the state, or SSLexStateInvalid.</summary>
    [Delphi(PortStatus.Ported)]
    public int LookupState(int TheToken, int TheState)
    {
        int row = PPRows[TheState];
        int count = BoldSSLexU.ReadInt(Buffer!, row);
        for (int i = 0; i < count; i++)
        {
            int entry = row + 4 + i * 12;
            int startPoint = BoldSSLexU.ReadInt(Buffer!, entry);
            if (TheToken == BoldSSLexU.SSLexConsumerBof)
            {
                if (startPoint == BoldSSLexU.SSLexConsumerBof)
                {
                    return BoldSSLexU.ReadInt(Buffer!, entry + 8);
                }
            }
            else if (TheToken < startPoint)
            {
                break;
            }
            else if (TheToken <= BoldSSLexU.ReadInt(Buffer!, entry + 4))
            {
                return BoldSSLexU.ReadInt(Buffer!, entry + 8);
            }
        }

        return BoldSSLexU.SSLexStateInvalid;
    }

    [Delphi(PortStatus.Ported)]
    public SSLexFinalState LookupFinal(int TheState)
    {
        int offset = PFinalStates + TheState * FinalStateSize;
        return new SSLexFinalState(BoldSSLexU.ReadInt(Buffer!, offset), BoldSSLexU.ReadInt(Buffer!, offset + 4), BoldSSLexU.ReadInt(Buffer!, offset + 8));
    }
}

/// <summary>A stack of expression lists.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexExpressionListStack : SSStack
{
    [Delphi(PortStatus.Ported)] public new SSLexExpressionList Top() => (SSLexExpressionList)base.Top()!;
    [Delphi(PortStatus.Ported)] public void Push(SSLexExpressionList TheList) => base.Push(TheList);
}

/// <summary>A lexer table: its expression lists, read from a file or from a resource's bytes.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexTable : TBoldMemoryManagedObject
{
    internal const int BaseSize = 36;

    public readonly List<SSLexExpressionList> ExpressionLists = [];
    public int NumLists;

    /// <summary>Reads the table from a file; raises when it is missing or not a lexer table.</summary>
    [Delphi(PortStatus.Ported)]
    public SSLexTable(string FileName)
    {
        BoldSSLexU.SSAux();
        if (!File.Exists(FileName))
        {
            throw new Exception(SysUtils.Format(BoldCoreConsts.sFileDoesNotExist, FileName));
        }

        Load(File.ReadAllBytes(FileName), FileName, SSExceptionError.SSExceptionLexBadTable, BoldSSLexU.SSLexMsgBadTable);
    }

    private SSLexTable()
    {
    }

    /// <summary>Reads the table from a resource's bytes; raises when there are none or they are not a lexer table.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the resource's bytes (a manifest resource here) where Delphi takes a module handle and resource names.")]
    public static SSLexTable CreateResource(byte[]? Resource, string TheName, string TheType)
    {
        BoldSSLexU.SSAux();
        if (Resource is null)
        {
            throw SSException.CreateName(SSExceptionError.SSExceptionLexFindResource, BoldSSLexU.SSLexMsgFindResource, TheType);
        }

        var result = new SSLexTable();
        result.Load(Resource, TheType, SSExceptionError.SSExceptionLexBadResource, BoldSSLexU.SSLexMsgBadResource);
        return result;
    }

    private void Load(byte[] Data, string Name, SSExceptionError BadId, string BadMessage)
    {
        if (BoldSSLexU.ReadInt(Data, 4) != BoldSSLexU.SSLexPairTable)
        {
            throw SSException.CreateName(BadId, BadMessage, Name);
        }

        NumLists = BoldSSLexU.ReadInt(Data, 0);
        int current = BaseSize;
        for (int i = 0; i < NumLists; i++)
        {
            var ExprList = new SSLexExpressionList();
            ExprList.ReadHeader(Data, current);
            ExprList.Open();
            ExpressionLists.Insert(i, ExprList);
            current += ExprList.Size;
        }
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSLexTable.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public SSLexExpressionList GetExpressionList(int TheList) =>
        TheList < 0 || TheList >= ExpressionLists.Count
            ? throw SSException.CreateLong(SSExceptionError.SSExceptionBadList, BoldSSLexU.SSLexMsgBadList, TheList)
            : ExpressionLists[TheList];
}

/// <summary>A token's text, position and token number, reference counted by the parser's stack.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexLexeme : TBoldMemoryManagedObject
{
    public string Buffer;
    public int Use;
    public int Line;
    public int Offset;
    public int Length;
    public uint Token;

    [Delphi(PortStatus.Ported)]
    public SSLexLexeme(string TheData, int TheLength, int TheLine, int TheOffset)
    {
        Buffer = TheData.Length > TheLength ? TheData[..TheLength] : TheData;
        Length = TheLength;
        Line = TheLine;
        Offset = TheOffset;
        Use = 0;
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSLexLexeme.Destroy", Note = "Freed the text; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void RefInc()
    {
        System.Diagnostics.Debug.Assert(Use >= 0);
        Use++;
    }

    [Delphi(PortStatus.Ported)]
    public bool RefDec()
    {
        Use--;
        System.Diagnostics.Debug.Assert(Use >= 0);
        return Use == 0;
    }
}

/// <summary>Binary or text input.</summary>
[Delphi(PortStatus.Ported)]
public enum SSLexConsumerMode
{
    SSLexBinaryMode,
    SSLexTextMode,
}

/// <summary>The characters a lexer reads, with the marks of the lexeme being scanned and its line and offset.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexConsumer : TBoldMemoryManagedObject
{
    public string Buffer = "";
    public int Bof;
    public int Line;
    public int Offset;
    public int BuffLen;
    public int BuffInc;
    public int Current;
    public int MarkLine;
    public int MarkOffset;
    public int ScanLine;
    public int ScanOffset;
    public bool EndOfData;
    public int Start;
    public int Index;
    public int DataLen;
    public int Mark;
    public int MarkContext;
    public int MarkContextLine;
    public int MarkContextOffset;
    public SSLexConsumerMode Mode;

    [Delphi(PortStatus.Ported)]
    public SSLexConsumer(int TheIncrement, SSLexConsumerMode TheMode)
    {
        BuffInc = TheIncrement;
        ScanLine = 0;
        ScanOffset = 0;
        Line = 1;
        Offset = 1;
        Bof = BoldSSLexU.SSLexConsumerBof;
        Mode = TheMode;
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSLexConsumer.Destroy", Note = "Freed the buffer; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void MarkFinal()
    {
        Mark = Index;
        MarkLine = ScanLine;
        MarkOffset = ScanOffset;
    }

    [Delphi(PortStatus.Ported)]
    public void MarkContextFinal()
    {
        MarkContext = Index;
        MarkContextLine = ScanLine;
        MarkContextOffset = ScanOffset;
    }

    [Delphi(PortStatus.Ported)]
    public void SetContextFinal()
    {
        Mark = MarkContext;
        MarkLine = MarkContextLine;
        MarkOffset = MarkContextOffset;
    }

    [Delphi(PortStatus.Ported)]
    public void FlushStartOfLine()
    {
        Start++;
        Line++;
        MarkLine--;
        MarkContextLine--;
        Offset = 1;
    }

    [Delphi(PortStatus.Ported)]
    public void FlushLexeme()
    {
        Start = Mark;
        Index = Mark;
        Line += MarkLine;
        Offset = MarkOffset;
        ScanLine = 0;
        ScanOffset = MarkOffset;
    }

    [Delphi(PortStatus.Ported)]
    public void FlushLexemeAll()
    {
        Start = Index;
        Line += ScanLine;
        Offset = ScanOffset;
        ScanLine = 0;
    }

    /// <summary>Reads the next character into Current (the beginning-of-file marker first); false at the end.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool Next()
    {
        if (EndOfData)
        {
            return false;
        }

        if (Bof != 0)
        {
            Current = Bof;
            Bof = 0;
            return true;
        }

        bool result = true;
        if (Index >= DataLen)
        {
            int NewLen = NextBuffer();
            if (NewLen == 0)
            {
                EndOfData = true;
                result = false;
            }
            else
            {
                DataLen += NewLen;
            }
        }

        if (result)
        {
            Current = Buffer[Index];
            Index++;
            if (Current == 0x0A)
            {
                ScanLine++;
                ScanOffset = 1;
            }
            else
            {
                ScanOffset++;
            }
        }

        return result;
    }

    /// <summary>More input; none here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual int NextBuffer() => 0;

    /// <summary>The lexeme up to the final mark, and moves past it.</summary>
    [Delphi(PortStatus.Ported)]
    public SSLexLexeme Lexeme()
    {
        int Length = Mark - Start;
        var result = new SSLexLexeme(Buffer.Substring(Start, Length), Length, Line, Offset);
        FlushLexeme();
        return result;
    }

    /// <summary>Everything scanned since the start, and moves past it.</summary>
    [Delphi(PortStatus.Ported)]
    public SSLexLexeme LexemeAll()
    {
        var result = new SSLexLexeme(Buffer.Substring(Start, Index - Start), Index - Start, Line, Offset);
        FlushLexemeAll();
        return result;
    }
}

/// <summary>Input from a string.</summary>
[Delphi(PortStatus.Ported)]
public class SSLexStringConsumer : SSLexConsumer
{
    [Delphi(PortStatus.Ported)]
    public SSLexStringConsumer(string TheString)
        : base(0, SSLexConsumerMode.SSLexBinaryMode)
    {
        DataLen = TheString.Length;
        BuffLen = DataLen;
        Buffer = TheString;
    }
}

/// <summary>The lexer: runs the current expression list's automaton over the input and gives the next lexeme.</summary>
[Delphi(PortStatus.Ported)]
public class SSLex : TBoldMemoryManagedObject
{
    public int State;
    public SSLexTable Table;
    public SSLexConsumer Consumer;
    public SSLexExpressionList? List;
    public SSLexExpressionListStack? Stack;

    /// <summary>A lexer on the consumer with the table, starting in expression list 0; both are required.</summary>
    [Delphi(PortStatus.Ported)]
    public SSLex(SSLexConsumer? TheConsumer, SSLexTable? TheTable)
    {
        Stack = null;
        if (TheTable is null || TheConsumer is null)
        {
            throw new SSException(SSExceptionError.SSExceptionMissingPart, BoldSSLexU.SSLexMsgMissingPart);
        }

        Table = TheTable;
        Consumer = TheConsumer;
        Stack = new SSLexExpressionListStack();
        PushExpressionList(0);
    }

    [Delphi(PortStatus.Replaced, Symbol = "SSLex.Destroy", Note = "Freed the stack; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public void PopExpressionList()
    {
        Stack!.Pop();
        List = Stack.Top();
    }

    /// <summary>The next lexeme, or null at the end of the input; ignored lexemes are skipped, invalid input goes to Error.</summary>
    [Delphi(PortStatus.Ported)]
    public SSLexLexeme? Next()
    {
        SSLexLexeme? result;
        while (true)
        {
            State = 0;
            result = null;
            bool Consumed = false;
            SSLexFinalState PFinal = List!.LookupFinal(State);
            while (Consumer.Next())
            {
                Consumed = true;
                State = List.LookupState(Consumer.Current, State);
                if (State == BoldSSLexU.SSLexStateInvalid)
                {
                    break;
                }

                SSLexFinalState PTempFinal = List.LookupFinal(State);
                if ((PTempFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsFinal) != 0)
                {
                    Consumer.MarkFinal();
                    PFinal = PTempFinal;
                }

                if ((PTempFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsContextStart) != 0)
                {
                    Consumer.MarkContextFinal();
                }
            }

            if (!Consumed)
            {
                break;
            }

            if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsContextEnd) != 0)
            {
                Consumer.SetContextFinal();
            }

            if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsIgnore) != 0)
            {
                Consumer.FlushLexeme();
                ProcessExpressionList(PFinal);
                continue;
            }

            if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsFinal) == 0)
            {
                result = Error();
                if (result is not null)
                {
                    break;
                }

                Consumer.FlushLexemeAll();
                continue;
            }

            ProcessExpressionList(PFinal);
            if ((PFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsStartOfLine) != 0 && Consumer.Line != 1 && Consumer.Offset != 1)
            {
                Consumer.FlushStartOfLine();
            }

            result = Complete(PFinal.Token);
            if (result is not null)
            {
                break;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void PushExpressionList(int TheList)
    {
        Stack!.Push(Table.GetExpressionList(TheList));
        List = Stack.Top();
    }

    [Delphi(PortStatus.Ported)]
    public void GotoExpressionList(int TheList)
    {
        Stack!.Pop();
        PushExpressionList(TheList);
    }

    [Delphi(PortStatus.Ported, Note = "Takes the final state as a value where Delphi takes a pointer to it.")]
    public void ProcessExpressionList(SSLexFinalState ThePFinal)
    {
        bool pop = (ThePFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsPop) != 0;
        bool push = (ThePFinal.Flags & BoldSSLexU.SSLexFinalStateFlagsPush) != 0;
        if (pop && push)
        {
            GotoExpressionList(ThePFinal.Push);
        }
        else if (pop)
        {
            PopExpressionList();
        }
        else if (push)
        {
            PushExpressionList(ThePFinal.Push);
        }
    }

    /// <summary>Raises the invalid-token error with the line and offset.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual SSLexLexeme? Error()
    {
        SSLexLexeme Lexeme = Consumer.LexemeAll();
        throw SSException.CreateLongLongNameLen(SSExceptionError.SSExceptionLexError, BoldSSLexU.SSLexMsgError, Lexeme.Line, Lexeme.Offset, Lexeme.Buffer,
            Lexeme.Length);
    }

    [Delphi(PortStatus.Ported)]
    public virtual SSLexLexeme? Complete(int TheToken)
    {
        SSLexLexeme result = Consumer.Lexeme();
        result.Token = unchecked((uint)TheToken);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public virtual string TokenToString(int TheToken) => "";
}
