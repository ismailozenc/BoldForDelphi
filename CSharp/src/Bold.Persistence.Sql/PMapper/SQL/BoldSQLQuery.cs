// Delphi: Source/PMapper/SQL/BoldSQLQuery.pas
// Port map: CSharp/port/units/BoldSQLQuery.md
// A SQL select under construction: table references with aliases, joins, columns to retrieve and order by, and where
// clause fragments (WCFs) that render themselves, with parameters for floats and dates.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLTableReference.Destroy", Note = "Freed the column references; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLWCFBinary.Destroy", Note = "Freed the arguments; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLWCFUnary.Destroy", Note = "Freed the argument; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLWCFWithQuery.Destroy", Note = "Freed the query; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLWCFInQuery.Destroy", Note = "Freed the argument (not the query); the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLNameSpace.Destroy", Note = "Freed the name list; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLQuery", Symbol = "TBoldSQLQuery.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldSQLQueryMode { qmSelect, qmInsert, qmUpdate, qmDelete }

/// <summary>An equality join between two column references.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLJoin
{
    internal readonly TBoldSQLColumnReference fColumnRef1;
    internal readonly TBoldSQLColumnReference fColumnRef2;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLJoin(TBoldSQLColumnReference ColumnRef1, TBoldSQLColumnReference ColumnRef2)
    {
        fColumnRef1 = ColumnRef1;
        fColumnRef2 = ColumnRef2;
    }

    [Delphi(PortStatus.Ported)]
    private string getJoinStatement() => SysUtils.Format("%s.%s = %s.%s",
        fColumnRef1.TableReference.AliasName, fColumnRef1.ColumnDescription.SQLName,
        fColumnRef2.TableReference.AliasName, fColumnRef2.ColumnDescription.SQLName);

    [Delphi(PortStatus.Ported)]
    private bool OrderedMatches(TBoldSQLColumnReference ColumnRef1, TBoldSQLColumnReference ColumnRef2) =>
        ColumnRef1.TableReference == fColumnRef1.TableReference && ColumnRef2.TableReference == fColumnRef2.TableReference &&
        ColumnRef1.ColumnDescription == fColumnRef1.ColumnDescription && ColumnRef2.ColumnDescription == fColumnRef2.ColumnDescription;

    [Delphi(PortStatus.Ported)]
    public bool Matches(TBoldSQLColumnReference ColumnRef1, TBoldSQLColumnReference ColumnRef2) =>
        OrderedMatches(ColumnRef1, ColumnRef2) || OrderedMatches(ColumnRef2, ColumnRef1);

    [Delphi(PortStatus.Ported)] public string JoinStatement => getJoinStatement();
}

/// <summary>A table in a query, with its alias and the references to its columns.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLTableReference
{
    internal TBoldSQLTableDescription? fTableDescription;
    private readonly TBoldObjectArray fColumnReferences;
    private readonly TBoldSQLQuery fQuery;
    internal string fAliasName = "";

    [Delphi(PortStatus.Ported)]
    public TBoldSQLTableReference(TBoldSQLQuery Query)
    {
        fColumnReferences = new TBoldObjectArray(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));
        fQuery = Query;
    }

    [Delphi(PortStatus.Ported)]
    private string GetAliasName()
    {
        if (fAliasName.Length == 0)
        {
            fAliasName = fQuery.GetUniqueAlias(TableDescription.SQLName);
        }

        return fAliasName;
    }

    [Delphi(PortStatus.Ported)]
    private string GetTableAliasDeclaration() => SysUtils.Format("%s %s", TableDescription.SQLName, AliasName);

    [Delphi(PortStatus.Ported)]
    private void EnsureColumnExists(string ColumnName, string Operation)
    {
        if (TableDescription.ColumnsList.ItemsBySQLName(ColumnName) is null)
        {
            throw new EBoldInternal(SysUtils.Format("%s: Table %s does not contain column %s", Operation, TableDescription.SQLName, ColumnName));
        }
    }

    /// <summary>The reference to a column of the table (one per column).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSQLColumnReference GetColumnReference(string ColumnName)
    {
        EnsureColumnExists(ColumnName, "AddColumnReference");
        for (int i = 0; i < fColumnReferences.Count; i++)
        {
            var reference = (TBoldSQLColumnReference)fColumnReferences[i]!;
            if (SysUtils.CompareText(reference.ColumnDescription.SQLName, ColumnName) == 0)
            {
                return reference;
            }
        }

        var Result = new TBoldSQLColumnReference(this, (TBoldSQLColumnDescription)TableDescription.ColumnsList.ItemsBySQLName(ColumnName)!);
        fColumnReferences.Add(Result);
        return Result;
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription TableDescription => fTableDescription!;
    [Delphi(PortStatus.Ported)] public string AliasName => GetAliasName();
    [Delphi(PortStatus.Ported)] public string TableAliasDeclaration => GetTableAliasDeclaration();
}

