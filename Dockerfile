FROM mcr.microsoft.com/dotnet/sdk:10.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /workspace

# Copy everything and build in the container
COPY . ./

RUN dotnet restore
RUN dotnet build -c Release

# Ensure TestResults directory exists and expose it via volume when running
RUN mkdir -p /workspace/tests/Armageddon.Tests/TestResults

# Default entry: run tests for the primary test project and collect XPlat Code Coverage
ENTRYPOINT ["sh","-c","dotnet test tests/Armageddon.Tests/Armageddon.Tests.csproj --no-build --collect:\"XPlat Code Coverage\" --results-directory /workspace/tests/Armageddon.Tests/TestResults --logger:\"trx;LogFileName=/workspace/tests/Armageddon.Tests/TestResults/test_results.trx\""]
