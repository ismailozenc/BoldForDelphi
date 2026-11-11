# Deployment

How Bold for C# leaves this repository: as NuGet packages, as a .NET tool, and as programs to publish, run as services or
in containers. The user guide's Deployment section explains each; this folder holds the pieces.

| File | What |
|---|---|
| [`install.ps1`](install.ps1) | Installs Bold on a machine that has the .NET 10 SDK: packs everything into `CSharp/artifacts/packages`, makes that folder the package source `bold`, installs `bold-codegen` as a global tool and publishes the programs into `CSharp/artifacts/publish`; it removes cached packages of the same version, so a new pack is the one used. `-Uninstall` undoes it. On Windows it also makes `.bld` the model editor's file type, and builds and installs the Visual Studio extension ([`../vs`](../vs)) when Visual Studio is there. CI runs it in Windows PowerShell and builds a new project on the installed source. |
| [`check.sh`](check.sh) | Proves the repository deploys: packs everything into `CSharp/artifacts/packages`, installs `bold-codegen` from there, builds and runs [`PackageSmoke`](PackageSmoke) on the packages alone, publishes the programs into `CSharp/artifacts/publish` and starts the servers from there. CI runs it on Windows after the tests. |
| [`PackageSmoke/`](PackageSmoke) | An application outside the solution that uses only the packages: the Library model, typed classes from `bold-codegen`, SQLite, OCL, transactions and a custom attribute type. |
| [`WinFormsSmoke/`](WinFormsSmoke) | The user guide's WinForms window on the packages alone; `check.sh` starts it on Windows and checks its grid. |
| `Directory.Build.*`, `Directory.Packages.props`, [`nuget.config`](nuget.config) | Keep the repository's build settings away from the smoke tests, and restore them from `CSharp/artifacts/packages` and nuget.org. |
| [`PACKAGE.md`](PACKAGE.md) | The readme inside every package. |
| [`Bold.Package.targets`](Bold.Package.targets) | Packed into every library package: compiles the package's Delphi type aliases into the projects that use it. |
| [`docker/`](docker) | Dockerfiles of the propagator server and the remote persistence server; build them from the repository's root. CI builds and runs both. |
| [`systemd/`](systemd) | systemd units for both servers. |

```
powershell -ExecutionPolicy Bypass -File CSharp\deploy\install.ps1     # install on this machine
bash CSharp/deploy/check.sh                  # pack, check and publish
dotnet pack CSharp/Bold.slnx -c Release      # only the packages
docker build -f CSharp/deploy/docker/propagator.Dockerfile -t bold-propagator .
```
