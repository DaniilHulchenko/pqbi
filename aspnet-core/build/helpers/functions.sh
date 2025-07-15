#!/bin/bash

function PrintHashTable(){
    local -n ptr_hashtable=$1

    for key in "${!ptr_hashtable[@]}"; do
        echo "Key: $key ==> ${ptr_hashtable[$key]}"
    done
}

AutoVaribleInjector() {
    local content="$1"
    declare -n props=$2


    PrintHashTable "props"


    for key in "${!props[@]}"; do
        local value="${props[$key]}"
        if [[ $content == *{{$key}}* ]]; then
            content="${content//{{$key}}/$value}"
        fi
    done

    echo "$content"
}

PropFileDissect() {
    propFilePath="$1"
    declare -n scenarioProperties=$2

    PrintHashTable "scenarioProperties"
    

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ -n "$line" ]]; then

            IFS="::" read -r leftPart rightPart <<< "$line"

            rightPart="${rightPart#*:}"

            if [[ $rightPart == *'{{'* ]]; then
                
                local token="${rightPart#*{{}"
                token="${token%%\}\}*}"
                echo "Extracted token: $token"

                local toReplace="{{$token}}"

                local valueToInject="${scenarioProperties[$token]}"
                rightPart="${rightPart//$toReplace/$valueToInject}"

                echo "Extracted leftPart: $leftPart"
                echo "Extracted rightPart: $rightPart"
            fi

            PropFileDissect_return["$leftPart"]=$rightPart
        fi
    done < "$propFilePath"

    PrintHashTable PropFileDissect_return
}


function RemoveDockerAndImage() {
    dockerImage="$1"
    dockerContainer="$2"
    dockerNetwork="$3"

    echo "Remove docker with an image of $dockerContainer"
    
    if [ -n "$dockerContainer" ]; then
        echo "Stopping and removing docker $dockerContainer"
        docker stop "$dockerContainer"
        docker rm "$dockerContainer"
    fi
    
    if [ -n "$dockerImage" ]; then
        echo "Removing docker image $dockerImage"
        docker rmi "$dockerImage"
    fi
    
    if [ -n "$dockerNetwork" ]; then
        echo "Removing docker network $dockerNetwork"
        docker network rm "$dockerNetwork"
    fi
}

function PlainBuilder {

	targetDirectory=$1

	if [ -z "$targetDirectory" ]; then
		targetDirectory=""
	fi

	# Removing target directory if exists
	if [ -d "$targetDirectory" ]; then
		rm -rf "$targetDirectory"
	fi

	preBin="pre"

	echo "Building $targetDirectory structure"

	# Creating preBin directory
	mkdir -p "$targetDirectory/$preBin"
}


copyAssociativeArray() {
    local -n srcArray=$1
    local -n destArray=$2
    
    for key in "${!srcArray[@]}"; do
        destArray["$key"]="${srcArray[$key]}"
    done
}

IsNotMatchInArray() {
    local fileNameLower=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    local matchFound=false

    local arrayName="$2[@]"
    local tmp
    for tmp in "${!arrayName}"; do
        itemLower=$(echo "$tmp" | tr '[:upper:]' '[:lower:]')
        if [ "$fileNameLower" == "$itemLower" ]; then
            matchFound=true
            break  # Exit the loop if a match is found
        fi
    done

    global_matchFound="$matchFound"
}


IsNexusPackageExist() {

    local dockerImage=$1
    local nexusUrl=$2
    
    IFS=':' read -ra dockerImageParts <<< "$dockerImage"
    dockerImageName=${dockerImageParts[0]}
    dockerImageVersion=${dockerImageParts[1]}
    
    uri="https://$nexusUrl/repository/docker-hosted/v2/${dockerImageName}/manifests/${dockerImageVersion}"
    response=$(curl -s -o /dev/null -w "%{http_code}" "$uri")
    
    if [ "$response" -eq 200 ]; then
       global_IsNexusPackageExist_result=0
    else
       global_IsNexusPackageExist_result=1
    fi


}



