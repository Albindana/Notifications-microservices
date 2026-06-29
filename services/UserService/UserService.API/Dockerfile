# Build context is the repository root (see docker-compose.yml) so the shared
# project under shared/ is reachable alongside the service's own projects.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/UserService/UserService.API/UserService.API.csproj", "services/UserService/UserService.API/"]
COPY ["services/UserService/UserService.Application/UserService.Application.csproj", "services/UserService/UserService.Application/"]
COPY ["services/UserService/UserService.Domain/UserService.Domain.csproj", "services/UserService/UserService.Domain/"]
COPY ["services/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "services/UserService/UserService.Infrastructure/"]
COPY ["shared/Shared.Contracts/Shared.Contracts.csproj", "shared/Shared.Contracts/"]
RUN dotnet restore "services/UserService/UserService.API/UserService.API.csproj"
COPY . .
RUN dotnet publish "services/UserService/UserService.API/UserService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]
