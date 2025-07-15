#   This script intended to run on tests env
# @$configName - PQBI config single/multi tenant
# @$testsGroup - Cypress E2E tests group, comes from PQBI.E2E project
param ( 
    [string]$configName ,
    [string]$testsGroup 
)

. "./containerRunner.ps1"


$isFinishedSuccessful = $true

try {
    Execute -configName $configName -testsGroup $testsGroup  -deploymentType "testing"
}
catch {
    Write-Host "An exception occurred: $($_.Exception.Message)"
    $isFinishedSuccessful = $false
}

$result = 0
if ($isFinishedSuccessful -eq $true) {
    Write-Host "Testing finished successfully."
}
else {
    Write-Host "Testing faild."
    $result = 1
}

return $result

