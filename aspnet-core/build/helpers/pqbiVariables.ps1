# COMMON PATHS
		
$buildFolder = (Get-Item -Path "./" -Verbose).FullName
$logsStash = Split-Path (Split-Path $buildFolder -Parent) -Parent

$slnFolder = Split-Path -Path $buildFolder -Parent
# Host
$outputFolder = Join-Path $buildFolder "outputs"
$webHostFolder = Join-Path $slnFolder "src/PQBI.Web.Host"
$pQBIWebHostDll = Join-Path $webHostFolder "bin/Debug/net6.0/PQBI.Web.Host.dll"

$appSettingsJonFilePath = Join-Path $webHostFolder "appsettings.json"

# Simulator
$outputFolder = Join-Path $buildFolder "outputs"
$pqsServiceMockBin = (Join-Path $outputFolder "pqsServiceMock")
$pqsMockServicePre = (Join-Path $pqsServiceMockBin "pre")
$pqsMockServiceBin = (Join-Path $pqsServiceMockBin "bin")
$pqsMockServiceConfigFolder = (Join-Path $buildFolder 'pqs-service-mock')
$outputPQSServiceMock = (Join-Path $pqsMockServicePre 'Utilities/PQSServiceMock')

$pqsServiceMockDockerComposeDockerFile = (Join-Path $pqsMockServiceBin  "docker-compose-pqs-service-mock.yml")
$pqsServiceMockDll = Join-Path $pqsMockServiceBin "pqsServiceMock.dll"

$pqsServiceMockFolder = Join-Path $slnFolder "Utilities\PQSServiceMock"
$pqsServiceMockCsprojFile = Join-Path $pqsServiceMockFolder "PQSServiceMock.csproj"
$pqsServiceMockDockerConfigFilesFolder = Join-Path $outputFolder "../pqs-service-mock"





$dbCreateFile = Join-Path $slnFolder "scripts/DbBuilder.ps1"
$EntityFrameworkCoreCsprojFile = Join-Path $slnFolder "src/PQBI.EntityFrameworkCore/PQBI.EntityFrameworkCore.csproj"
$hostCoreCsprojFile = Join-Path $slnFolder "src/PQBI.Web.Host/PQBI.Web.Host.csproj"
$angularFolder = Join-Path $buildFolder "../../angular"

# If a develoer running on his local machine
$isDeveloperRun = $true

# Test
$pqbiE2e = Join-Path $buildFolder "../../../pqbi.e2e"
$pqbiE2eNodeModules = Join-Path $pqbiE2e "node_modules"

################################pqbi-host#######################################
###############################################################################
###############################################################################

$pqbi = (Join-Path $outputFolder "Host")
$hostPre = (Join-Path $pqbi "pre")
$hostBin = (Join-Path $pqbi "bin")

$outputHost = (Join-Path $hostPre "src/PQBI.Web.Host")
$dockerHostConfigurations = (Join-Path $hostPre "docker_configurations")



$dockerComposePqbiFilePath = Join-Path $hostPre "docker-compose-pqbi.yml"
$pqbiHostDockerConfigFilesFolder = Join-Path $outputFolder "../pqbi-host"
# $hostDockerTemplateConfigFolder = Join-Path $pqbiHostDockerConfigFilesFolder "template-configs"

#################################pqbi-ng#######################################
###############################################################################
###############################################################################

$ng = (Join-Path $outputFolder "ng")
$ngPre = (Join-Path $ng "pre")
$ngBin = (Join-Path $ng "bin")
$ngPreAssets = (Join-Path $ngPre "src/assets")
$ngBinAssests = (Join-Path $ngBin "src/assets")
$dockerNgConfigurations = (Join-Path $ngPre "docker_configurations")


$ngDockerConfigFilesFolder = Join-Path $outputFolder "../pqbi-ng"

##############################pqbi-tests#######################################
###############################################################################
###############################################################################

$pqbiTestsFolder = Join-Path $outputFolder "tests"
$testsBin = Join-Path $pqbiTestsFolder "pre"
$specsInputFolder = Join-Path $testsBin "specs"
$pqbiE2eSpecsFolder = Join-Path $pqbiE2e "cypress/e2e"
$testsDockerConfigFolder = Join-Path $outputFolder "../pqbi-tests"
$testBuildConfigDir = (Join-Path $testsDockerConfigFolder "build_configs")
$scenarioConfigDirectoryPath1 = (Join-Path $testBuildConfigDir "1__single_tenant")
$scenarioConfigDirectoryPath2 = (Join-Path $testBuildConfigDir "2__multi_tenant")

$dockerComposeTestFilePath = (Join-Path $testsBin "docker-compose-tests.yml");

##############################pqbi-global######################################
###############################################################################
###############################################################################

$globalConfigsFilesFolder = Join-Path $buildFolder "/pqbi-global"


$locationIndexerPaths = @{
	
    'host'                      = $hostPre
    'ng'                        = $ngPre
    'tests'                     = $testsBin
    'pqsServiceMock'            = $pqsMockServicePre
    'package.json'              = $testsBin

}



# Build
$BUILDER_REFERER = "Local_Run"

class ScenarioInfo {
    # [int]$ScenarioIndex
    [string]$ScenarioDirectoryPath
    [string]$PropsFilePath
    [hashtable]$ScenarioOverridedProps
    # [ComponentsInfo]$Components = [ComponentsInfo]::new()
}

