# Deployment

How Bold for C# leaves this repository: as NuGet packages, as a .NET tool, and as programs to publish, run as services or
in containers. The user guide's Deployment section explains each; this folder holds the pieces.

| File | What |
|---|---|
| [`check.sh`](check.sh) | Proves the repository deploys: packs everything into `CSharp/artifacts/packages`, installs `bold-codegen` from there, builds and runs [`PackageSmoke`](PackageSmoke) on the packages alone, publishes the programs into `CSharp/artifacts/publish` and starts the servers from there. CI runs it on Windows after the tests. |
| [`PackageSmoke/`](PackageSmoke) | An application outside the solution that uses only the packages: the Library model, typed classes from `bold-codegen`, SQLite, OCL, transactions and a custom attribute type. |
| [`PACKAGE.md`](PACKAGE.md) | The readme inside every package. |
| [`Bold.Package.targets`](Bold.Package.targets) | Packed into every library package: compiles the package's Delphi type aliases into the projects that use it. |
| [`docker/`](docker) | Dockerfiles of the propagator server and the remote persistence server; build them from the repository's root. CI builds and runs both. |
| [`systemd/`](systemd) | systemd units for both servers. |

```
bash CSharp/deploy/check.sh                  # pack, check and publish
dotnet pack CSharp/Bold.slnx -c Release      # only the packages
docker build -f CSharp/deploy/docker/propagator.Dockerfile -t bold-propagator .
```
