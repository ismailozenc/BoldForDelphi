// The ids of the package: the same guids and numbers as in BoldPackage.vsct.
using System;

namespace Bold.VisualStudio;

internal static class PackageIds
{
    public const string Package = "6b1e6f4c-2c7a-4c0b-9e8a-5b0c1d2e3f41";
    public const string CommandSet = "a3d7b2e1-4f5c-4a6b-8c9d-0e1f2a3b4c52";
    public const string EditorFactory = "d2c4e6a8-1b3d-4f5a-9c7e-2a4b6c8d0e63";

    /// <summary>The UI context that is active while one .bld file is selected in Solution Explorer.</summary>
    public const string BldSelected = "f1e2d3c4-b5a6-4978-8a9b-0c1d2e3f4a74";

    public static readonly Guid CommandSetGuid = new(CommandSet);

    public const int ModelEditor = 0x0100;
    public const int GenerateClasses = 0x0101;
    public const int PropagatorAnalyzer = 0x0102;
    public const int UserGuide = 0x0110;
    public const int Tutorials = 0x0111;
    public const int OpenModel = 0x0120;
    public const int GenerateClassesOfModel = 0x0121;
}
