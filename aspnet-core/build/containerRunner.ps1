

. "./helpers/env.ps1"
. "./helpers/pqbiVariables.ps1"
. "./helpers/functions.ps1"
. "./helpers/ScenarioRunner.ps1"

function GetScenarioInfo_E2E {
	param (
		[string]$scenariosFolder,
		[hashtable]$commonScenarioProperties,
		[ScenarioInfo]$previousScenario
	)
		
	$scenarioInfo = [ScenarioInfo]::new()
	$ScenarioInfo.ScenarioDirectoryPath = $scenariosFolder
	$ScenarioInfo.PropsFilePath = Join-Path $scenariosFolder "props.txt"
	$ScenarioInfo.ScenarioOverridedProps = PropFileDissect $ScenarioInfo.PropsFilePath $commonScenarioProperties

	return $scenarioInfo
}

function IsNexusPackageExist {
	param 
	(
		[string]$dockerImage,
		[string]$nexusUrl
	) 
		
	$dockerImageParts = $dockerImage.Split(":")
	$dockerImageName = $dockerImageParts[0]
	$dockerImageVersion = $dockerImageParts[1]
		
	$uri = "https://$nexusUrl/repository/docker-hosted/v2/${dockerImageName}/manifests/${dockerImageVersion}";
	try {
		$response = Invoke-WebRequest -Uri $uri -UseBasicParsing
		return $true
	}
	catch {
		return $false
	}
}


function BuildPqbiHost {
	param (
		[hashtable]$scenarioParams,
		[string]$scenarioConfigDirectoryPath,
		[DefaultEnvBase]$dockerImageWorkItem
	)

	RemoveDockerAndImage -dockerImage $dockerImageWorkItem.HOST_NEXUS_IMAGE_REVISION -dockerContainer $dockerImageWorkItem.HOST_CONTAINER
	RemoveDockerAndImage -dockerContainer $dockerImageWorkItem.HOST_SEQ_CONTAINER
	RemoveDockerAndImage -dockerImage "postgres" -dockerContainer $dockerImageWorkItem.HOST_POSTGRES_DB_CONTAINER
		
	PlainBuilder -targetDirectory $pqbi

	$isMultiTenant = $dockerImageWorkItem.HostComp.IsMultiTenant
		
	$hostVaribles = @{
		'PQBI_HOST_CONTAINER'        = $dockerImageWorkItem.HOST_CONTAINER
		'HOST_POSTGRES_DB_CONTAINER' = $dockerImageWorkItem.HOST_POSTGRES_DB_CONTAINER
		'PQBI_HOST_IMAGE'            = $dockerImageWorkItem.PQBI_HOST_IMAGE
		'PQBI_HOST_SEQ_CONTAINER'    = $dockerImageWorkItem.HOST_SEQ_CONTAINER
		'NETWORK_NAME'               = $dockerImageWorkItem.NETWORK_NAME
		'BUILDER_REFERER'            = $BUILDER_REFERER
		'NEXUS_DOCKER_HOSTED'        = $dockerImageWorkItem.NEXUS_DOCKER_HOSTED
		'ASPNETCORE_ENVIRONMENT'     = $dockerImageWorkItem.HOST_DEPLOYMENT_TYPE
		'HOST_DOCKER_INTERNAL_IP'    = $dockerImageWorkItem.HOST_DOCKER_INTERNAL_IP
	}

	$hostVaribles += $scenarioParams
	
	$locations = @{
		'appsettings.Development.json' = $outputHost, $dockerHostConfigurations
		'appsettings.Staging.json'     = $outputHost, $dockerHostConfigurations
		'appsettings.Production.json'  = $outputHost
	}
	$locations += $locationIndexerPaths

	
	ScenarioDataOrganizer  $pqbiHostDockerConfigFilesFolder  $locations -prefixFiles @('host')  $hostVaribles  $dockerImageWorkItem.HOST_DEPLOYMENT_TYPE
	ScenarioDataOrganizer  $scenarioConfigDirectoryPath  $locations -prefixFiles @("host")   $hostVaribles  $dockerImageWorkItem.HOST_DEPLOYMENT_TYPE
	Copy-Item -Path (Join-Path $globalConfigsFilesFolder "\*") -Destination $hostPre -Recurse -Force
	Copy-Item (Join-Path $slnFolder "nuget.config")  -Destination $hostPre -Recurse -Force
	
	if (($isMultiTenant -eq $true -or (IsNexusPackageExist -dockerImage $dockerImageWorkItem.PQBI_HOST_IMAGE -nexusUrl $dockerImageWorkItem.NEXUS_REPO_URL) -eq $false)) {
		Set-Location $webHostFolder
		Copy-Item (Join-Path $pqbiHostDockerConfigFilesFolder "*") $hostPre
		
		Set-Location $slnFolder

		$excludeItems = "aspnetzeroradtool", "build", "docker", "scripts", "etc", ".vs", "bin", "obj", "appsettings.Staging.json", "appsettings.Production.json", "PQBI_Db.db"
		if ($isMultiTenant -eq $true) {
			$excludeItems += "SQLite"
		}
		else {
			$excludeItems += "Postgres"
		}

		SmartCopy -SourceDirectory $slnFolder  -DestinationDirectory  $hostPre -excludeItems $excludeItems
		
	}

	if ($isMultiTenant -eq $true) {
		Set-Location $hostPre

		docker-compose -f postgres-docker-compose.yml up -d
	}
	
	Set-Location $hostPre
	
	DockerLaunchAndPush -fileComposePath $dockerComposePqbiFilePath -isDetachMode $true  -dockerImage $dockerImageWorkItem.PQBI_HOST_IMAGE -nexusHostPush $dockerImageWorkItem.NEXUS_DOCKER_HOSTED -nexusHostCheck $dockerImageWorkItem.NEXUS_REPO_URL -isPublish $dockerImageWorkItem.IsPublishDockerToNexus -isRebuildImage $isMultiTenant
}

