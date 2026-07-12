# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Armageddon.Web/Armageddon.Web.csproj", "src/Armageddon.Web/"]
COPY ["src/Armageddon.Abstractions/Armageddon.Abstractions.csproj", "src/Armageddon.Abstractions/"]

RUN dotnet restore "src/Armageddon.Web/Armageddon.Web.csproj"

COPY . .
WORKDIR "/src/src/Armageddon.Web"
RUN dotnet publish "Armageddon.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Armageddon.Web.dll"]
