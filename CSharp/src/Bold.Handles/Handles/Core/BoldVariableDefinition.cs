// Delphi: Source/Handles/Core/BoldVariableDefinition.pas
// Port map: CSharp/port/units/BoldVariableDefinition.md

using System.ComponentModel;

namespace Bold;

/// <summary>The older name of <see cref="TBoldOclVariables"/>.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldVariableDefinition : TBoldOclVariables
{
    /// <summary>Adds the variables to <paramref name="Owner"/> when given.</summary>
    public TBoldVariableDefinition(IContainer? Owner = null)
        : base(Owner)
    {
    }
}
