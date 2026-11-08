# The propagator and lock manager server (Bold.Propagator.Server) in a container. Build it from the repository's root:
#
#   docker build -f CSharp/deploy/docker/propagator.Dockerfile -t bold-propagator .
#   docker run -d -p 5050:8080 --name bold-propagator bold-propagator
#
# Clients reach the hub at http://<host>:5050/propagator; /health answers when the server runs. Settings: mount your own
# Bold.Propagator.Server.ini over /app/Bold.Propagator.Server.ini.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Only what the server is built from: the C# port, and the OCL tables Bold.ObjectSpace embeds from the Delphi sources.
COPY CSharp/global.json CSharp/Directory.Build.props CSharp/Directory.Build.targets CSharp/Directory.Packages.props CSharp/
COPY CSharp/src CSharp/src
COPY Source/ObjectSpace/Ocl/BoldOcl.dfa Source/ObjectSpace/Ocl/BoldOcl.llr Source/ObjectSpace/Ocl/
RUN dotnet publish CSharp/src/Bold.Propagator.Server -c Release -o /app --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bold.Propagator.Server.dll"]
