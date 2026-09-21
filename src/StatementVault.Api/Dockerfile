# Build from the repository root:
#   docker build -f src/StatementVault.Api/Dockerfile -t statementvault-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props nuget.config global.json ./
COPY src/StatementVault.Domain/ src/StatementVault.Domain/
COPY src/StatementVault.Persistence/ src/StatementVault.Persistence/
COPY src/StatementVault.Api/ src/StatementVault.Api/
RUN dotnet restore src/StatementVault.Api/StatementVault.Api.csproj
RUN dotnet publish src/StatementVault.Api/StatementVault.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "StatementVault.Api.dll"]