function BuildPQSServiceMock {
	param (
		[hashtable]$scenarioParams,
		[string]$scenarioConfigDirectoryPath,
		[DefaultEnvBase]$dockerImageWorkItem
	)
		
	if ($dockerImageWorkItem.MockServiceComp.IsActive -eq $false) {
		return
	}
		
		
	RemoveDockerAndImage -dockerImage $dockerImageWorkItem.PQSSERVICE_NEXUS_IMAGE_REVISION -dockerContainer $dockerImageWorkItem.PQSSERVICE_MOCK_CONTAINER
		
	PlainBuilder -targetDirectory $pqsServiceMockBin
		
	$simulatorInitialData = @{
		'PQBI_PQSSERVICE_MOCK_IMAGE'     = $dockerImageWorkItem.PQBI_PQSSERVICE_MOCK_IMAGE
		'PQBI_PQSSERVICE_MOCK_CONTAINER' = $dockerImageWorkItem.PQSSERVICE_MOCK_CONTAINER
		'NETWORK_NAME'                   = $dockerImageWorkItem.NETWORK_NAME
		'NEXUS_DOCKER_HOSTED'            = $dockerImageWorkItem.NEXUS_DOCKER_HOSTED
		'ASPNETCORE_ENVIRONMENT'         = "Development"
		
	}
	
	$simulatorInitialData += $scenarioParams
	
	$mockLocation = @{
		'appsettings.Development.json' = $outputPQSServiceMock
		'appsettings.Staging.json'     = $outputPQSServiceMock
		'appsettings.Production.json'  = $outputPQSServiceMock
	}
	
	$mockLocation += $locationIndexerPaths
	
	ScenarioDataOrganizer -sourceDir $pqsServiceMockDockerConfigFilesFolder -locationIndex $mockLocation -prefixFiles @("pqsServiceMock") -valueMapper  $simulatorInitialData
	Copy-Item -Path (Join-Path  $globalConfigsFilesFolder "\*")  -Destination $pqsMockServicePre -Recurse -Force
	Copy-Item (Join-Path $slnFolder "nuget.config")  -Destination $pqsMockServicePre -Recurse -Force
	
	
	if ((IsNexusPackageExist -dockerImage $dockerImageWorkItem.PQBI_PQSSERVICE_MOCK_IMAGE  -nexusUrl $dockerImageWorkItem.NEXUS_REPO_URL) -eq $false) {
		Set-Location $slnFolder
		SmartCopy -SourceDirectory $slnFolder  -DestinationDirectory  $pqsMockServicePre -excludeItems "aspnetzeroradtool", "build", "docker", "scripts", "etc", ".vs", "bin", "obj", "appsettings.Development.json"
	}
	
	Set-Location $pqsMockServicePre
	DockerLaunchAndPush -fileComposePath "docker-compose-pqs-service-mock.yml"  -isDetachMode $true -dockerImage $dockerImageWorkItem.PQBI_PQSSERVICE_MOCK_IMAGE   -nexusHostPush $dockerImageWorkItem.NEXUS_DOCKER_HOSTED -nexusHostCheck $dockerImageWorkItem.NEXUS_REPO_URL -isPublish $dockerImageWorkItem.IsPublishDockerToNexus -isRebuildImage $false
}


