# Define variables
$containerName = "seq"
$logPath = "C:\Remove_Demos\Logs"

# Stop and remove the container if it exists
if ($(docker ps -aq -f name=$containerName)) {
    Write-Host "Stopping and removing existing container: $containerName"
    docker stop $containerName
    docker rm $containerName
} else {
    Write-Host "Container '$containerName' not found, skipping removal."
}

# Clean up the Logs directory
if (Test-Path $logPath) {
    Write-Host "Cleaning up logs directory: $logPath"
    Remove-Item -Path "$logPath\*" -Force -Recurse
} else {
    Write-Host "Log directory not found, creating: $logPath"
    New-Item -ItemType Directory -Path $logPath | Out-Null
}

# Run the new Docker container (Fixed variable reference)
Write-Host "Starting new Docker container: $containerName"
docker run -d --restart unless-stopped --name seq -e ACCEPT_EULA=Y -v "${logPath}:/data" -p 8090:80 datalust/seq:latest

Write-Host "Docker container restarted successfully!"