/// <summary>A column of a table reference.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLColumnReference
{
    private readonly TBoldSQLColumnDescription fColumnDescription;
    private readonly TBoldSQLTableReference fTableReference;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLColumnReference(TBoldSQLTableReference TableReference, TBoldSQLColumnDescription ColumnDescription)
    {
        fTableReference = TableReference;
        fColumnDescription = ColumnDescription;
    }

    [Delphi(PortStatus.Ported)]
    private string GetPrefixedColumnName() => SysUtils.Format("%s.%s", TableReference.AliasName, ColumnDescription.SQLName);

    [Delphi(PortStatus.Ported)] public TBoldSQLTableReference TableReference => fTableReference;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription ColumnDescription => fColumnDescription;
    [Delphi(PortStatus.Ported)] public string PrefixedColumnName => GetPrefixedColumnName();
}

/// <summary>A where clause fragment, rendered in a query.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSqlWCF
{
    [Delphi(PortStatus.Ported)]
    public abstract string GetAsString(TBoldSQLQuery Query);
}

[Delphi(PortStatus.Ported)]
public abstract class TBoldSQLWCFBinary : TBoldSqlWCF
{
    protected readonly TBoldSqlWCF fArg1;
    protected readonly TBoldSqlWCF fArg2;
    protected readonly string fSymbol;

    [Delphi(PortStatus.Ported)]
    protected TBoldSQLWCFBinary(TBoldSqlWCF arg1, TBoldSqlWCF arg2, string Symbol)
    {
        fArg1 = arg1;
        fArg2 = arg2;
        fSymbol = Symbol;
    }
}

/// <summary>(arg1 symbol arg2).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFBinaryInfix : TBoldSQLWCFBinary
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLWCFBinaryInfix(TBoldSqlWCF arg1, TBoldSqlWCF arg2, string Symbol) : base(arg1, arg2, Symbol)
    {
    }

    /// <summary>column = id, or column in (ids); nil for no ids.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldSQLWCFBinaryInfix? CreateWCFForIdList(TBoldSQLColumnReference ColumnRef, TBoldObjectIdList IdList)
    {
        if (IdList.Count == 1)
        {
            return new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(ColumnRef), new TBoldSQLWCFGenericExpression(IdList.CommaSeparatedIdList()), "=");
        }

        if (IdList.Count > 1)
        {
            return new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(ColumnRef), new TBoldSQLWCFGenericExpression("(" + IdList.CommaSeparatedIdList() + ")"), "in");
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => "(" + fArg1.GetAsString(Query) + " " + fSymbol + " " + fArg2.GetAsString(Query) + ")";
}

/// <summary>(symbol(arg1, arg2)).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFBinaryPrefix : TBoldSQLWCFBinary
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLWCFBinaryPrefix(TBoldSqlWCF arg1, TBoldSqlWCF arg2, string Symbol) : base(arg1, arg2, Symbol)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => "(" + fSymbol + "(" + fArg1.GetAsString(Query) + ", " + fArg2.GetAsString(Query) + "))";
}

[Delphi(PortStatus.Ported)]
public abstract class TBoldSQLWCFUnary : TBoldSqlWCF
{
    protected readonly TBoldSqlWCF fArg1;
    protected readonly string fSymbol;