function BuildNg {
	param (
		[hashtable]$scenarioParams,
		[DefaultEnvBase]$dockerImageWorkItem
	)
		
	RemoveDockerAndImage -dockerImage $dockerImageWorkItem.FRONT_NEXUS_MAGE_REVISION -dockerContainer $dockerImageWorkItem.FRONT_CONTAINER
	PlainBuilder -targetDirectory $ng

	# ----------------------------angular config value injections--------------------------

	Set-Location $angularFolder
	# Copy-Item (Join-Path $angularFolder "\*") $ngPre -Force  -Recurse -Exclude "node_modules", "dist", ".angular", ".vscode", "nswag", ".vs"
	Copy-Item (Join-Path $angularFolder "\*") $ngPre -Force -Recurse -Exclude "node_modules", "dist", ".angular", ".vscode", "nswag", ".vs" -Verbose
	

	
	Set-Location $ngPre

	$ngVaribles = @{
		'PQBI_FRONT_IMAGE'     = $dockerImageWorkItem.PQBI_FRONT_IMAGE
		'PQBI_FRONT_CONTAINER' = $dockerImageWorkItem.FRONT_CONTAINER
		'NETWORK_NAME'         = $dockerImageWorkItem.NETWORK_NAME
		'NEXUS_DOCKER_HOSTED'  = $dockerImageWorkItem.NEXUS_DOCKER_HOSTED
		'NEXUS_YARN_PROXY_URL' = $dockerImageWorkItem.NEXUS_YARN_PROXY_URL
	}

	$ngVaribles += $scenarioParams
	
	$ngLocation = @{
		'appconfig.production.json' = $ngPreAssets, $dockerNgConfigurations
	}
	
	$ngLocation += $locationIndexerPaths
	
	ScenarioDataOrganizer -sourceDir $ngDockerConfigFilesFolder -locationIndex $ngLocation -prefixFiles @("ng") -valueMapper  $ngVaribles -env  $dockerImageWorkItem.ASPNETCORE_ENVIRONMENT
	
	Copy-Item -Path (Join-Path  $globalConfigsFilesFolder "\*")  -Destination $ngPre -Recurse -Force
	
	Set-Location $ngPre
	DockerLaunchAndPush -fileComposePath "docker-compose-ng.yml" -isDetachMode $true -dockerImage  $dockerImageWorkItem.PQBI_FRONT_IMAGE   -nexusHostPush $dockerImageWorkItem.NEXUS_DOCKER_HOSTED -nexusHostCheck $dockerImageWorkItem.NEXUS_REPO_URL  -isPublish $dockerImageWorkItem.IsPublishDockerToNexus -isRebuildImage $false
	
}


