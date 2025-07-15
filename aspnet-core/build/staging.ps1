# This script intended to run on staging env
# @$configName - PQBI config single/multi tenant
# @$pqsServiceUrl - URL of PQSCADA Service
param ( 
    [string]$configName ,
    [string]$pqsServiceUrl
)
. "./containerRunner.ps1"

$isFinishedSuccessful = $true

try {
    Execute -configName $configName -testsGroup $testsGroup -pqsServiceUrl $pqsServiceUrl -deploymentType "staging"
}
catch {
    Write-Host "An exception occurred: $($_.Exception.Message)"
    $isFinishedSuccessful = $false
}

if ($isFinishedSuccessful -eq $true) {
    Write-Host "Deployment of staging has finished successfully."
}
else {
    
    Write-Host "Deployment faild."
}
