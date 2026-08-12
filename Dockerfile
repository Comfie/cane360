FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Web/Web.csproj src/Web/
RUN dotnet restore src/Web/Web.csproj

COPY src/Domain src/Domain
COPY src/Application src/Application
COPY src/Infrastructure src/Infrastructure
COPY src/Web src/Web
RUN dotnet publish src/Web/Web.csproj --configuration Release --no-restore \
    --output /app/publish \
    /p:BuildClientApp=false \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.1 AS final
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cane360.Web.dll"]
