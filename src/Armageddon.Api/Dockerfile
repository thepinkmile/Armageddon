# syntax=docker/dockerfile:1
# Build context must be the repo root:
#   docker build -f src/Armageddon.Api/Dockerfile .
#
# Multi-arch (cross-compile for Raspberry Pi):
#   docker buildx build --platform linux/arm64 -f src/Armageddon.Api/Dockerfile -t armageddon-api .

# ── Build stage ───────────────────────────────────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /repo

# Restore — copy project files first so this layer is cached unless deps change
COPY src/Armageddon.Abstractions/Armageddon.Abstractions.csproj  src/Armageddon.Abstractions/
COPY src/Armageddon.Api/Armageddon.Api.csproj                    src/Armageddon.Api/
RUN dotnet restore src/Armageddon.Api/Armageddon.Api.csproj -a $TARGETARCH

# Copy source and publish — restore is intentionally not skipped so any web/static
# asset pipeline (e.g. OpenAPI UI, Scalar) resolves correctly for the target platform.
COPY src/Armageddon.Abstractions/ src/Armageddon.Abstractions/
COPY src/Armageddon.Api/          src/Armageddon.Api/
RUN dotnet publish src/Armageddon.Api/Armageddon.Api.csproj \
        -c Release \
        -a $TARGETARCH \
        -o /app/publish \
        /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for the Docker healthcheck (not present in the base image by default)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create the data directory that SQLite will write into.
# Mount a Docker volume here at runtime to persist the database across restarts.
RUN mkdir -p /app/data

# Create the certs directory; mount your PFX here at runtime:
#   -v /opt/armageddon/certs:/app/certs:ro
RUN mkdir -p /app/certs

COPY --from=build /app/publish .

# Disable io_uring socket transport. .NET 10's socket layer probes for io_uring
# at startup; on Raspberry Pi kernels below ~6.1 this probe hangs indefinitely,
# preventing Kestrel from ever reaching the listen/bind stage.
ENV DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS=0
ENV DOTNET_SYSTEM_NET_DISABLE_IOURING=1
ENV ASPNETCORE_ENVIRONMENT=Production
# Clear the base image's default HTTP_PORTS so it does not conflict with the
# explicit Kestrel binding configured in Program.cs via ConfigureKestrel().
ENV ASPNETCORE_HTTP_PORTS=

# JWT key, issuer and connection string are injected at runtime via env vars or
# a compose environment section — never bake secrets into the image.
# ENV Jwt__Key=<set-at-runtime>
# ENV ConnectionStrings__DefaultConnection=Data Source=/app/data/armageddon.db

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "Armageddon.Api.dll"]
