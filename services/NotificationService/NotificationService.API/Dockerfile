# Build context is the repository root (see docker-compose.yml) so the shared
# project under shared/ is reachable alongside the service's own projects.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/NotificationService/NotificationService.API/NotificationService.API.csproj", "services/NotificationService/NotificationService.API/"]
COPY ["services/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "services/NotificationService/NotificationService.Application/"]
COPY ["services/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj", "services/NotificationService/NotificationService.Domain/"]
COPY ["services/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "services/NotificationService/NotificationService.Infrastructure/"]
COPY ["shared/Shared.Contracts/Shared.Contracts.csproj", "shared/Shared.Contracts/"]
RUN dotnet restore "services/NotificationService/NotificationService.API/NotificationService.API.csproj"
COPY . .
RUN dotnet publish "services/NotificationService/NotificationService.API/NotificationService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
