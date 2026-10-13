namespace Bold.Porting
{
    /// <summary>
    /// How far a C# element has been ported from its Delphi original.
    /// </summary>
    /// <remarks>
    /// There is no "Verified" value on purpose. The port tool shows an element as verified only when it is
    /// <see cref="Ported"/> and at least one passing test carries a matching <see cref="CoversAttribute"/>,
    /// so the success indicator always comes from test results, never from a hand-written claim.
    /// </remarks>
    public enum PortStatus
    {
        /// <summary>Declared with the Delphi name and signature; the body throws <see cref="System.NotImplementedException"/>.</summary>
        Stub = 0,

        /// <summary>Part of the Delphi behaviour is ported. <see cref="DelphiAttribute.Note"/> says what is missing.</summary>
        Partial = 1,

        /// <summary>All of the Delphi behaviour is ported.</summary>
        Ported = 2,

        /// <summary>A .NET feature provides the Delphi behaviour instead. <see cref="DelphiAttribute.Note"/> says which.</summary>
        Replaced = 3,

        /// <summary>Intentionally not ported. <see cref="DelphiAttribute.Note"/> says why.</summary>
        Dropped = 4,
    }
}
