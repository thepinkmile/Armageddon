<#
.SYNOPSIS
Build linux/arm64 images locally and tag them for the private registry.

.DESCRIPTION
Cross-compiles both images for linux/arm64 and loads them into the local
Docker daemon with two tags each:
  - armageddon-api:latest        (short local tag)
  - <Registry>/armageddon-api:latest  (ready to push to the private registry)

Run this script during development. When you are ready to deploy, push the
registry-tagged images:
  docker push armageddon.local/docker-local/armageddon-api:latest
  docker push armageddon.local/docker-local/armageddon-web:latest

Or simply run deploy.ps1 which builds, pushes, and restarts the Pi stack in
one step.

NOTE: --platform linux/arm64 is required when building on an x64 machine so
that TARGETARCH resolves correctly inside the Dockerfile.

.PARAMETER Registry
Base URL of the private Docker registry.
Defaults to "armageddon.local/docker-local".

.EXAMPLE
.\build.ps1

.EXAMPLE
.\build.ps1 -Registry "myregistry.local/docker-local"
#>

[CmdletBinding()]
param(
[string]$Registry = "armageddon.local/docker-local"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ApiLocal    = "armageddon-api:latest"
$WebLocal    = "armageddon-web:latest"
$ApiRegistry = "$Registry/armageddon-api:latest"
$WebRegistry = "$Registry/armageddon-web:latest"

function Invoke-Step([string]$Title, [scriptblock]$Body) {
Write-Host ""
Write-Host "==> $Title" -ForegroundColor Cyan
& $Body
}

$RepoRoot = $PSScriptRoot
Push-Location $RepoRoot

try {

# 1. Ensure buildx builder with multi-arch support
Invoke-Step "Ensuring buildx builder" {
$builders = docker buildx ls 2>&1
if ($builders -notmatch "armageddon-builder") {
docker buildx create --name armageddon-builder --driver docker-container --bootstrap
}
docker buildx use armageddon-builder
}

# 2. Build API image — load into local daemon with both tags
Invoke-Step "Building armageddon-api (linux/arm64)" {
docker buildx build `
--platform linux/arm64 `
--no-cache `
--file src/Armageddon.Api/Dockerfile `
--tag $ApiLocal `
--tag $ApiRegistry `
--load `
.
}

# 3. Build Web image — load into local daemon with both tags
Invoke-Step "Building armageddon-web (linux/arm64)" {
docker buildx build `
--platform linux/arm64 `
--no-cache `
--file src/Armageddon.Web/Dockerfile `
--tag $WebLocal `
--tag $WebRegistry `
--load `
.
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Images are loaded locally. To push to the registry when ready:" -ForegroundColor Yellow
Write-Host "  docker push $ApiRegistry"
Write-Host "  docker push $WebRegistry"

} finally {
Pop-Location
}
