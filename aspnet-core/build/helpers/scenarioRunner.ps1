function InitiateMainScenarioProperties {
    param 
    (
        [DefaultEnvBase]$dockerImageWorkItem,
        [string]$pqsServiceUrl = ""
    ) 
       
    $properties = @{
        'PQS_ADMIN_NAME'                    = 'admin'
        'PQS_ADMIN_PASSWORD'                = 'PQSpqs12345'

        'POSTGRES_USERNAME'                 = $dockerImageWorkItem.POSTGRES_USERNAME
        'POSTGRES_PASSWORD'                 = $dockerImageWorkItem.POSTGRES_PASSWORD


        'PQS_SERVICE_REST_URL'              = $dockerImageWorkItem.PQS_SERVICE_REST_URL
        'NOP_SESSION_INTERVAL_IN_SECONDS'   = $dockerImageWorkItem.NOP_SESSION_INTERVAL_IN_SECONDS
        'PQBI_FRONT_CONTAINER_PORT'         = $dockerImageWorkItem.PQBI_FRONT_CONTAINER_PORT

        'NG_HTTP_APPBASE_URL'               = $dockerImageWorkItem.NG_HTTP_APPBASE_URL 
        'HOST_SERVER_ROOT_URL'              = $dockerImageWorkItem.HOST_SERVER_ROOT_URL 
        'SERVER_ROOT_ADDRESS'               = $dockerImageWorkItem.SERVER_ROOT_ADDRESS 
        'CLIENT_ROOT_ADDRESS'               = $dockerImageWorkItem.CLIENT_ROOT_ADDRESS
        'PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS' = $dockerImageWorkItem.PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS
        'CORS_ORIGIN_CONTAINER'             = $dockerImageWorkItem.CORS_ORIGIN_CONTAINER

    }

    return $properties
}



function CreateDestinationFilePath ( [hashtable]$locationIndex, [string]$prefixComponent, [string]$replace, [string]$fileFullName    ) {
    

    $target = $locationIndex[$prefixComponent]

    $fileName = $fileFullName.Replace($replace, "")

    if ($locationIndex.ContainsKey($fileName)) {
        $target = $locationIndex[$fileName]
    }

    $destinationFilePath = Join-Path $target $fileName

    return $destinationFilePath
}

function GetPathesFromTemplateFile ( [hashtable]$locationIndex, [string]$prefixComponent, [string]$replace, [string]$fileFullName    ) {
    

    $pathes = $locationIndex[$prefixComponent]

    $fileName = $fileFullName.Replace($replace, "")

    if ($locationIndex.ContainsKey($fileName)) {
        $pathes = $locationIndex[$fileName]
    }

    $list = New-Object System.Collections.Generic.List[string]

    foreach ($path in $pathes) {
        $destinationFilePath = Join-Path $path $fileName
        $list.Add($destinationFilePath)
    }

    return $list
}


function ScenarioDataOrganizer {
    param (
        [string]$sourceDir,
        [string[]]$prefixFiles,
        [hashtable]$locationIndex,
        [hashtable]$valueMapper,
        [string]$env = "Development"

    )

    Write-Host "Scenario orchestrator started"

    foreach ($prefixFile in $prefixFiles) {
        $filter = $prefixFile + "_"
        $subFilter = $prefixFile + "_override_"
        
        Get-ChildItem -Path $sourceDir -Filter ($filter + '*') | foreach {
            
            # $targetDir = $locationIndex[$prefixFile]
            $sourceFilePath = $_.FullName
            $fileName = $_.Name

            if ($fileName -match $subFilter ) {

                # _override_ case
                $isList = $false
                $firstDestinationFilePath = ""

                $list = GetPathesFromTemplateFile $locationIndex $prefixFile $subFilter $fileName

                if ($list -is [string]) {
                    $firstDestinationFilePath = $list
                } else {
                    $firstDestinationFilePath = $list[0]  # Accessing the first element of the array
                    $isList = $true;
                }

                
                $tmpDir = Split-Path $firstDestinationFilePath
                if (-not (Test-Path $tmpDir)) {
                    New-Item -Path $tmpDir -ItemType Directory -Force
                }
            
                            
                $tokensDictionary = Get-JsonTokens -JsonFilePath   $_.FullName
                Inject-DataIntoJson $firstDestinationFilePath $valueMapper $tokensDictionary 

                if ($isList) {
                    # Iterate from the second item
                    for ($i = 1; $i -lt $list.Length; $i++) {
                        $destinationFilePath = $list[$i]

                        $tmpDir = Split-Path $destinationFilePath
                        if (-not (Test-Path $tmpDir)) {
                            New-Item -Path $tmpDir -ItemType Directory -Force
                        }

                        Copy-Item -Path  $firstDestinationFilePath -Destination $destinationFilePath -Force
                    }
                }
            }
            else {

                $destinationFilePath = ""

                if ($fileName -match "\.template\.") {
                    $fileName = $fileName.Replace("template", $env )
                }

                $destinationFilePathes = GetPathesFromTemplateFile $locationIndex $prefixFile $filter $fileName
                
                foreach ($destinationFilePath in $destinationFilePathes) {
                  
                    $tmpDir = Split-Path $destinationFilePath
                    if (-not (Test-Path $tmpDir)) {
                        New-Item -Path $tmpDir -ItemType Directory -Force
                    }
    
                  
                    Write-Output "Copying $sourceFilePath to $destinationFilePath"
                    Copy-Item -Path $sourceFilePath -Destination $destinationFilePath -Force

                    if ($valueMapper) {
                        Replace-VariableValues $destinationFilePath $valueMapper
                    }
                }
            }
        }
    }
}


