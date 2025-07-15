#!/bin/bash

# COMMON PATHS

buildFolder=$(realpath "./")
logsStash=$(dirname $(dirname "$buildFolder"))

slnFolder=$(dirname "$buildFolder")

# Host
outputFolder="$buildFolder/outputs"
webHostFolder="$slnFolder/src/PQBI.Web.Host"
pQBIWebHostDll="$webHostFolder/bin/Debug/net6.0/PQBI.Web.Host.dll"
appSettingsJonFilePath="$webHostFolder/appsettings.json"

# Simulator
pqsServiceMockBin="$outputFolder/pqsServiceMock"
pqsMockServicePre="$pqsServiceMockBin/pre"
pqsMockServiceBin="$pqsServiceMockBin/bin"
pqsMockServiceConfigFolder="$buildFolder/pqs-service-mock"
outputPQSServiceMock="$pqsMockServicePre/Utilities/PQSServiceMock"
pqsServiceMockDockerComposeDockerFile="$pqsMockServiceBin/docker-compose-pqs-service-mock.yml"
pqsServiceMockDll="$pqsMockServiceBin/pqsServiceMock.dll"
pqsServiceMockFolder="$slnFolder/Utilities/PQSServiceMock"
pqsServiceMockCsprojFile="$pqsServiceMockFolder/PQSServiceMock.csproj"
pqsServiceMockDockerConfigFilesFolder="$outputFolder/../pqs-service-mock"

dbCreateFile="$slnFolder/scripts/DbBuilder.ps1"
EntityFrameworkCoreCsprojFile="$slnFolder/src/PQBI.EntityFrameworkCore/PQBI.EntityFrameworkCore.csproj"
hostCoreCsprojFile="$slnFolder/src/PQBI.Web.Host/PQBI.Web.Host.csproj"
angularFolder="$buildFolder/../../angular"

# If a developer running on his local machine
isDeveloperRun=true

# Test
pqbiE2e="$buildFolder/../../../PQBI.E2E"
pqbiE2eNodeModules="$pqbiE2e/node_modules"

# pqbi-host
pqbi="$outputFolder/Host"
hostPre="$pqbi/pre"
hostBin="$pqbi/bin"
outputHost="$hostPre/src/PQBI.Web.Host"
dockerHostConfigurations="$hostPre/docker_configurations"
dockerComposePqbiFilePath="$hostPre/docker-compose-pqbi.yml"
pqbiHostDockerConfigFilesFolder="$outputFolder/../pqbi-host"

# pqbi-ng
ng="$outputFolder/ng"
ngPre="$ng/pre"
ngBin="$ng/bin"
ngPreAssets="$ngPre/src/assets"
ngBinAssests="$ngBin/src/assets"
dockerNgConfigurations="$ngPre/docker_configurations"

ngConfigurationAppSettingProductionFile="$ngPre/docker_configurations/appconfig.production.json"
ngDockerConfigFilesFolder="$outputFolder/../pqbi-ng"

# pqbi-tests
pqbiTestsFolder="$outputFolder/tests"
testsBin="$pqbiTestsFolder/pre"
specsInputFolder="$testsBin/specs"
pqbiE2eSpecsFolder="$pqbiE2e/cypress/e2e"
testsDockerConfigFolder="$outputFolder/../pqbi-tests"
testBuildConfigDir="$testsDockerConfigFolder/build_configs"
scenarioConfigDirectoryPath1="$testBuildConfigDir/1__single_tenant"
scenarioConfigDirectoryPath2="$testBuildConfigDir/2__multi_tenant"
dockerComposeTestFilePath="$testsBin/docker-compose-tests.yml"

# pqbi-global
globalConfigsFilesFolder="$buildFolder/pqbi-global"

# Build
BUILDER_REFERER="Local_Run"

declare -A locationIndexerPaths

locationIndexerPaths=(
    [host]="$hostPre"
    [ng]="$ngPre"
    [tests]="$testsBin"
    [pqsServiceMock]="$pqsMockServicePre"
    [package.json]="$testsBin"
)