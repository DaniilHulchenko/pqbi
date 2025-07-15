# The following script assists in preparing the environment for local testing(without dockers).
# This script cleans the SEQ logs, also creating a new db and in addition launching the web.host.dll in development mode.

param 
(
	[string]$seqLogsPath = "C:\Remove_Demos\Logs"
) 

. "./helpers/pqbiVariables.ps1"
. "./helpers/functions.ps1"


$seqDockerImage = "datalust/seq"
$seqDockerName = "SEQ_LOGGER"



RemoveDockerAndImage -dockerContainer $seqDockerName

Remove-Item $seqLogsPath -Force -Recurse -ErrorAction Ignore
New-Item -Path $seqLogsPath -ItemType Directory -Force


docker run -d --restart unless-stopped --name "${seqDockerName}" -e ACCEPT_EULA=Y -v "${seqLogsPath}:/data" -p 8090:80 "${seqDockerImage}:latest"
Get-Process *iis* | Stop-Process

& $dbCreateFile -workingDir $webHostFolder -entityFrameworkCoreCsproj $EntityFrameworkCoreCsprojFile


Write-Host "pQBIWebHostDll === ${pQBIWebHostDll}"

$csprojPath = "C:\PQSCADA\pqbi\aspnet-core\src\PQBI.Web.Host\bin\Debug\net6.0\PQBI.Web.Host.dll"  # Path to your .csproj file
$webHostDirectory = Split-Path -Path $pQBIWebHostDll -Parent

Set-Location $webHostDirectory
$env:ASPNETCORE_ENVIRONMENT = "Development"
# dotnet $pQBIWebHostDll --launch-profile "IIS Express"

# Test-Path $vsPath
# Test-Path $csprojPath

# Start-Process -FilePath $vsPath -ArgumentList "/debugexe", $csprojPath
# Invoke-Expression "cmd /c `"& `"$vsPath`" /debugexe `"$csprojPath`"`""


