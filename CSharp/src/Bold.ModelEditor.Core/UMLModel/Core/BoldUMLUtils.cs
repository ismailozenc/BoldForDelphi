// Delphi: Source/UMLModel/Core/BoldUMLUtils.pas
// Port map: CSharp/port/units/BoldUMLUtils.md

namespace Bold;

/// <summary>Routines of BoldUMLUtils.</summary>
public static class BoldUMLUtils
{
    /// <summary>The database configuration of the persistence handle whose model is the given one; none when there is no such handle.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldSQLDataBaseConfig? SQLDataBaseConfigforModel(TBoldAbstractModel BoldModel)
    {
        TBoldObjectArray? handles = BoldHandle.BoldHandleList;
        for (int i = 0; handles is not null && i < handles.Count; i++)
        {
            if (handles[i] is TBoldAbstractPersistenceHandleDB handle && handle.BoldModel == BoldModel)
            {
                return handle.SQLDatabaseConfig;
            }
        }

        return null;
    }
}
