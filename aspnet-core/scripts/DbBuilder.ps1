
#This script creates a database from sctratch

param (
    [string]$workingDir, 
    [string]$entityFrameworkCoreCsproj, 
    [bool]$injectConnectionString = $false # If $true, May change appsettings.json indentation
)

function RelocateMigration {
    param (
        [string]$sourceDirectory,
        [string]$destinationDirectory
    ) 
        
    
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        
        New-Item -Path $destinationDirectory -ItemType Directory -Force
    }
    
    $files = Get-ChildItem -Path $sourceDirectory -File
    foreach ($file in $files) {
        $destinationPath = Join-Path -Path $destinationDirectory -ChildPath $file.Name
        Move-Item -Path $file.FullName -Destination $destinationPath -Force
        Write-Output "Moved $($file.Name) to $($destinationPath)"
    }
}




# ---------------------------------------------------------------------------------------------------------

# [string]$workingDir = "C:/PQSCADA/pqbi/aspnet-core/src/PQBI.Web.Host"
# [string]$entityFrameworkCoreCsproj = "C:/PQSCADA/pqbi/aspnet-core/src/PQBI.EntityFrameworkCore/PQBI.EntityFrameworkCore.csproj"
[string]$scriptDirectory = $PSScriptRoot

[string]$migrationTmpDirectory = Join-Path $scriptDirectory "Tmp"
[string]$hostDir = Split-Path -Path $entityFrameworkCoreCsproj -Parent
[string]$sqLiteMigration = Join-Path $hostDir "Migrations/SQLite"
[string]$postGresMigration = Join-Path $hostDir "Migrations/Postgres"
[string]$jsonFilePath = Join-Path $workingDir "appsettings.json"
[string]$dockerPostgresFile = Join-Path $workingDir "local-run-docker-compose.yml"

[string]$targetDbFiles = "PQBI_Db.*"

[string]$dockerContainerName = "pqbi.postgres.db"

# ------------------------------------------------------------------------------------------------------------

$jsonContent = Get-Content -Path $jsonFilePath -Raw | ConvertFrom-Json

$dbConnectionString = ""

$isMultiTenant = $false
if ($jsonContent.PqbiConfig){
    if ($jsonContent.PqbiConfig.MultiTenancyEnabled) {
        $isMultiTenant = $jsonContent.PqbiConfig.MultiTenancyEnabled
    }
}


# $isMultiTenant = $true
if ($isMultiTenant) {

    if ($jsonContent.ConnectionStrings){
        if ($jsonContent.ConnectionStrings.PostgresDb) {
            $dbConnectionString = $jsonContent.ConnectionStrings.PostgresDb

            if($injectConnectionString){
                $jsonContent.ConnectionStrings.Default = $dbConnectionString
                $jsonContent | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonFilePath
            }
        }
    }

    # Postgres Multi Tenant
    RelocateMigration $sqLiteMigration $migrationTmpDirectory


    docker-compose -f $dockerPostgresFile  down
    docker-compose -f $dockerPostgresFile  up -d

    dotnet ef database update --project $entityFrameworkCoreCsproj -- "connection:$dbConnectionString"

    RelocateMigration $migrationTmpDirectory $sqLiteMigration
}
else {
    # LocalDb Single Tenant

    $workingDirPath = -join ($workingDir, "\*")
    # Delete previous database
    Write-Host "Removing PQBI_Db"
    Get-ChildItem -Path $workingDirPath -Include $targetDbFiles
    Get-ChildItem -Path $workingDirPath -Include $targetDbFiles | Remove-Item
    Write-Host "Removed PQBI_Db"

    if ($jsonContent.ConnectionStrings){
        if ($jsonContent.ConnectionStrings.SQLiteDb) {
            $dbConnectionString = $jsonContent.ConnectionStrings.SQLiteDb

            if($injectConnectionString){
                $jsonContent.ConnectionStrings.Default = $dbConnectionString
                $jsonContent | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonFilePath
            }
        }
    }

    RelocateMigration $postGresMigration $migrationTmpDirectory
        
    dotnet ef database update --project $entityFrameworkCoreCsproj -- "connection:$dbConnectionString"

    RelocateMigration $migrationTmpDirectory $postGresMigration


    $dbCoreDirectoryPath = $entityFrameworkCoreCsproj | Split-Path
    $dbCoreDirectoryPath = -Join ($dbCoreDirectoryPath, "\*")
    Write-Host ( -join ("Db located = ", $dbCoreDirectoryPath))

    Write-Host "Coping"
    Get-Item -Path $dbCoreDirectoryPath -Include $targetDbFiles 

    Get-Item -Path $dbCoreDirectoryPath -Include $targetDbFiles | Move-Item -Destination $workingDir
}





Write-Host "End!!!!!"

