FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file for restore
COPY src/StepikAnalytics.csproj StepikAnalytics/

# Restore
WORKDIR /src/StepikAnalytics
RUN dotnet restore StepikAnalytics.csproj -f net10.0

# Copy source
WORKDIR /src
COPY src/ StepikAnalytics/

# Build
WORKDIR /src/StepikAnalytics
RUN dotnet publish StepikAnalytics.csproj -c Release -f net10.0 -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "StepikAnalytics.Backend.dll"]
