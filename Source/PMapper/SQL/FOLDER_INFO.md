# Source/PMapper/SQL

**What this folder is for:** The SQL folder inside Source/PMapper. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 11 file(s), plus 5 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldPMappersSQL.pas` | Unit | Method GenerateDatabaseScript: Generates a SQL script to create the full database schema for this persistence mapper Declares TBoldSystemSQLMapper, TBoldObjectSQLMapper, TBoldMemberSQLMapper. |
| `BoldPSDescriptionsSQL.pas` | Unit | Method EffectiveGenerationMode: Internal Declares TBoldSQLSystemDescription, TBoldSQLDescriptionElement, TBoldSQLTableDescription, TBoldSQLColumnDescription and 6 more. |
| `BoldPSParamsSQL.pas` | Unit | Declares TBoldPSSQLParams. |
| `BoldSQLDatabaseConfig.pas` | Unit | Method CorrectlyQuotedDefaultValue: Auxillary function that quotes default column values if required Declares TBoldSQLDataBaseConfig. |
| `BoldSQLMappingInfo.pas` | Unit | Method ScriptForWriteData: Generates a SQL script to write mapping info to the database Declares TBoldSQLMappingInfo, TBoldSQLMappingInfoClass, TBoldClassMappingInfo, TBoldMemberMappingInfo and 10 more. |
| `BoldSQLQuery.pas` | Unit | Declares TBoldSQLJoin, TBoldSQLTableReference, TBoldSQLColumnReference, TBoldSqlWCF and 23 more. |
| `BoldSqlNodeMaker.pas` | Unit | Declares TBoldSqlNodeMaker. |
| `BoldSqlNodes.pas` | Unit | Declares TBoldSqlNode, TBoldSqlTypeNode, TBoldSqlListCoercion, TBoldSqlOperation and 15 more. |
| `BoldSqlNodesResolver.pas` | Unit | Declares TBoldSqlNodeResolver. |
| `BoldSqlQueryGenerator.pas` | Unit | Declares TBoldSqlQueryGenerator. |
| `BoldSqlSymbols.pas` | Unit | Declares TBSS_Symbol, TBSS_BinarySymbol, TBSS_BinaryBooleanSymbol, TBSS_Equal and 95 more. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
