# BoldForDelphi

## Changes in this copy

This copy is being used to rebuild Bold in C#. Some parts have been removed, and each folder has a `FOLDER_INFO.md` that describes its contents.

Removed so far:
- **Standalone tools:** `Tools/BoldClientifier`, `Tools/BoldUpgrader` and `Tools/DBConvert`. `Tools/PropagatorAnalyzer` is kept.
- **Delphi package files (`*.dpk`):** all 27, including the ones in `examples/` and `Source/Unassigned/`. The units they listed are still in the tree.
- **Dead database drivers:** the BDE, DBISAM, Advantage, IBX, DBExpress, SQLDirect and DOA persistence back ends, and the unassigned Asta, BDEUtils, dbvalidate and Oracle-BDE code. ADO and the generic DB layer are kept.
- **Non-Windows code (CLX and Linux):** the CLX GUI (`Source/BoldQAwareGUI`), the CLX environment (`BoldEnvironmentCLX.pas`, `BoldEnvironmentAllowBothUseCLX.pas`, and `BoldEnvironmentAllowBothUseVCL.pas`, which existed only to allow VCL and CLX together), the CLX configuration in `BoldEnvironment.pas`, and the `{$IFDEF LINUX}` branches in `BoldPerformanceCounter.pas` and `BoldUtils.pas`. The matching entries are removed from `PackageDef.txt` and `SourceDef.txt`.
- **Unrelated examples:** the example projects that depend on a dropped feature (COM/DCOM, MSXML/SOAP/XML, OLE, Rational Rose, the system debugger form) are removed: 41 example folders, deleted as 32 top-level folders in `examples/CBuilder` and `examples/Delphi`. Examples that only use kept features, or that use nothing dropped, are kept.

Not yet removed: the rest of the drop list (COM and DCOM distribution, Rational Rose links, Delphi IDE integration, and samples). Those are tracked in the drop list and will be removed in later changes.

This is master branch. If you want latest development please check develop branch instead
https://github.com/Embarcadero/BoldForDelphi/tree/develop

Purpose of this is always be stable and ready for deploy to a production environment.
Do not commit directly to master branch. Instead merge changes from develop branch.
A new release is done for each merge from develop.

The original source code of the Bold library for Delphi
Version 4.0.1.0 Bold for Delphi, Release 4.0 - 2004-04-23

Note that masterBranch is not compatible with unicode. It is made for D7 or D2007.
The branch AttracsBold is compatible with latest Delphi, currently 10.4.2.

Bold is a tool in the MDA (Model Driven Architecture) space, which allows you to start with an UML model of your application and a set of business rules written in a high level language and “execute” the model after creating a graphical user interface for it.
Bold includes a sophisticated object-relational mapping layer, ability to map data to multiple formats, changes synchronization, and much more. It includes a large amount of IDE integrated tools and options to work with external UML modeling software.

For more information see:
- https://delphi.fandom.com/wiki/Bold_for_Delphi
- http://boldfordelphi.blogspot.com/

There is a new Discord chat dedicated only for this project.
If you are interested to join the chat about the repository login to Discord here https://discord.gg/C6frzsn
  
The project source code has been made available by https://www.embarcadero.com/ and published under an MIT license.
