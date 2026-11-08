# The remote persistence server (Bold.Remote.Server) in a container, with the propagator on the same host. Build it
# from the repository's root:
#
#   docker build -f CSharp/deploy/docker/remote.Dockerfile -t bold-remote .
#
# It reads the model from /model/Model.bld and keeps a SQLite database in the volume /data, whose tables it creates the
# first time (and leaves alone afterwards):
#
#   docker run -d -p 5000:8080 -v "$PWD/Model.bld:/model/Model.bld:ro" -v bold-data:/data --name bold-remote bold-remote
#
# For SQL Server set Bold__Provider=Microsoft.Data.SqlClient, Bold__Engine=dbeSQLServer and Bold__ConnectionString.
# Clients reach /persistence and /propagator; /health answers when the server runs.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Only what the server is built from: the C# port, and the OCL tables Bold.ObjectSpace embeds from the Delphi sources.
COPY CSharp/global.json CSharp/Directory.Build.props CSharp/Directory.Build.targets CSharp/Directory.Packages.props CSharp/
COPY CSharp/src CSharp/src
COPY Source/ObjectSpace/Ocl/BoldOcl.dfa Source/ObjectSpace/Ocl/BoldOcl.llr Source/ObjectSpace/Ocl/
RUN dotnet publish CSharp/src/Bold.Remote.Server -c Release -o /app --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir /data /model && chown $APP_UID /data
VOLUME /data
ENV Bold__Model=/model/Model.bld \
    Bold__Provider=Microsoft.Data.Sqlite \
    Bold__ConnectionString="Data Source=/data/Bold.db" \
    Bold__Engine=dbeSQLite \
    Bold__CreateSchema=true \
    Bold__Propagator=true
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bold.Remote.Server.dll"]
