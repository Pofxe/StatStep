FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects
COPY ../StepikAnalytics.Shared/StepikAnalytics.Shared.csproj StepikAnalytics.Shared/
COPY StepikAnalytics.Backend.csproj StepikAnalytics.Backend/

# Restore
WORKDIR /src/StepikAnalytics.Backend
RUN dotnet restore

# Copy source
WORKDIR /src
COPY ../StepikAnalytics.Shared/ StepikAnalytics.Shared/
COPY . StepikAnalytics.Backend/

# Build
WORKDIR /src/StepikAnalytics.Backend
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "StepikAnalytics.Backend.dll"]
