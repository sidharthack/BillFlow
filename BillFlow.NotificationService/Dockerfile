# ── Stage 1: Build ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY BillFlow.slnx ./
COPY BillFlow.Contracts/BillFlow.Contracts.csproj BillFlow.Contracts/
COPY BillFlow.NotificationService/BillFlow.NotificationService.csproj BillFlow.NotificationService/

# Restore NuGet packages
RUN dotnet restore BillFlow.NotificationService/BillFlow.NotificationService.csproj

# Copy full source
COPY BillFlow.Contracts/ BillFlow.Contracts/
COPY BillFlow.NotificationService/ BillFlow.NotificationService/

# Publish
RUN dotnet publish BillFlow.NotificationService/BillFlow.NotificationService.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ───────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Non-root user for security
USER app

# ASP.NET Core listens on 8080 inside container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BillFlow.NotificationService.dll"]