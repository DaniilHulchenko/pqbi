function  RemoveDockerAndImage {
	param (
		[string]$dockerImage = "",
		[string]$dockerContainer = "",
		[string]$dockerNetwork = ""
	)

	Write-Host "Remove docker with an image of $dockerContainer"
	
	if ($dockerContainer) {
		Write-Host "Stoping and removing docker  $dockerContainer"
		docker stop $dockerContainer
		docker rm $dockerContainer
	}
	
	if ($dockerImage) {
		Write-Host "Removing docker image  $dockerImage"
		docker rmi $dockerImage
	}
	
	if ($dockerNetwork) {
		Write-Host "Removing docker network  $dockerNetwork"
		docker network rm $dockerNetwork
	}
}

function PlainBuilder {

	param 
	(
		[string]$targetDirectory = ""
	) 

	Remove-Item $targetDirectory -Force -Recurse -ErrorAction Ignore
		
	$preBin = "pre"
		
	Write-Host "Building $targetDirectory structure"

	New-Item -Path (Join-Path $targetDirectory $preBin) -ItemType Directory -Force
		
}


function SmartCopy {
	param(
		[string]$SourceDirectory,
		[string]$DestinationDirectory,
		[string []]$excludeItems
	)

		Write-Host "Copy Operation from [$SourceDirectory] to  $DestinationDirectory started."

		SmartCopyExecuter -SourceDirectory $SourceDirectory -DestinationDirectory $DestinationDirectory -excludeItems $excludeItems

		Write-Host "Copy Operation from [$SourceDirectory] to  $DestinationDirectory finished."
}


function SmartCopyExecuter {
	param(
		[string]$SourceDirectory,
		[string]$DestinationDirectory,
		[string []]$excludeItems
	)

	if (-not (Test-Path -Path $SourceDirectory -PathType Container)) {
		Write-Error "Source directory '$SourceDirectory' not found."
		return
	}

	if (-not (Test-Path -Path $DestinationDirectory -PathType Container)) {
		New-Item -Path $DestinationDirectory -ItemType Directory -Force | Out-Null
	}

	$items = Get-ChildItem -Path $SourceDirectory

	foreach ($item in $items) {
		$itemName = $item.Name.ToLower()
		if ($excludeItems -notcontains $itemName) {
			$destinationPath = Join-Path -Path $DestinationDirectory -ChildPath $item.Name

			if ($item.PSIsContainer) {
				SmartCopyExecuter -SourceDirectory $item.FullName -DestinationDirectory $destinationPath -excludeItems $excludeItems
			}
			else {
				Copy-Item -Path $item.FullName -Destination $destinationPath -Force
			}
		}
	}
}