function Replace-VariableValues {
    param (
        [string]$filePath,
        [hashtable]$hashTable
    ) 
	
    Write-Host ""
    Write-Host ""
    Write-Host ""
    Write-Host "Value injection of $filePath started"

    $content = Get-Content $filePath

    foreach ($key in $hashTable.Keys) {
        $value = $hashTable.$key
        Write-Output "$key = $value"

        InjectVariablesByContent -content ([ref]$content) -key $key -value $value

    }

    Set-Content $filePath $content
    
    Write-Host "Value injection of $filePath finished"
    Write-Host ""
}

function InjectVariablesByContent {
    param (
        [ref]$content,
        [string]$key,
        [string]$value
    ) 
	
    $content.Value = $content.Value -replace "\{\{$key\}\}", $value
}

function AutoVaribleInjector {
    param (
        [string]$content,
        [hashtable]$props
    ) 

    foreach ($key in $props.Keys) {
        $value = $props[$key]
        
        if ($content.Contains("{{$key}}")) {
            $content = $content -replace "\{\{$key\}\}", $value
            # return $content
        }
    }

    return $content
}

function Get-JsonTokens {
    param (
        [Parameter(Mandatory = $true)]
        # [ValidateScript({Test-Path $_ -PathType 'Leaf' -and $_ -like '*.json'})]
        [string]$JsonFilePath
    )

    $mainTokens = @()

    $json = Get-Content -Raw -Path $JsonFilePath | ConvertFrom-Json

    # Helper function to recursively search for tokens
    function Get-Tokens($tokens, $object, $jsonElementPath) {
        $currentToken = $tokens
        
        foreach ($property in $object.PSObject.Properties) {

            if ($property.Value -is [string]) {
                $matches = [regex]::Matches($property.Value, '\{\{([^}]*)\}\}')
                foreach ($match in $matches) {
                    $propPath = $jsonElementPath + $property.Name
                    $currentToken += [PSCustomObject]@{
                        Path  = $propPath
                        Token = $match.Groups[1].Value
                    }
                }
            }
            elseif ($property.Value -is [object]) {

                $tokenPath = ""
                if ($tokens.Count -eq 0) {
                    $tokenPath = $property.Name
                }
                else {
                    $tokenPath += ".${$(property.Name)}"
                }

                Get-Tokens $currentToken $property.Value ($jsonElementPath + $tokenPath + '.')
            }
        }

        return $currentToken
    }

    $res = Get-Tokens $mainTokens  $json ''
    $tokenDictionary = [System.Collections.Generic.Dictionary[String, Object]]::new()

    foreach ($item in $res) {
        $key = $item.Path;
        if (-not $tokenDictionary.ContainsKey($key)) {
            $tokenDictionary.Add($key, $item.Token)
        }
    }

    return $tokenDictionary
}



function Inject-DataIntoJson {
    param (
        [Parameter(Mandatory = $true)]
        [string]$JsonFilePath,

        [Parameter(Mandatory = $true)]
        [hashtable]$ScenarioVariables,

        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.Dictionary[string, Object]]$TokenAlias
    )
    
    $originalJson = Get-Content -Raw -Path $JsonFilePath | ConvertFrom-Json

    foreach ($key in $TokenAlias.Keys) {

        $tokenValue = $TokenAlias[$key]
       
        $props = $key -split '\.'
        $value = $ScenarioVariables[$tokenValue]
        $targetObject = $originalJson

        for ($i = 0; $i -lt ($props.Length - 1); $i++) {
            $targetObject = $targetObject | Select-Object -ExpandProperty $props[$i]
        }

        $propName = $props[$i]
        $targetObject.$propName = $value
        $updatedJson = $originalJson | ConvertTo-Json -Depth 10
        $updatedJson | Set-Content -Path $JsonFilePath
    }
}


function PropFileDissect {
    param (
        [string]$propFilePath,
        [hashtable]$scenarioProperties
    )

    $testvaribles = @{}

    foreach ($line in Get-Content -Path $propFilePath) {
	
        if ($line) {
	
            $splitResults = $line -split "::"
				
            $key = $splitResults[0]
            $teplateCmd = $splitResults[1]
	
            $cmd = AutoVaribleInjector -content $teplateCmd -props $scenarioProperties
            $testvaribles[$key] = $cmd
            Write-Host "$key == $cmd"
        }
    }

    return $testvaribles
}
