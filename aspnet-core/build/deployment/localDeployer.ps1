param ( 
  [string]$pqsServiceUrl,
  [string]$gitVersion,
  [switch]$loadFromLocal, # if true, load from current dir instead of pulling from Nexus
  [switch]$runSeq,
  [string]$localPath 
)
 
 
function Update-ConfigFileWithIp {
  param (
    [string]$fileName,
    [string]$targetToken, # Pass the token without braces, e.g., "myIp"
    [string]$value,
    [string]$targetFile # New parameter to specify the file to save the updated content
  )
 
  if (Test-Path $fileName) {
    Write-Host "Found $fileName at $fileName"
 
    # Read the content of the file
    $content = Get-Content $fileName -Raw
 
    # Replace the placeholder with the actual value
    $updatedContent = $content -replace "\{\{$targetToken\}\}", $value
 
    Set-Content $targetFile -Value $updatedContent -Force
    Write-Host "Updated $targetFile with the value for '$targetToken'."
 
  }
  else {
    Write-Error "$fileName not found."
  }
}
 
 
function Remove-DockerResourcesByPrefix {
  param (
    [string]$prefix
  )
 
  # Remove Docker networks with the specified prefix
  $networks = docker network ls --format "{{.Name}}"
  $networksToRemove = $networks | Where-Object { $_ -like "$prefix*" }
  foreach ($network in $networksToRemove) {
    docker network rm $network
    Write-Host "Removed network: $network"
  }
 
  # Remove Docker containers with the specified prefix
  # $containers = docker ps -a --format "{{.Names}}"
  # $containersToRemove = $containers | Where-Object { $_ -like "$prefix*" }
  # foreach ($container in $containersToRemove) {
  #   docker rm -f $container
  #   Write-Host "Removed container: $container"
  # }
 
  # # Remove Docker images with the specified prefix
  # $images = docker images --format "{{.Repository}}"
  # $imagesToRemove = $images | Where-Object { $_ -like "$prefix*" }
  # foreach ($image in $imagesToRemove) {
  #   docker rmi -f $image
  #   Write-Host "Removed image: $image"
  # }
}
 
function Remove-DockerContainer {
  param (
    [string]$ContainerName,
    [bool]$Force = $true  # Default value set to true
  )

  # Check if the container exists
  $container = docker ps -a --filter "name=$ContainerName" --format "{{.Names}}"

  if (-not $container) {
    Write-Host "Container '$ContainerName' not found." -ForegroundColor Red
    return
  }

  # Stop the container if it's running
  $running = docker ps --filter "name=$ContainerName" --format "{{.Names}}"
  if ($running) {
    Write-Host "Stopping container '$ContainerName'..." -ForegroundColor Yellow
    docker stop $ContainerName | Out-Null
  }

  # Remove the container
  Write-Host "Removing container '$ContainerName'..." -ForegroundColor Green
  if ($Force) {
    docker rm -f $ContainerName
  }
  else {
    docker rm $ContainerName
  }

  Write-Host "Container '$ContainerName' removed successfully." -ForegroundColor Cyan
}

function Remove-DockerImagesByRepository {
  param (
    [string]$repository
  )
 
  # Get all Docker images with their repository and tag
  $images = docker images --format "{{.Repository}}:{{.Tag}}"
  # Filter images by the repository name
  $imagesToRemove = $images | Where-Object { $_ -like "$repository*" }
 
  foreach ($image in $imagesToRemove) {
    docker rmi -f $image
    Write-Host "Removed image: $image"
  }
}
 
# $gitVersion = "04d0c8d"
 
$NEXUS_USERNAME = "pqbi"
$NEXUS_PASSWD = "PQBI123"
 
$NEXUS_REPO_URL = "nexus.elspec.local:8443"
$NEXUS_DOCKER_HOSTED = "nexus.elspec.local:8445"
 
$APPSETTINGS_STAGING_FILE_JSON_NAME = "appsettings.staging.json"
$APPCONFIG_PRODUCTION_FILE_JSON_NAME = "appconfig.production.json"
$NGINX_CONF_FILE__NAME = "nginx.conf"
 
#----------------------------------------------------------------------------------------------------
#----------------------------------------------------------------------------------------------------
#----------------------------------------------------------------------------------------------------
 
 if ($loadFromLocal.IsPresent) {
	 
    if (-not $PSBoundParameters.ContainsKey('localPath')) {
        $currentPath = (Get-Location).Path
    }
    else {
        $currentPath = (Resolve-Path $localPath).Path
    }
}
else {
    # original behaviour
    $currentPath = (Get-Location).Path
}