    [Delphi(PortStatus.Ported)]
    protected TBoldSQLWCFUnary(TBoldSqlWCF arg1, string Symbol)
    {
        fArg1 = arg1;
        fSymbol = Symbol;
    }
}

/// <summary>(symbol(arg1)).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFUnaryPrefix : TBoldSQLWCFUnary
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLWCFUnaryPrefix(TBoldSqlWCF arg1, string Symbol) : base(arg1, Symbol)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => "(" + fSymbol + "(" + fArg1.GetAsString(Query) + "))";
}

/// <summary>((arg1) symbol).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFUnaryPostfix : TBoldSQLWCFUnary
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLWCFUnaryPostfix(TBoldSqlWCF arg1, string Symbol) : base(arg1, Symbol)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => "((" + fArg1.GetAsString(Query) + ") " + fSymbol + ")";
}

/// <summary>A quoted string literal.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFString : TBoldSqlWCF
{
    private readonly string fStr;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFString(string Value)
    {
        fStr = Value;
    }

    /// <summary>Strings in AnsiSQL are quoted with single quotes, and internal quotes are doubled.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => "'" + fStr.Replace("'", "''", StringComparison.Ordinal) + "'";
}

[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFInteger : TBoldSqlWCF
{
    private readonly int fInt;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFInteger(int Value)
    {
        fInt = Value;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => SysUtils.IntToStr(fInt);
}

/// <summary>A float, as a parameter of the query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFFloat : TBoldSqlWCF
{
    private readonly double fFloat;
    private TParam? fParam;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFFloat(double Value)
    {
        fFloat = Value;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        fParam ??= Query.AddParam();
        fParam.ParamType = TParamType.ptInput;
        fParam.AsFloat = fFloat;
        return ":" + fParam.Name;
    }
}

/// <summary>A date, as a parameter of the query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFDate : TBoldSqlWCF
{
    private readonly TDateTime fDate;
    private TParam? fParam;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFDate(TDateTime Value)
    {
        fDate = Value;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        fParam ??= Query.AddParam();
        fParam.ParamType = TParamType.ptInput;
        fParam.AsDate = fDate;
        return ":" + fParam.Name;
    }
}

/// <summary>A time, as a parameter of the query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFTime : TBoldSqlWCF
{
    private readonly TDateTime fTime;
    private TParam? fParam;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFTime(TDateTime Value)
    {
        fTime = Value;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        fParam ??= Query.AddParam();
        fParam.ParamType = TParamType.ptInput;
        fParam.AsTime = fTime;
        return ":" + fParam.Name;
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFColumnRef : TBoldSqlWCF
{
    private readonly TBoldSQLColumnReference fColumnref;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFColumnRef(TBoldSQLColumnReference ColumnRef)
    {
        fColumnref = ColumnRef;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => fColumnref.PrefixedColumnName;

    [Delphi(PortStatus.Ported)] public TBoldSQLColumnReference ColumnRef => fColumnref;
}

[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFJoin : TBoldSqlWCF
{
    private readonly TBoldSQLColumnReference fColumnref1;
    private readonly TBoldSQLColumnReference fColumnref2;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFJoin(TBoldSQLColumnReference ColumnRef1, TBoldSQLColumnReference ColumnRef2)
    {
        fColumnref1 = ColumnRef1;
        fColumnref2 = ColumnRef2;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => fColumnref1.PrefixedColumnName + "=" + fColumnref2.PrefixedColumnName;
}

/// <summary>A fragment with a subquery, whose parameters it copies to the outer query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFWithQuery : TBoldSqlWCF
{
    protected TBoldSQLQuery fQuery;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFWithQuery(TBoldSQLQuery query)
    {
        fQuery = query;
    }

    [Delphi(PortStatus.Ported)]
    protected string QueryAsString()
    {
        var Strings = new TStringList();
        Query.GenerateSQL(Strings);
        return Strings.Text;
    }

    [Delphi(PortStatus.Ported)]
    protected void CopyParams(TBoldSQLQuery Query)
    {
        for (int i = 0; i < this.Query.Params.Count; i++)
        {
            TParam param = this.Query.Params[i];
            TParam topQueryParam = Query.Params.FindParam(param.Name) ?? Query.AddParam(param.Name);
            topQueryParam.Assign(param);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        string result = "(" + QueryAsString() + ")";
        CopyParams(Query);
        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLQuery Query => fQuery;
}

/// <summary>EXISTS (subquery selecting the main table's id).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFExists : TBoldSQLWCFWithQuery
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFExists(TBoldSQLQuery query, TBoldSQLTableReference MainTable) : base(query)
    {
        query.ClearColumnsToRetrieve();
        query.AddColumnToRetrieve(MainTable.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        string result = "EXISTS (" + QueryAsString() + ")";
        CopyParams(Query);
        return result;
    }
}

/// <summary>(arg1 IN (subquery selecting the main table's id)).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFInQuery : TBoldSQLWCFWithQuery
{
    private readonly TBoldSqlWCF fArg1;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFInQuery(TBoldSqlWCF Arg1, TBoldSQLQuery Query, TBoldSQLTableReference MainTable) : base(Query)
    {
        fArg1 = Arg1;
        Query.ClearColumnsToRetrieve();
        Query.AddColumnToRetrieve(MainTable.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        string result = "(" + Arg1.GetAsString(Query) + " IN (" + QueryAsString() + "))";
        CopyParams(Query);
        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldSqlWCF Arg1 => fArg1;
}

/// <summary>A subquery computing a group function.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFGroupFunction : TBoldSQLWCFWithQuery
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFGroupFunction(TBoldSQLQuery query) : base(query)
    {
        query.ClearColumnsToRetrieve();
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PreGetAsString()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        PreGetAsString();
        string result = "(" + QueryAsString() + ")";
        CopyParams(Query);
        return result;
    }
}

/// <summary>A subquery counting rows.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFSize : TBoldSQLWCFGroupFunction
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLWCFSize(TBoldSQLQuery query) : base(query)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void PreGetAsString() => fQuery.RetrieveCountStar();
}

/// <summary>A subquery applying an operation (MAX, SUM, ...) to a column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFGroupFunctionWithColumn : TBoldSQLWCFGroupFunction
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFGroupFunctionWithColumn(TBoldSQLQuery query, TBoldSQLColumnReference ColumnRef, string Operation) : base(query)
    {
        query.AddColumnToRetrieve(ColumnRef);
        query.fGroupOperation = Operation;
    }
}

/// <summary>Literal SQL text.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFGenericExpression : TBoldSqlWCF
{
    private readonly string fExpr;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFGenericExpression(string Expr)
    {
        fExpr = Expr;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query) => fExpr;
}

/// <summary>Aliases and parameter numbers shared by a query and its subqueries.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLNameSpace
{
    private readonly TStringList fUsedNames = new();
    private int fUsedParams;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLNameSpace()
    {
    }

    [Delphi(PortStatus.Ported)]
    public int GetUnusedParamNumber() => fUsedParams++;

    [Delphi(PortStatus.Ported)]
    public string GetUniqueAlias(string TableName)
    {
        int i = 1;
        string result;
        do
        {
            result = SysUtils.Copy(TableName, 1, 10) + "_" + SysUtils.IntToStr(i);
            i++;
        }
        while (fUsedNames.IndexOf(result) != -1);
        fUsedNames.Add(result);
        return result;
    }
}

/// <summary>A select under construction, which generates its SQL.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLQuery
{
    private readonly TBoldSQLQueryMode fMode;
    private readonly TBoldObjectArray fJoins;
    private readonly TBoldSqlTableReferenceList fTableReferences;
    private readonly TBoldSQLSystemDescription fSystemDescription;
    private readonly TBoldObjectArray fColumnsToRetrieve;
    private readonly TBoldObjectArray fColumnsToOrderBy;
    private readonly TBoldObjectArray fWhereClauseFragments;
    private readonly TParams fParams;
    private bool fRetrieveCountStar;
    internal string fGroupOperation = "";
    private readonly TBoldSQLNameSpace? fNameSpace;
    private TBoldSQLTableReference? fMainTable;
    private readonly TBoldSQLDataBaseConfig fSQLDatabaseConfig;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLQuery(TBoldSQLQueryMode Mode, TBoldSQLSystemDescription SystemDescription, TBoldSQLDataBaseConfig SQLDatabaseConfig, TBoldSQLNameSpace? NameSpace)
    {
        fMode = Mode;
        fJoins = new TBoldObjectArray(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));
        fTableReferences = new TBoldSqlTableReferenceList(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));
        fColumnsToRetrieve = new TBoldObjectArray(0, TBoldContainerOptions.Empty);
        fColumnsToOrderBy = new TBoldObjectArray(0, TBoldContainerOptions.Empty);
        fSQLDatabaseConfig = SQLDatabaseConfig;
        fWhereClauseFragments = new TBoldObjectArray(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));
        fSystemDescription = SystemDescription;
        fParams = new TParams();
        fNameSpace = NameSpace;
        IgnoreHistoricObjects = true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLColumnReference GetColumnToRetrieve(int Index) => (TBoldSQLColumnReference)fColumnsToRetrieve[Index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLJoin GetJoin(int index) => (TBoldSQLJoin)fJoins[index]!;

    [Delphi(PortStatus.Ported)]
    internal string GetUniqueAlias(string TableName)
    {
        if (fNameSpace is not null)
        {
            return fNameSpace.GetUniqueAlias(TableName);
        }

        int Counter = 0;
        string result;
        bool OK;
        do
        {
            OK = true;
            Counter++;
            result = SysUtils.Copy(TableName, 1, 10) + "_" + SysUtils.IntToStr(Counter);
            for (int i = 0; i < TableReferences.Count; i++)
            {
                OK = OK && TableReferences[i].fAliasName != result;
            }
        }
        while (!OK);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSqlWCF GetWCF(int index) => (TBoldSqlWCF)fWhereClauseFragments[index]!;

    /// <summary>
    /// SELECT columns, FROM tables (with SQL92 joins, the main table first and joins in ON clauses where they connect a
    /// table to the previous one), WHERE joins and fragments, ORDER BY.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void GenerateSelect(TStrings Strings)
    {
        var TempStringList = new TStringList();
        for (int i = 0; i < fColumnsToRetrieve.Count; i++)
        {
            TempStringList.Add(ColumnToRetrieve(i).PrefixedColumnName);
        }

        if (fGroupOperation.Length != 0)
        {
            TempStringList[0] = fGroupOperation + "(" + TempStringList[0] + ")";
        }

        if (fRetrieveCountStar)
        {
            TempStringList.Add("COUNT(*)");
        }

        Strings.Add("SELECT " + BoldUtils.BoldSeparateStringList(TempStringList, ", ", "", ""));
        string PrefixOfNextWCF = "WHERE";
        if (SQLDatabaseConfig.UseSQL92Joins && TableReferences.Count > 0)
        {
            // ensure that the maintable reference is first.
            for (int i = 0; i < TableReferences.Count; i++)
            {
                if (TableReferences[i] == MainTable)
                {
                    TableReferences.Move(i, 0);
                }
            }

            var UnprocessedJoins = new TBoldObjectArray(0, TBoldContainerOptions.Empty);
            for (int i = 0; i < fJoins.Count; i++)
            {
                UnprocessedJoins.Add(fJoins[i]);
            }

            string temp = "FROM " + TableReferences[0].TableAliasDeclaration;
            string PrevTable = TableReferences[0].AliasName;
            for (int i = 1; i < TableReferences.Count; i++)
            {
                string cond = "";
                for (int j = UnprocessedJoins.Count - 1; j >= 0; j--)
                {
                    var tempJoin = (TBoldSQLJoin)UnprocessedJoins[j]!;
                    if ((tempJoin.fColumnRef1.TableReference.AliasName == TableReferences[i].AliasName && tempJoin.fColumnRef2.TableReference.AliasName == PrevTable) ||
                        (tempJoin.fColumnRef2.TableReference.AliasName == TableReferences[i].AliasName && tempJoin.fColumnRef1.TableReference.AliasName == PrevTable))
                    {
                        UnprocessedJoins.Delete(j);
                        if (cond.Length != 0)
                        {
                            cond += " AND ";
                        }

                        cond += tempJoin.JoinStatement;
                    }
                }

                if (cond.Length == 0)
                {
                    temp += ", " + TableReferences[i].TableAliasDeclaration;
                    PrevTable = TableReferences[i].AliasName;
                }
                else
                {
                    temp += SysUtils.Format(" JOIN %s ON (%s)", TableReferences[i].TableAliasDeclaration, cond);
                }
            }

            Strings.Add(temp);
            for (int i = 0; i < UnprocessedJoins.Count; i++)
            {
                Strings.Add(PrefixOfNextWCF + " " + ((TBoldSQLJoin)UnprocessedJoins[i]!).JoinStatement);
                PrefixOfNextWCF = " AND";
            }
        }
        else
        {
            TempStringList.Clear();
            for (int i = 0; i < TableReferences.Count; i++)
            {
                TempStringList.Add(TableReferences[i].TableAliasDeclaration);
            }

            Strings.Add("FROM " + BoldUtils.BoldSeparateStringList(TempStringList, ", ", "", ""));
            for (int i = 0; i < fJoins.Count; i++)
            {
                Strings.Add(PrefixOfNextWCF + " " + Join(i).JoinStatement);
                PrefixOfNextWCF = " AND";
            }
        }

        for (int i = 0; i < fWhereClauseFragments.Count; i++)
        {
            Strings.Add(PrefixOfNextWCF + " " + WCF(i).GetAsString(this));
            PrefixOfNextWCF = "  AND";
        }

        if (fColumnsToOrderBy.Count > 0)
        {
            string order = "";
            for (int i = 0; i < fColumnsToOrderBy.Count; i++)
            {
                if (order.Length != 0)
                {
                    order += ", ";
                }

                order += ((TBoldSQLColumnReference)fColumnsToOrderBy[i]!).PrefixedColumnName;
            }

            Strings.Add("ORDER BY " + order);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void EnsureTableExists(string tableName, string Operation)
    {
        if (SystemDescription.SQLTablesList.ItemsBySQLName(tableName) is null)
        {
            throw new EBoldInternal(SysUtils.Format("%s: Table %s does not exist", Operation, tableName));
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetAsString()
    {
        var Strings = new TStringList();
        GenerateSelect(Strings);
        return Strings.Text;
    }

    [Delphi(PortStatus.Ported)] protected TBoldSqlTableReferenceList TableReferences => fTableReferences;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldSQLColumnReference ColumnToRetrieve(int Index) => GetColumnToRetrieve(Index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldSQLJoin Join(int Index) => GetJoin(Index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldSqlWCF WCF(int index) => GetWCF(index);

    [Delphi(PortStatus.Ported)]
    public TBoldSQLJoin AddJoin(TBoldSQLColumnReference ColumnRef1, TBoldSQLColumnReference ColumnRef2)
    {
        TBoldSQLJoin? result = null;
        for (int i = 0; i < fJoins.Count; i++)
        {
            if (Join(i).Matches(ColumnRef1, ColumnRef2))
            {
                result = Join(i);
            }
        }

        if (result is null)
        {
            result = new TBoldSQLJoin(ColumnRef1, ColumnRef2);
            fJoins.Add(result);
        }

        return result;
    }

    /// <summary>A reference to the table; for versioned tables (when ignoring history), joined with the root table's current versions.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSQLTableReference AddTableReference(string TableName)
    {
        EnsureTableExists(TableName, "AddTableReference");
        var result = new TBoldSQLTableReference(this) { fTableDescription = SystemDescription.SQLTablesList.ItemsBySQLName(TableName) };
        fTableReferences.Add(result);
        if (result.TableDescription.Versioned && IgnoreHistoricObjects)
        {
            TBoldSQLTableDescription RootTable = ((TBoldDefaultSystemDescription)SystemDescription).RootTable!;
            if (result.TableDescription == RootTable)
            {
                var NowTimeWCF = new TBoldSQLWCFInteger(BoldDefs.BOLDMAXTIMESTAMP);
                var EndTimeColWCF = new TBoldSQLWCFColumnRef(result.GetColumnReference(BoldDefs.TIMESTAMPSTOPCOLUMNNAME));
                AddWCF(new TBoldSQLWCFBinaryInfix(NowTimeWCF, EndTimeColWCF, "="));
            }
            else
            {
                TBoldSQLTableReference RootTableRef = AddTableReference(RootTable.SQLName);
                AddJoin(result.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME), RootTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME));
                AddJoin(result.GetColumnReference(BoldDefs.IDCOLUMN_NAME), RootTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            }
        }

        return result;
    }

    /// <summary>Adds a column to the select; the main table is the table of all the columns, if they share one.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddColumnToRetrieve(TBoldSQLColumnReference ColumnReference)
    {
        if (fColumnsToRetrieve.IndexOf(ColumnReference) == -1)
        {
            fColumnsToRetrieve.Add(ColumnReference);
        }

        if (fColumnsToRetrieve.Count == 1)
        {
            fMainTable = ColumnReference.TableReference;
        }
        else if (fMainTable != ColumnReference.TableReference)
        {
            fMainTable = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddColumnToOrderBy(TBoldSQLColumnReference Columnreference) => fColumnsToOrderBy.Add(Columnreference);

    [Delphi(PortStatus.Ported)]
    public void GenerateSQL(TStrings Strings)
    {
        if (Mode == TBoldSQLQueryMode.qmSelect)
        {
            GenerateSelect(Strings);
        }
    }

    /// <summary>A new parameter, named ParamN from the name space when no name is given.</summary>
    [Delphi(PortStatus.Ported)]
    public TParam AddParam(string name = "")
    {
        TParam result = fParams.Add();
        result.Name = name.Length == 0 ? "Param" + SysUtils.IntToStr(fNameSpace!.GetUnusedParamNumber()) : name;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool HastableReferenceInList(TBoldSQLTableReference TableReference) => fTableReferences.IndexOf(TableReference) != -1;

    [Delphi(PortStatus.Ported)]
    public void RetrieveCountStar() => fRetrieveCountStar = true;

    [Delphi(PortStatus.Ported)]
    public void AddWCF(TBoldSqlWCF? WCF)
    {
        if (WCF is not null)
        {
            fWhereClauseFragments.Add(WCF);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ClearColumnsToRetrieve()
    {
        fColumnsToRetrieve.Clear();
        fMainTable = null;
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLQueryMode Mode => fMode;
    [Delphi(PortStatus.Ported)] public TBoldSQLSystemDescription SystemDescription => fSystemDescription;
    [Delphi(PortStatus.Ported)] public TBoldSQLTableReference? MainTable => fMainTable;
    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDatabaseConfig => fSQLDatabaseConfig;
    [Delphi(PortStatus.Ported)] public TParams Params => fParams;
    [Delphi(PortStatus.Ported)] public bool IgnoreHistoricObjects { get; set; }
}

/// <summary>A typed array of table references.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlTableReferenceList(int InitialCapacity, TBoldContainerOptions Options) : TBoldObjectArray(InitialCapacity, Options)
{
    [Delphi(PortStatus.Ported)]
    private new TBoldSQLTableReference Get(int Index) => (TBoldSQLTableReference)base[Index]!;

    [Delphi(PortStatus.Ported)]
    private void Put(int Index, TBoldSQLTableReference Value) => base[Index] = Value;

    [Delphi(PortStatus.Ported)]
    public int Add(TBoldSQLTableReference Item) => base.Add(Item);

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, TBoldSQLTableReference Item) => base.Insert(Index, Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldSqlTableReferenceList.Items")]
    public new TBoldSQLTableReference this[int Index] { get => Get(Index); set => Put(Index, value); }
}
