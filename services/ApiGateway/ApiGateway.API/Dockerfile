# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/ApiGateway/ApiGateway.API/ApiGateway.API.csproj", "services/ApiGateway/ApiGateway.API/"]
RUN dotnet restore "services/ApiGateway/ApiGateway.API/ApiGateway.API.csproj"
COPY . .
RUN dotnet publish "services/ApiGateway/ApiGateway.API/ApiGateway.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.API.dll"]