# Write-Output "${currentPath}"

 
$binDirectoryPath = Join-Path $currentPath "bin"
 
$currentAppSettings = Join-Path $currentPath $APPSETTINGS_STAGING_FILE_JSON_NAME
$currentConfigProdction = Join-Path $currentPath $APPCONFIG_PRODUCTION_FILE_JSON_NAME
$currentConfignginx = Join-Path $currentPath $NGINX_CONF_FILE__NAME 

$appSettingsBinFile = Join-Path $binDirectoryPath $APPSETTINGS_STAGING_FILE_JSON_NAME 
$appConfigProdctionBinFile = Join-Path $binDirectoryPath $APPCONFIG_PRODUCTION_FILE_JSON_NAME 
$nginxBinFile = Join-Path $binDirectoryPath $NGINX_CONF_FILE__NAME
 
#----------------------------------------------------------------------------------------------------
#----------------------------------------------------------------------------------------------------
#----------------------------------------------------------------------------------------------------

Remove-Item -Path $binDirectoryPath -Recurse -Force

if (-Not (Test-Path $binDirectoryPath)) {
  Write-Output "The 'bin' directory does not exist. Creating it now..."
  New-Item -ItemType Directory -Path $binDirectoryPath | Out-Null
  Write-Output "'bin' directory created successfully."
}
else {
  Write-Output "The 'bin' directory already exists."
  Write-Output "The 'bin' directory already exists."
  Write-Output "The 'bin' directory already exists."
  exit 1


}
 
# $nexusHostUrl = "https://nexus.elspec.local:8443/repository/docker-hosted/v2/pqbi.host/manifests/cid1.rev${gitVersion}.staging"
# $nexusNgUrl = "https://nexus.elspec.local:8443/repository/docker-hosted/v2/pqbi.ng/manifests/cid1.rev${gitVersion}.staging"

Remove-DockerContainer -ContainerName "pqbi.ng"
Remove-DockerImagesByRepository -repository "nexus.elspec.local:8445/pqbi.ng"

Remove-DockerContainer -ContainerName "pqbi.host"
Remove-DockerImagesByRepository -repository "nexus.elspec.local:8445/pqbi.host"

docker stop pqbi.seq
docker rm pqbi.seq
docker rmi datalust/seq:2023.4

 
# Remove-DockerImagesByRepository -repository "nexus.elspec.local:8445/pqbi.ng"
Remove-DockerResourcesByPrefix -prefix "pqbi"
# Remove-DockerNetworksByPrefix -prefix "pqbi.network.cid"
 

$imagesDir  = ""
if ($loadFromLocal)
{
	$nexusHostUrl = "pqbi.host.cid1.rev${gitVersion}.tar"
	$nexusNgUrl = "pqbi.ng.cid1.rev${gitVersion}.tar"
	
	$imagesDir  = Join-Path $currentPath 'images'          # images\*.tar live beside the script
	$tarFiles   = @(
		$nexusHostUrl,   # Angular front-end image
		$nexusNgUrl      # Web-server image
	)
	
	Write-Host  "Loading Docker images that are packaged with the installer …"

	foreach ($tar in $tarFiles) {
		$tarPath = Join-Path $imagesDir $tar
		if (Test-Path $tarPath) {
			Write-Host "  → loading $tar"
			docker image load --input $tarPath       # same as: docker load -i $tarPath
		}
		else {
			throw "Image archive $tarPath not found. Aborting."
		}
	}
}
else
{
	Write-Output "------------------ Login Private Docker Hub ------------------"
	docker login "https://$($NEXUS_DOCKER_HOSTED)" -u $NEXUS_USERNAME -p $NEXUS_PASSWD

	$nexushosturl = "nexus.elspec.local:8445/pqbi.host:cid1.rev${gitversion}.staging"
	$nexusngurl = "nexus.elspec.local:8445/pqbi.ng:cid1.rev${gitversion}.staging"

	docker pull $nexusHostUrl
	docker pull $nexusNgUrl
}

 
# Get the local machine IP
$myIp = ""
try {
  $myIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias 'Ethernet*' | Select-Object -First 1).IPAddress
  Write-Host "Local machine IP: $myIp"
  Write-Host "Local machine IP: $myIp"
  Write-Host "Local machine IP: $myIp"
  Write-Host "Local machine IP: $myIp"
}
catch {
  Write-Error "Could not determine local IP address. Exiting."
  exit 1
}
 
 
# Update appsettings.staging.json with the local IP
Update-ConfigFileWithIp -fileName $currentAppSettings -targetToken "myIp" -value $myIp -targetFile $appSettingsBinFile
Update-ConfigFileWithIp -fileName $appSettingsBinFile -targetToken "pqsServiceUrl" -value $pqsServiceUrl -targetFile $appSettingsBinFile
 
 
# Update appconfig.production.json with the local IP
Update-ConfigFileWithIp -fileName $currentConfigProdction -targetToken "myIp" -value $myIp -targetFile $appConfigProdctionBinFile
 
 
Update-ConfigFileWithIp -fileName $currentConfignginx -targetToken "myIp" -value $myIp -targetFile $nginxBinFile
 
