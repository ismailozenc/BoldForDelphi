using System;

namespace Bold.Porting
{
    /// <summary>
    /// Links a C# element to the Delphi element it was ported from, and records its port status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Delphi unit and symbol are normally inferred: the unit is the C# file name (BoldDefs.cs or
    /// BoldDefs.Something.cs is unit BoldDefs) and the symbol is the C# name path (TBoldSystem.UpdateDatabase).
    /// This works because ported code keeps the Delphi file, type and member names.
    /// Set <see cref="Unit"/> or <see cref="Symbol"/> only where the names differ, for example
    /// <c>[Delphi(PortStatus.Ported, Symbol = "TBoldSystem.Destroy")]</c> on a Dispose method.
    /// </para>
    /// <para>
    /// For Delphi elements that have no C# declaration to carry the attribute (type aliases, dropped
    /// routines), use it at assembly level with both <see cref="Unit"/> and <see cref="Symbol"/> set.
    /// </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Constructor |
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event,
        AllowMultiple = true, Inherited = false)]
    public sealed class DelphiAttribute : Attribute
    {
        public DelphiAttribute(PortStatus status)
        {
            Status = status;
        }

        /// <summary>How far this element is ported.</summary>
        public PortStatus Status { get; }

        /// <summary>The Delphi unit, when it is not the C# file name.</summary>
        public string? Unit { get; set; }

        /// <summary>
        /// The Delphi symbol, when it is not the C# name path. Use "Type.Member" for members, the plain
        /// name for unit-level routines and constants, "Type.Member(Type1,Type2)" for one overload,
        /// and "initialization" or "finalization" for unit sections.
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>What is missing (Partial), what replaces it (Replaced) or why it is dropped (Dropped).</summary>
        public string? Note { get; set; }
    }
}