function BuildFullTests {
	param (
		[string]$scenarioConfigDirectoryPath,
		[hashtable]$testsVaribles = @{},
		[string]$configName,
		[string]$testsGroup,
		[DefaultEnvBase]$dockerImageWorkItem
	)
		
		
	if ($dockerImageWorkItem.TestsComp.IsActive -eq $false) {
		return
	}

	$specFilesFolder = Join-Path $pqbiE2eSpecsFolder "\"
	$specFilesFolder = Join-Path $specFilesFolder $configName
	$specFilesFolder = Join-Path $specFilesFolder "\"
	$specFilesFolder = Join-Path $specFilesFolder $testsGroup
		
	$specFilesFolder = Resolve-Path $specFilesFolder
		
	RemoveDockerAndImage -dockerImage $dockerImageWorkItem.TESTS_NEXUS_IMAGE_REVISION -dockerContainer $dockerImageWorkItem.TESTS_CONTAINER
	PlainBuilder -targetDirectory $pqbiTestsFolder
		
	Copy-Item -Path (Join-Path $pqbiE2e "\*") -Destination $testsBin -Recurse -Exclude "node_modules", ".git" -Force
	Copy-Item -Path (Join-Path  $globalConfigsFilesFolder "\*")  -Destination $testsBin -Recurse -Force
		
	New-Item -Path $specsInputFolder -ItemType Directory -Force
	Copy-Item -Path (Join-Path $specFilesFolder "\*") -Destination $specsInputFolder -Recurse  -Force
		
	Set-Location $testsBin

	# ----------------------------tests docker compose value injections--------------------------
	
	$testsDockerComposeVaribles = @{
		'PQBI_TESTS_IMAGE'     = $dockerImageWorkItem.PQBI_TESTS_IMAGE
		'PQBI_TESTS_CONTAINER' = $dockerImageWorkItem.TESTS_CONTAINER
		'NETWORK_NAME'         = $dockerImageWorkItem.NETWORK_NAME
		'NPM_COMMAND_PROPERTY' = $testsVaribles['NPM_COMMAND_PROPERTY']
		'NEXUS_DOCKER_HOSTED'  = $dockerImageWorkItem.NEXUS_DOCKER_HOSTED
		'NEXUS_NPM_PROXY_URL'  = $dockerImageWorkItem.NEXUS_NPM_PROXY_URL
		'NEXUS_CYPRESS_BIN'    = $dockerImageWorkItem.NEXUS_CYPRESS_BIN
		'CLIENT_ROOT_ADDRESS'  = $dockerImageWorkItem.CLIENT_ROOT_ADDRESS
		'CYPRESS_VERSION'      = $dockerImageWorkItem.CYPRESS_VERSION
	}

	ScenarioDataOrganizer -sourceDir $testsDockerConfigFolder -locationIndex $locationIndexerPaths -prefixFiles @("tests") -valueMapper  $testsDockerComposeVaribles
	
	ScenarioDataOrganizer -sourceDir $scenarioConfigDirectoryPath -locationIndex $locationIndexerPaths -prefixFiles "tests" -valueMapper  $testsVaribles
	DockerLaunchAndPush -fileCompose $dockerComposeTestFilePath -dockerImage $dockerImageWorkItem.PQBI_TESTS_IMAGE    -nexusHostPush $dockerImageWorkItem.NEXUS_DOCKER_HOSTED -nexusHostCheck $dockerImageWorkItem.NEXUS_REPO_URL  -isPublish $dockerImageWorkItem.IsPublishDockerToNexus -isRebuildImage $false
}


function DockerLaunchandpush {
	param 
	(
		[string]$fileComposePath,
		[bool]$isDetachMode,
		[string]$dockerImage,
		[string]$nexusHostPush,
		[string]$nexusHostCheck,
		[bool]$isPublish,
		[bool]$isRebuildImage
	) 
		
		
	$dockerComposeArgs = @("-f", $fileComposePath, "up")
	if ($isDetachMode) {
		$dockerComposeArgs += "-d"
	}

	
			
	$isImageExistedInNexus = IsNexusPackageExist -dockerImage $dockerImage -nexusUrl $nexusHostCheck;
			
	if ($isImageExistedInNexus -eq $false) {
		Write-Output "------------------ Building Image ------------------"
		try {
			docker-compose --file $fileComposePath build --progress plain
		}
		catch {
			Write-Output "Error: $_"
			throw
		}
	}
	else {
		if ($isRebuildImage) {
			$dockerComposeArgs += "--build"
		}
	} 

		
	Write-Output "------------------ Container run ------------------"
	$env:CACHEBUST = Get-Date -Format "yyyyMMddHHmmss"; docker-compose @dockerComposeArgs
		
	if ( $isImageExistedInNexus -eq $false) {
		if ($isPublish) {

			Write-Output "------------------ Publishing Image to Nexus ------------------"
			docker push "${nexusHostPush}/${dockerImage}"
		}
	}
}