docker network create pqbi.network.cid_.rev$gitVersion
 
Write-Host "Docker host starting..."
Write-Host "Docker host starting..."
Write-Host "Docker host starting..."
 
Write-Output "${binDirectoryPath}/${APPSETTINGS_STAGING_FILE_JSON_NAME}"
 
Write-Host "bin: ${appSettingsBinFile} pwd: $(pwd) currentPath: $currentPath"

docker run -d `
  --name pqbi.host `
  --network pqbi.network.cid_.rev$gitVersion `
  -p 44301:443 `
  -v "${appSettingsBinFile}:/app/appsettings.staging.json" `
  -v "${currentPath}/docker_logs:/app/Logs" `
  -v "${currentPath}/PQBI_Db.db:/app/PQBI_Db.db" `
  -e "ASPNETCORE_ENVIRONMENT=staging" `
  -e "ASPNETCORE_URLS=https://+:443;http://+:80" `
  -e "ASPNETCORE_HTTPS_PORT=44301" `
  -e "Kestrel__Certificates__Default__Password=2825e4d9-5cef-4373-bed3-d7ebf59de216" `
  -e "Kestrel__Certificates__Default__Path=/https/pqbi-devcert-host.pfx" `
  -e "KestrelServer__IsEnabled=true" `
  -e "LOG_FILE_PATH=Logs/" `
  -e "SEQ_HOST_URL=http://pqbi.seq" `
  -e "BUILDER_REFERER=Local_Run" `
  nexus.elspec.local:8445/pqbi.host:cid1.rev$gitVersion.staging


 
 
Write-Host "Docker host is running..."
Write-Host "Docker host is running..."
Write-Host "Docker host is running..."
 
Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

if ($runSeq)
{
	Write-Host "Docker SEQ starting..."
	Write-Host "Docker SEQ starting..."
	Write-Host "Docker SEQ starting..."
	 
	Write-Output "${binDirectoryPath}/${APPSETTINGS_STAGING_FILE_JSON_NAME}"
	
	
	if (-not ($loadfromlocal))
	{
		write-output "------------------ pulling seq docker image ------------------"
		docker pull datalust/seq:2023.4
	}
	else
	{
		$tar = "pqbi_seq.tar"
		$tarpath = join-path $imagesdir $tar
		if (test-path $tarpath) {
			write-host "  → loading $tar"
			docker image load --input $tarpath       # same as: docker load -i $tarpath
		}
		else {
			throw "image archive $tarpath not found. aborting."
		}
	}
	
	# Run Seq container
	Write-Output "------------------ Starting Seq Container ------------------"
	docker run -d `
	  --name pqbi.seq `
	  -e ACCEPT_EULA=Y `
	  -p 8095:80 `
	  -p 45341:45341 `
	  --restart unless-stopped `
	  --network pqbi.network.cid_.rev$gitVersion `
	  datalust/seq:2023.4

	Write-Host "Docker SEQ is running..."
	Write-Host "Docker SEQ is running..."
	Write-Host "Docker SEQ is running..."

	Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
	Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
	Write-Host "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
}

 
 
Write-Host "Docker client is starting..."
Write-Host "Docker client is starting..."
Write-Host "Docker client is starting..."
 
 
# $appConfigProdctionBinFile = Join-Path $binDirectoryPath $APPCONFIG_PRODUCTION_FILE_JSON_NAME 
# $nginxBinFile = Join-Path $binDirectoryPath $NGINX_CONF_FILE__NAME 
docker run -d `
  --name pqbi.ng `
  --network "pqbi.network.cid_.rev$gitVersion" `
  -p 443:443 `
  -v "${nginxBinFile}:/etc/nginx/nginx.conf:ro" `
  -v "${appConfigProdctionBinFile}:/usr/share/nginx/html/assets/appconfig.production.json" `
  "nexus.elspec.local:8445/pqbi.ng:cid1.rev$gitVersion.staging"
 
Write-Host "Docker client is running..."
Write-Host "Docker client is running..."
Write-Host "Docker client is running..."
 
 
Write-Host "xxx - xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
Write-Host "xxx - xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
Write-Host "xxx - xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"