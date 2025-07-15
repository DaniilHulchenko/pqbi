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
 
Remove-DockerContainer -ContainerName "pqbi.ng"
Remove-DockerImagesByRepository -repository "nexus.elspec.local:8445/pqbi.ng"

Remove-DockerContainer -ContainerName "pqbi.host"
Remove-DockerImagesByRepository -repository "nexus.elspec.local:8445/pqbi.host"

docker stop pqbi.seq
docker rm pqbi.seq
docker rmi datalust/seq:2023.4
