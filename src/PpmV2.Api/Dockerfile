# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only csproj-Files (better caching for code changes)
COPY ["src/PpmV2.Api/PpmV2.Api.csproj", "src/PpmV2.Api/"]
COPY ["src/PpmV2.Application/PpmV2.Application.csproj", "src/PpmV2.Application/"]
COPY ["src/PpmV2.Domain/PpmV2.Domain.csproj", "src/PpmV2.Domain/"]
COPY ["src/PpmV2.Infrastructure/PpmV2.Infrastructure.csproj", "src/PpmV2.Infrastructure/"]

# NuGet-Pakete restore
RUN dotnet restore "src/PpmV2.Api/PpmV2.Api.csproj"

COPY . .

# Publish in ein schlankes Verzeichnis
WORKDIR "/src/src/PpmV2.Api"
RUN dotnet publish "PpmV2.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy only the published result to the runtime image
COPY --from=build /app/publish .

# Default port in the container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
# ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PpmV2.Api.dll"]
