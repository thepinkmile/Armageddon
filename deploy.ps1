<#
.SYNOPSIS
Build linux/arm64 images, push them to the private registry, and deploy to a
Raspberry Pi via SSH.

.DESCRIPTION
1. Delegates to build.ps1 to cross-compile both images for linux/arm64 and
   tag them for the private registry.
2. Pushes each image to the private registry at armageddon.local/docker-local/.
3. Copies docker-compose.yml to the Pi over SCP.
4. SSHs in to pull the images and restart the stack with docker compose.

To build without deploying (e.g. during development), run build.ps1 directly.

.PARAMETER PiHost
SSH host for the Raspberry Pi (hostname or IP address).

.PARAMETER PiUser
SSH user on the Raspberry Pi. Defaults to "pi".

.PARAMETER PiPath
Remote directory on the Pi where files are placed. Defaults to "~/armageddon".

.PARAMETER Registry
Base URL of the private Docker registry. Defaults to "armageddon.local/docker-local".
Passed through to build.ps1.

.PARAMETER JwtKey
JWT signing key injected into the running stack.
If omitted the script falls back to the Jwt__Key environment variable.
If neither is supplied the script exits with an error.

.EXAMPLE
# Minimal - rely on $env:Jwt__Key already being set
.\deploy.ps1 -PiHost raspberrypi.local

# Explicit key and custom user
.\deploy.ps1 -PiHost 192.168.1.42 -PiUser ubuntu -JwtKey "my-strong-secret"
#>

[CmdletBinding()]
param(
[Parameter(Mandatory)]
[string]$PiHost = "armageddon.local",

[string]$PiUser = "stem",

[string]$PiPath = "~/armageddon-docker",

[string]$Registry = "armageddon.local/docker-local",

[string]$JwtKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Resolve JWT key
if (-not $JwtKey) { $JwtKey = $env:Jwt__Key }
if (-not $JwtKey) {
Write-Error "JWT signing key not supplied. Pass -JwtKey or set the Jwt__Key environment variable."
}

$ApiImage = "$Registry/armageddon-api:latest"
$WebImage = "$Registry/armageddon-web:latest"

function Invoke-Step([string]$Title, [scriptblock]$Body) {
Write-Host ""
Write-Host "==> $Title" -ForegroundColor Cyan
& $Body
}

$RepoRoot = $PSScriptRoot
Push-Location $RepoRoot

try {

# 1. Build both images and tag for the registry (delegates to build.ps1)
Invoke-Step "Building images" {
& "$RepoRoot\build.ps1" -Registry $Registry
}

# 2. Push images to the private registry
Invoke-Step "Pushing $ApiImage" {
docker push $ApiImage
}

Invoke-Step "Pushing $WebImage" {
docker push $WebImage
}

# 3. Copy compose file to Pi
$Remote = "${PiUser}@${PiHost}"
Invoke-Step "Copying docker-compose.yml to ${Remote}:${PiPath}" {
ssh $Remote "mkdir -p $PiPath"
scp docker-compose.yml "${Remote}:${PiPath}/"
}

# 4. Pull images and restart stack on Pi
# The script is written to a temp file and piped to bash -s to avoid
# quoting and escaping issues that break inline heredocs over SSH.
Invoke-Step "Pulling images and restarting stack on Pi" {
	$lines = @(
		"set -e",
		"cd $PiPath",
		"# Ensure the data directory exists with correct ownership for SQLite",
		"sudo mkdir -p /opt/armageddon/data",
		"sudo chown -R 1000:1000 /opt/armageddon/data",
		"printf 'Jwt__Key=%s\n' '$JwtKey' > .env",
		"docker compose pull",
		"docker compose up -d --remove-orphans"
	)
	$tmpScript = [System.IO.Path]::GetTempFileName()
	try {
		[System.IO.File]::WriteAllLines($tmpScript, $lines)
		Get-Content -Raw $tmpScript | ssh $Remote "bash -s"
	} finally {
		Remove-Item -ErrorAction SilentlyContinue $tmpScript
	}
}

Write-Host ""
Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "  API  -> http://${PiHost}:8080"
Write-Host "  Web  -> http://${PiHost}:8081"

} finally {
Pop-Location
}
