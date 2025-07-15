#!/bin/bash


source helpers/vars.sh
source helpers/envFactory.sh
source helpers/functions.sh
source helpers/scenarioRunner.sh


function DockerLaunchAndPush() {
    local fileComposePath="$1"
    local isDetachMode="$2"
    local dockerImage="$3"
    local nexusHostPush="$4"
    local nexusHostCheck="$5"
    local isPublish="$6"
    local isRebuildImage="$7"
        
    local dockerComposeArgs=(-f "$fileComposePath" up)
    
    if [ "$isDetachMode" = true ]; then
        dockerComposeArgs+=(-d)
    fi
    
    local global_IsNexusPackageExist_result=1 
    IsNexusPackageExist  "$dockerImage" "$nexusHostCheck"


    mkdir docker_logs
    chmod 777 docker_logs
    touch docker_logs/environments.txt

    if [ "$global_IsNexusPackageExist_result" -gt 0 ]; then
        echo "------------------ Building Image ------------------"

        docker-compose --file "$fileComposePath" build --progress plain
        if [ $? -ne 0 ]; then
            echo "docker-compose build failed"
            exit 1
        fi
    else 
        if [ "$isRebuildImage" = true ]; then
            echo "------------------ Re-building existing Image ------------------"
            dockerComposeArgs+=(--build)
        fi
    fi


    
    echo "------------------ Container run ------------------"
    CACHEBUST=$(date +"%Y%m%d%H%M%S") docker-compose "${dockerComposeArgs[@]}"
    
    if [ "$global_IsNexusPackageExist_result" -gt 0 ]; then
        if [ "$isPublish" = true ]; then
            echo "------------------ Publishing Image to Nexus ------------------"
            docker push "${nexusHostPush}/${dockerImage}"
        fi
    fi
}

