# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the API and Abstractions project files (do not include Web)
COPY ["src/Armageddon.Api/Armageddon.Api.csproj", "src/Armageddon.Api/"]
COPY ["src/Armageddon.Abstractions/Armageddon.Abstractions.csproj", "src/Armageddon.Abstractions/"]

RUN dotnet restore "src/Armageddon.Api/Armageddon.Api.csproj"

# Copy remaining source files for those projects
COPY src/Armageddon.Api/. src/Armageddon.Api/
COPY src/Armageddon.Abstractions/. src/Armageddon.Abstractions/

WORKDIR "/src/src/Armageddon.Api"
RUN dotnet publish "Armageddon.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Armageddon.Api.dll"]
