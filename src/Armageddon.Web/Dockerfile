# syntax=docker/dockerfile:1
# Build context must be the repo root:
#   docker build -f src/Armageddon.Web/Dockerfile .
#
# Multi-arch (cross-compile for Raspberry Pi):
#   docker buildx build --platform linux/arm64 -f src/Armageddon.Web/Dockerfile -t armageddon-web .

# ── Build stage ───────────────────────────────────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /repo

# Restore — project files only for cache efficiency
COPY src/Armageddon.Abstractions/Armageddon.Abstractions.csproj  src/Armageddon.Abstractions/
COPY src/Armageddon.Web/Armageddon.Web.csproj                    src/Armageddon.Web/
# Restore with the target arch RID so the Blazor/ASP.NET web asset packs for arm64 are downloaded.
# --no-restore is intentionally omitted on publish so the asset pipeline resolves correctly.
RUN dotnet restore src/Armageddon.Web/Armageddon.Web.csproj -a $TARGETARCH

# Copy source and publish — restore is re-run as part of publish to ensure
# Blazor _framework assets (blazor.web.js etc.) are correctly resolved for the target platform.
COPY src/Armageddon.Abstractions/ src/Armageddon.Abstractions/
COPY src/Armageddon.Web/          src/Armageddon.Web/
RUN dotnet publish src/Armageddon.Web/Armageddon.Web.csproj \
        -c Release \
        -a $TARGETARCH \
        -o /app/publish \
        /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install ca-certificates tooling so update-ca-certificates is available
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Bake the public API cert into the image trust store at build time.
# This allows outbound HTTPS calls from the Web container to https://api:8443
# to be trusted without any runtime entrypoint script.
# The .crt is the public certificate only — no secrets are embedded.
COPY deployment/certs/armageddon.crt /usr/local/share/ca-certificates/armageddon.crt
RUN update-ca-certificates

# Create the certs directory for the runtime-mounted PFX (Kestrel HTTPS):
#   -v /opt/armageddon/certs:/app/certs:ro
# Create the data directory for DataProtection key persistence:
#   -v /opt/armageddon/web-data:/app/data
RUN mkdir -p /app/certs /app/data/dp-keys

# Disable io_uring socket transport. .NET 10's socket layer probes for io_uring
# at startup; on Raspberry Pi kernels below ~6.1 this probe hangs indefinitely,
# preventing Kestrel from ever reaching the listen/bind stage.
ENV DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS=0
ENV DOTNET_SYSTEM_NET_DISABLE_IOURING=1
ENV ASPNETCORE_ENVIRONMENT=Production
# Clear the base image's default HTTP_PORTS so it does not conflict with the
# explicit Kestrel binding configured in Program.cs via ConfigureKestrel().
ENV ASPNETCORE_HTTP_PORTS=

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "Armageddon.Web.dll"]