BuildPQSServiceMock() {
    local -n hostVaribles=$1
    local scenarioConfigDirectoryPath=$2
    local -n loacationPathes=$3


    local image_id=$(docker inspect --format='{{.Image}}' "$PQSSERVICE_MOCK_CONTAINER")
    RemoveDockerAndImage "$image_id" $PQSSERVICE_MOCK_CONTAINER


    PlainBuilder $pqsServiceMockBin
    local prefixFilesArray=("pqsServiceMock")

    declare -A mockLocations
    mockLocations=(
        ["appsettings.Development.json"]="$outputPQSServiceMock"
        ["appsettings.Staging.json"]="$outputPQSServiceMock"
        ["appsettings.Production.json"]="$outputPQSServiceMock"
    )

    copyAssociativeArray "loacationPathes" "mockLocations"

    

    ScenarioDataOrganizer "$pqsServiceMockDockerConfigFilesFolder"  "mockLocations" "prefixFilesArray"  "hostVaribles" "$HOST_DEPLOYMENT_TYPE"

    cp -r "$globalConfigsFilesFolder"/* "$pqsMockServicePre"
    cp -r "$slnFolder/nuget.config" "$pqsMockServicePre"


    local pqsHostName="${hostVaribles["PQBI_PQSSERVICE_MOCK_IMAGE"]}"
    local global_IsNexusPackageExist_result=1 
    IsNexusPackageExist  "$pqsHostName"  "$NEXUS_REPO_URL"


    if [ "$global_IsNexusPackageExist_result" -eq 0 ]; then
        
        echo "Image exists"

    else

        echo "PQSService Image doesnt exists."
        cd "$slnFolder"

        excludeItems=("aspnetzeroradtool" "build" "docker" "scripts" "etc" ".vs" "bin" "obj" "appsettings.Staging.json" "appsettings.Production.json" "PQBI_Db.db")
        rsync -av --exclude="${excludeItems[@]/#/--exclude=}" "$slnFolder/" "$pqsMockServicePre/" >/dev/null 2>&1
    fi

    cd "$pqsMockServicePre"
	DockerLaunchAndPush "docker-compose-pqs-service-mock.yml"  true "$PQBI_PQSSERVICE_MOCK_IMAGE"   "${NEXUS_DOCKER_HOSTED}" "${NEXUS_REPO_URL}" "${IsPublishDockerToNexus}" false

}

BuildPqbiHost() {
   
    local -n hostVaribles=$1
    local scenarioConfigDirectoryPath=$2
    local -n loacationPathes=$3
    local isMultiTenant=$4


    local image_id=$(docker inspect --format='{{.Image}}' "$HOST_CONTAINER")
    RemoveDockerAndImage "$image_id" $HOST_CONTAINER

    image_id=$(docker inspect --format='{{.Image}}' "$HOST_SEQ_CONTAINER")
    RemoveDockerAndImage "$image_id" $HOST_SEQ_CONTAINER


    RemoveDockerAndImage  "postgres"  $HOST_POSTGRES_DB_CONTAINER ""

    docker-compose --file postgres-docker-compose.yml up -d

    PlainBuilder "$pqbi"

    declare -A locations
    locations=(
        ["appsettings.Development.json"]="$outputHost $dockerHostConfigurations"
        ["appsettings.Staging.json"]="$outputHost $dockerHostConfigurations"
        ["appsettings.Production.json"]="$outputHost"
    )

    local prefixFilesArray=("host")

    copyAssociativeArray "loacationPathes" "locations"

    ScenarioDataOrganizer "$pqbiHostDockerConfigFilesFolder" "locations" "prefixFilesArray" "hostVaribles" "$HOST_DEPLOYMENT_TYPE"
    ScenarioDataOrganizer "$scenarioConfigDirectoryPath" "locations" "prefixFilesArray" "hostVaribles" "$HOST_DEPLOYMENT_TYPE"

    cp -r "$globalConfigsFilesFolder"/* "$hostPre"
    cp -r "$slnFolder/nuget.config" "$hostPre"


    local hostName="${hostVariables["PQBI_HOST_IMAGE"]}"
    
    local global_IsNexusPackageExist_result=1 
    IsNexusPackageExist  "$hostName"  "$NEXUS_REPO_URL"

    if [ "$global_IsNexusPackageExist_result" -gt 0 ] || [ "$isMultiTenant" = true ]; then

        cd "$webHostFolder"
        cp -r "$pqbiHostDockerConfigFilesFolder"/* "$hostPre"

        cd "$slnFolder"

        excludeItems=("aspnetzeroradtool" "build" "docker" "scripts" "etc" ".vs" "bin" "obj" "appsettings.Staging.json" "appsettings.Production.json" "PQBI_Db.db")

        if [ "$isMultiTenant" = true ]; then
            excludeItems+=("SQLite")
        else
            excludeItems+=("Postgres")
        fi

        rsync -av --exclude="${excludeItems[@]/#/--exclude=}" "$slnFolder/" "$hostPre/" >/dev/null 2>&1
        
    else
        echo "Image exists"
    fi

    cd "$hostPre"

    if [ "$isMultiTenant" = true ]; then
        docker-compose -f postgres-docker-compose.yml up -d
    fi

    mkdir Logs
    chmod 777 Logs

    DockerLaunchAndPush  "$dockerComposePqbiFilePath" true "${PQBI_HOST_IMAGE}" "${NEXUS_DOCKER_HOSTED}" "${NEXUS_REPO_URL}" "${IsPublishDockerToNexus}" "${isMultiTenant}"
}

# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------

BuildNg(){
    local -n ngVars="$1"
    local -n loacationPathes=$2


     local image_id=$(docker inspect --format='{{.Image}}' "$PQBI_FRONT_CONTAINER")
    RemoveDockerAndImage "$image_id" $PQBI_FRONT_CONTAINER

    PlainBuilder "$ng"

    ls "$buildFolder"
    ls "$angularFolder"

    rsync -av --exclude={'node_modules','dist','.angular','.vscode','nswag','.vs'} "$angularFolder/" "$ngPre/" >/dev/null 2>&1

    cd "$ngPre"

    
    ngPreAssetsAppConfig="$ngPreAssets/appconfig.production.json"

    if [ ! -d "$dockerNgConfigurations" ]; then
        mkdir -p "$dockerNgConfigurations"
        echo "Created directory: $dockerNgConfigurations"
    fi

    cp "$ngPreAssetsAppConfig" "$dockerNgConfigurations"
    echo "Copied $ngPreAssetsAppConfig to $dockerNgConfigurations"

    
    declare -A ngLocations
    ngLocations["appconfig.production.json"]="$ngPreAssets $dockerNgConfigurations"


    copyAssociativeArray "loacationPathes" "ngLocations"


    PrintHashTable "ngLocations"

    local prefixFilesArray=("ng")



    ScenarioDataOrganizer "$ngDockerConfigFilesFolder" "ngLocations" "prefixFilesArray" "ngVars" "${ngVars[ASPNETCORE_ENVIRONMENT]}"
    cp -r "$globalConfigsFilesFolder"/* "$ngPre"

    set "$ngPre"
	DockerLaunchAndPush "docker-compose-ng.yml" true "$PQBI_FRONT_IMAGE" "${NEXUS_DOCKER_HOSTED}" "${NEXUS_REPO_URL}" "${IsPublishDockerToNexus}" false

}

# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------

BuildFullTests() {
    local -n testsVars=$1
    local specFolder=$2
    local -n loacationPathes=$3
    local scenarioConfigDirectoryPath=$4

    local image_id=$(docker inspect --format='{{.Image}}' "$TESTS_CONTAINER")
    RemoveDockerAndImage "$image_id" $TESTS_CONTAINER


    PrintHashTable "testsVars"

    PlainBuilder "$pqbiTestsFolder"

    cp -r "$pqbiE2e"/* "$testsBin" --force
    cp -r "${globalConfigsFilesFolder}"/* "${testsBin}" --force


    mkdir -p "${specsInputFolder}"
    cp -r "${specFilesFolder}"/* "${specsInputFolder}" --force


    local prefixFilesArray=("tests")

    cd "$testsBin"


    ScenarioDataOrganizer "$testsDockerConfigFolder" "loacationPathes" "prefixFilesArray" "testsVars" "${testsVars[ASPNETCORE_ENVIRONMENT]}"
    ScenarioDataOrganizer "$scenarioConfigDirectoryPath"  "loacationPathes"  "prefixFilesArray" "testsVars" "${testsVars[ASPNETCORE_ENVIRONMENT]}"

	DockerLaunchAndPush  $dockerComposeTestFilePath  false $PQBI_TESTS_IMAGE  "${NEXUS_DOCKER_HOSTED}" "${NEXUS_REPO_URL}" "${IsPublishDockerToNexus}" false

}

# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------
# -------------------------s---------------------------------------------------
# ----------------------------------------------------------------------------

App() {
    
    local configName=$1
    local testsGroup=$2
    local pqsServiceUrl=$3
    local deploymentType=$4


echo "Variable from script1: $buildFolder"
echo "Variable from script1: $appSettingsJonFilePath"

scenarioPath=""

isMultiTenant=true
isScenarioNameFound=false
isDeployment="false"

if [ "$deploymentType" != "testing" ]; then
    isDeployment="true"
else
    isDeployment="false"
fi

testBuildConfigDir="/home/pqbi/WORK/PQBI/aspnet-core/build/pqbi-tests/build_configs"

local isMultiTenant=true
for item in "$testBuildConfigDir"/*; do
    dirName=$(basename "$item")
    printf "Directory name: %s\n" "$dirName"
    
    if [ "$configName" = "$dirName" ]; then
        printf "Match found: %s\n" "$configName"
        isScenarioNameFound=true
        scenarioPath="$item"

        if [[ $configName == *single* ]]; then
            isMultiTenant=false
        fi

        break
    fi
done

if [ "$isScenarioNameFound" = false ]; then
    echo "Such scenario was not found."
    exit 1
fi


PropsFilePath=""
gitRevision=$(git rev-parse --short HEAD)    



createEnv "$gitRevision" "$pqsServiceUrl" "$deploymentType" "${configName:0:1}" "$isMultiTenant"

echo "-----------------------------Login private Docker Hub-----------------------------"

echo "https://${NEXUS_DOCKER_HOSTED}"
echo "${NEXUS_USERNAME}"
echo "${NEXUS_PASSWD}"

docker login "https://${NEXUS_DOCKER_HOSTED}" -u "${NEXUS_USERNAME}" -p "${NEXUS_PASSWD}"

declare -A commonVariables

commonVariables["PQS_ADMIN_NAME"]=$PQS_ADMIN_NAME
commonVariables["PQS_ADMIN_PASSWORD"]=$PQS_ADMIN_PASSWORD
commonVariables["POSTGRES_USERNAME"]=$POSTGRES_USERNAME
commonVariables["POSTGRES_PASSWORD"]=$POSTGRES_PASSWORD
commonVariables["PQS_SERVICE_REST_URL"]=$PQS_SERVICE_REST_URL
commonVariables["NOP_SESSION_INTERVAL_IN_SECONDS"]=$NOP_SESSION_INTERVAL_IN_SECONDS
commonVariables["PQBI_FRONT_CONTAINER_PORT"]=$PQBI_FRONT_CONTAINER_PORT
commonVariables["NG_HTTP_APPBASE_URL"]=$NG_HTTP_APPBASE_URL
commonVariables["HOST_SERVER_ROOT_URL"]=$HOST_SERVER_ROOT_URL
commonVariables["SERVER_ROOT_ADDRESS"]=$SERVER_ROOT_ADDRESS
commonVariables["CLIENT_ROOT_ADDRESS"]=$CLIENT_ROOT_ADDRESS
commonVariables["PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS"]=$PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS
commonVariables["CORS_ORIGIN_CONTAINER"]=$CORS_ORIGIN_CONTAINER
commonVariables["ASPNETCORE_ENVIRONMENT"]=$HOST_DEPLOYMENT_TYPE
commonVariables["HOST_DOCKER_INTERNAL_IP"]=$HOST_DOCKER_INTERNAL_IP


declare -A hostVariables

hostVariables["PQBI_HOST_CONTAINER"]=$HOST_CONTAINER
hostVariables["HOST_POSTGRES_DB_CONTAINER"]=$HOST_POSTGRES_DB_CONTAINER
hostVariables["PQBI_HOST_IMAGE"]=$PQBI_HOST_IMAGE
hostVariables["PQBI_HOST_SEQ_CONTAINER"]=$HOST_SEQ_CONTAINER
hostVariables["NETWORK_NAME"]=$NETWORK_NAME
hostVariables["BUILDER_REFERER"]=$BUILDER_REFERER
hostVariables["NEXUS_DOCKER_HOSTED"]=$NEXUS_DOCKER_HOSTED
hostVariables["ASPNETCORE_ENVIRONMENT"]=$HOST_DEPLOYMENT_TYPE

copyAssociativeArray "commonVariables" "hostVariables"

echo "xxxx=> ${hostVariables["PQBI_HOST_IMAGE"]}"

BuildPqbiHost "hostVariables" "$scenarioPath" "locationIndexerPaths" "$isMultiTenant"


# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------


if [ "$isDeployment" == "false" ]; then

    declare -A pqsServiceVariables
    pqsServiceVariables["PQBI_PQSSERVICE_MOCK_IMAGE"]=$PQBI_PQSSERVICE_MOCK_IMAGE
    pqsServiceVariables["PQBI_PQSSERVICE_MOCK_CONTAINER"]=$PQSSERVICE_MOCK_CONTAINER
    pqsServiceVariables["NETWORK_NAME"]=$NETWORK_NAME
    pqsServiceVariables["NEXUS_DOCKER_HOSTED"]=$NEXUS_DOCKER_HOSTED

    copyAssociativeArray "commonVariables" "pqsServiceVariables"

    BuildPQSServiceMock "pqsServiceVariables" "$scenarioPath" "locationIndexerPaths"

fi

# ----------------------------------------------------------------------------
# ----------------------------------------------------------------------------


declare -A ngVariables
ngVariables["PQBI_FRONT_IMAGE"]=$PQBI_FRONT_IMAGE
ngVariables["PQBI_FRONT_CONTAINER"]=$FRONT_CONTAINER
ngVariables["NETWORK_NAME"]=$NETWORK_NAME
ngVariables["NEXUS_DOCKER_HOSTED"]=$NEXUS_DOCKER_HOSTED
ngVariables["NEXUS_YARN_PROXY_URL"]=$NEXUS_YARN_PROXY_URL

copyAssociativeArray "commonVariables" "ngVariables" 

BuildNg "ngVariables" "locationIndexerPaths"



    # ----------------------------------------------------------------------------
    # ----------------------------------------------------------------------------
if [ "$isDeployment" == "false" ]; then


    propFile="$scenarioPath/props.txt"

    declare -A PropFileDissect_return=()
    PropFileDissect "$propFile" "commonVariables"



    declare -A testsVariables
    copyAssociativeArray "PropFileDissect_return" "testsVariables" 

    testsVariables["PQBI_TESTS_IMAGE"]=$PQBI_TESTS_IMAGE
    testsVariables["PQBI_TESTS_CONTAINER"]=$TESTS_CONTAINER
    testsVariables["NETWORK_NAME"]=$NETWORK_NAME
    testsVariables["NEXUS_DOCKER_HOSTED"]=$NEXUS_DOCKER_HOSTED
    testsVariables["NEXUS_NPM_PROXY_URL"]=$NEXUS_NPM_PROXY_URL
    testsVariables["NEXUS_CYPRESS_BIN"]=$NEXUS_CYPRESS_BIN
    testsVariables["CLIENT_ROOT_ADDRESS"]=$CLIENT_ROOT_ADDRESS
    testsVariables["CYPRESS_VERSION"]=$CYPRESS_VERSION

    copyAssociativeArray "commonVariables" "testsVariables" 


    specFilesFolder="${pqbiE2eSpecsFolder}/"
    specFilesFolder="${specFilesFolder}${configName}"
    specFilesFolder="${specFilesFolder}/"
    specFilesFolder="${specFilesFolder}/${testsGroup}"

    BuildFullTests  "testsVariables"  "$specFilesFolder" "locationIndexerPaths" "$scenarioPath"
fi


}