function createEnvironment {
	param 
	(
		[string]$gitRevision,
		[string]$pqsServiceUrl,
		[string] $env,
		[string] $cid,
		[bool] $isMultiTenant
	) 
		
	$env = $env.ToLower()
		
	switch ($env) {
			
		"testing" {
			return [TestingEnv]::new($gitRevision, $cid, $isMultiTenant)
		}
			
		Default {
			return [DeploymentEnv]::new($gitRevision, $pqsServiceUrl, $cid, $env, $isMultiTenant)
		}
	}
}

#   This script intended to run on tests env
# @$configName - PQBI config single/multi tenant
# @$testsGroup - Cypress E2E tests group, comes from PQBI.E2E project
# @$pqsServiceUrl - URL of PQSCADA Service
function Execute {
	param ( 
		[string]$configName ,
		[string]$testsGroup = "" ,
		[string]$pqsServiceUrl = "",
		[string]$deploymentType = ""
	)

	# 	Set-Location $testsBin
	# docker-compose --file docker-compose-tests.yml up 
	
	$scenarioPath = ''
	$isScenarioNamefound = $false
	
	foreach ($item in Get-ChildItem -Path $testBuildConfigDir ) {
		if ($configName.ToLower() -eq $item.Name.ToLower()) {
			$isScenarioNamefound = $true
			$scenarioPath = $item.FullName
		}
	}
	
	if ($isScenarioNamefound -eq $false) {
		throw New-Object System.Exception -ArgumentList "Such scenario was not found."
	}
	
	$scenarioInfo = [ScenarioInfo]::new()
	$gitRevision = $(git rev-parse --short master)    	
	
	$isMultiTenant = $true

	if ($configName -cmatch "single") {
		$isMultiTenant = $false
	}

	$dockerImageWorkItem = createEnvironment -gitRevision  $gitRevision -pqsServiceUrl $pqsServiceUrl -env $deploymentType -cid $configName[0] -isMultiTenant $isMultiTenant
	Write-Output "------------------ Login Private Docker Hub ------------------"
	docker login "https://$($dockerImageWorkItem.NEXUS_DOCKER_HOSTED)" -u $dockerImageWorkItem.NEXUS_USERNAME -p $dockerImageWorkItem.NEXUS_PASSWD

	$ScenarioProperties = InitiateMainScenarioProperties -dockerImageWorkItem $dockerImageWorkItem  -pqsServiceUrl $pqsServiceUrl


	$scenarioInfo = GetScenarioInfo_E2E $scenarioPath $ScenarioProperties $scenarioInfo
	$testvaribles = $scenarioInfo.ScenarioOverridedProps
	

	BuildPqbiHost -scenarioParams $ScenarioProperties -scenarioConfigDirectoryPath $scenarioInfo.ScenarioDirectoryPath -dockerImageWorkItem $dockerImageWorkItem


	BuildPQSServiceMock -scenarioParams $ScenarioProperties -scenarioConfigDirectoryPath $scenarioInfo.ScenarioDirectoryPath -dockerImageWorkItem $dockerImageWorkItem

	# Write-Output "------------------ BuildNg_E2E ------------------"
	BuildNg -scenarioParams $ScenarioProperties   -dockerImageWorkItem $dockerImageWorkItem

	# $testvaribles += $ScenarioProperties
	# BuildFullTests -scenarioConfigDirectoryPath $scenarioInfo.ScenarioDirectoryPath -testsVaribles $testvaribles -configName $configName   -testsGroup $testsGroup  -dockerImageWorkItem $dockerImageWorkItem



	 
}