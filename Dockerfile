# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Aristokeides.Api/Aristokeides.Api.csproj", "Aristokeides.Api/"]
RUN dotnet restore "Aristokeides.Api/Aristokeides.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Aristokeides.Api"
RUN dotnet build "Aristokeides.Api.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "Aristokeides.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install git for backend git operations
RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
EXPOSE 2222

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Ensure directory exists for git repositories
RUN mkdir -p /app/repositories

ENTRYPOINT ["dotnet", "Aristokeides.Api.dll"]
