#!/bin/bash

source helpers/functions.sh

CreateDestinationFilePath() {
    locationIndex=$1
    prefixComponent=$2
    replace=$3
    fileFullName=$4

    target="${locationIndex[$prefixComponent]}"
    fileName="${fileFullName//$replace/}"

    if [[ -v locationIndex[$fileName] ]]; then
        target="${locationIndex[$fileName]}"
    fi

    destinationFilePath="$target/$fileName"

    echo "$destinationFilePath"
}

SmartCopy() {
    local SourceDirectory=$1
    local DestinationDirectory=$2
    local -n items=$3

    echo "Copy Operation from [$SourceDirectory] to  $DestinationDirectory started."

    SmartCopyExecuter "$SourceDirectory" "$DestinationDirectory" "items"

    echo "Copy Operation from [$SourceDirectory] to  $DestinationDirectory finished."
}

SmartCopyExecuter() {
    local SourceDirectory=$1
    local DestinationDirectory=$2
    local -n ignore=$3

    if [ ! -d "$SourceDirectory" ]; then
        echo "Source directory '$SourceDirectory' not found." >&2
    else
  
        if [ ! -d "$DestinationDirectory" ]; then
            mkdir -p "$DestinationDirectory"
        fi

        local tmp
        for tmp in "$SourceDirectory"/*; do

            if [ -z "$tmp" ] || [ ! -e "$tmp" ]; then
                continue
            fi
            
            itemName=$(basename "$tmp" | tr '[:upper:]' '[:lower:]')

            local global_matchFound
            IsNotMatchInArray "$itemName" "ignore"

            if [ "$global_matchFound" = true ]; then
                echo "$tmp was ignored"
                continue
            fi

            destinationPath="$DestinationDirectory/$(basename "$tmp")"

            if [ -d "$tmp" ]; then
                local arrayptr=ignore
                SmartCopyExecuter "$tmp" "$destinationPath" "arrayptr"
            else
                cp -f "$tmp" "$destinationPath"
            fi

        done
    fi

}

GetPathesFromTemplateFile() {
    local -n indexes=$1
    local prefixComponent=$2
    local replace=$3
    local fileFullName=$4


    for key in "${!indexes[@]}"; do
        echo "Key: $key ==> ${indexes[$key]}"
    done


    local pathes="${indexes[$prefixComponent]}"
    local fileName="${fileFullName//$replace/}"

    if [[ -v indexes[$fileName] ]]; then
        pathes="${indexes[$fileName]}"
    fi

    for path in $pathes; do
        local destinationFilePath="$path/$fileName"
        destinationFilePathes_list+=("$destinationFilePath")
    done

}



InjectVariablesByContent() {
    local content_ref=$1
    local key=$2
    local value=$3

    global_file_content="${content_ref//\{\{$key\}\}/$value}"
}


ReplaceVariableValues() {
    local filePath=$1
    local -n hashTable=$2

    echo ""
    echo ""
    echo ""
    echo "Value injection of $filePath started"

    local content=$(<"$filePath")

    echo "file => $content"

    local global_file_content
    for key in "${!hashTable[@]}"; do
        value="${hashTable[$key]}"
        echo "$key = $value"

        InjectVariablesByContent "$content" "$key" "$value"

        content="$global_file_content"
    done

    echo "$content" > "$filePath"

    echo "Value injection of $filePath finished"
    echo ""
}

LocateAndInject_xxx() {
    local targetJsonFilePath="$1"
    local -n tokens=$2
    local -n hashTable=$3

   for key in "${!tokens[@]}"; do
        local pathVariable="$key" 
        local hashValue="${tokens[$key]}"  

        local jsonValue="${hashTable[$hashValue]}"
        
        jsonValue=$(echo "$jsonValue" | sed 's/"/\\"/g')

        echo "jsonValue = $jsonValue"

        regexPattern="\"$(sed 's/\./\\./g; s/:/\\:/g' <<< "$pathVariable")\":\s*\"[^\"]*\""



        json=$(<"$targetJsonFilePath")
        if grep -qE "$regexPattern" <<< "$json"; then

            modifiedJson=$(sed -E 's/('"$regexPattern"')/\1 '"$jsonValue"'/' <<< "$json")

            echo "$modifiedJson" > "$targetJsonFilePath"
        else
            echo "Path '$pathVariable' not found in JSON file."
        fi
    done

}



LocateAndInject() {
    local targetJsonFilePath="$1"
    local -n tokens=$2
    local -n hashTable=$3

   for key in "${!tokens[@]}"; do
        local path="$key"  
        local hashValue="${tokens[$key]}"

        local jsonValue="${hashTable[$hashValue]}"
        
        jsonValue=$(echo "$jsonValue" | sed 's/"/\\"/g')

        echo "jsonValue = $jsonValue"

        if [[ $path == *.* ]]; then
            local last_substring="${path##*.}"
            echo "Last substring after the last \".\": $last_substring"
            
            local prior_substring="${path%.*}"
            local new_variable="[\"$last_substring\"]"
            local variable3=".$prior_substring$new_variable"

             jq --arg value "$jsonValue" "$variable3 = \$value" "$targetJsonFilePath" > "$targetJsonFilePath.tmp" && mv "$targetJsonFilePath.tmp" "$targetJsonFilePath"

        fi

        jq --arg value "$jsonValue" ". |= . + {\"$path\": \$value}" "$targetJsonFilePath" > "$targetJsonFilePath.tmp" && mv "$targetJsonFilePath.tmp" "$targetJsonFilePath"
    done

}


ExtractTokens() {
    local json_file="$1"

    local tmp=($(jq -r 'paths as $p | select(getpath($p) | type == "string" and test("{{[^{}]+}}")) | "\($p|join(".")) \(getpath($p) | sub("\\{\\{"; "") | sub("\\}\\}"; ""))"' "$json_file"))


    PrintHashTable "tmp"


    for key in "${!tmp[@]}"; do
        echo "tmp Key: $key ==> tmp value ${tmp[$key]}"
    done

    declare -A hashtable

    for ((i = 0; i < ${#tmp[@]}; i += 2)); do
        path=${tmp[$i]}
        value=${tmp[$((i + 1))]}
        tokeHashTable_Return["$path"]=$value
    done


     for key in "${!tokeHashTable_Return[@]}"; do
        echo "$key ==> ${tokeHashTable_Return[$key]}"
    done

}

GetJsonTokens() {
    local json_file="$1"

    local tmp=($(jq -r 'paths as $p | select(getpath($p) | test("{{[^{}]+}}")) | "\($p|join(".")) \(getpath($p) | sub("\\{\\{"; "") | sub("\\}\\}"; ""))"' "$json_file"))
    
    local tmp2=($(jq -r 'paths as $p | select(getpath($p) | type == "string" and test("{{[^{}]+}}")) | "\($p|join(".")) \(getpath($p) | sub("\\{\\{"; "") | sub("\\}\\}"; ""))"' "$json_file"))


    for key in "${!tmp[@]}"; do
        echo "tmp Key: $key ==> tmp value ${tmp[$key]}"
    done

    declare -A hashtable

    for ((i = 0; i < ${#tmp[@]}; i += 2)); do
        path=${tmp[$i]}
        value=${tmp[$((i + 1))]}
        tokeHashTable_Return["$path"]=$value
    done


     for key in "${!tokeHashTable_Return[@]}"; do
        echo "$key ==> ${tokeHashTable_Return[$key]}"
    done
}

ScenarioDataOrganizer() {
    local sourceDir=$1
    local -n locationIndex=$2
    local -n prefixFiles=$3
    local -n valueMapper=$4
    local env=$5
    
    echo "Scenario orchestrator started source [$sourceDir]"

    for prefixFile in "${prefixFiles[@]}"; do
        
        filter="${prefixFile}_"
        subFilter="${prefixFile}_override_"
        files=$(find "$sourceDir" -type f -name "$filter*")

        for file in $files; do
            echo "file ==> $file"
        done

        for file in $files; do

            local destinationFilePathes_list=()
            fileName=$(basename "$file")

            echo "xxxxxxx=> $fileName xxxxxxxxxxxxx"

            if [[ "$fileName" == *"$subFilter"* ]]; then

                echo "$fileName contains $subFilter"

                local isList=false
                local firstDestinationFilePath=""

                declare -A tokeHashTable_Return

                ExtractTokens "$file"

                echo "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
                PrintHashTable tokeHashTable_Return
                               
                GetPathesFromTemplateFile "locationIndex" "$prefixFile" "$subFilter" "$fileName"

                PrintHashTable "tokeHashTable_Return"

                for key in "${!destinationFilePathes_list[@]}"; do
                    local targetFile="${destinationFilePathes_list[$key]}" 
                    echo "Key: $key ==> ${destinationFilePathes_list[$key]}"

                    LocateAndInject "$targetFile" "tokeHashTable_Return" "valueMapper"
                done
               
            else

                echo "prefixFile ====> $prefixFile" 

                sourceFilePath="$file"
                echo "$fileName"


                if [[ "$fileName" =~ \.template\. ]]; then
                    fileName="${fileName//template/$env}"
                fi

                PrintHashTable "locationIndex"
                GetPathesFromTemplateFile "locationIndex" "$prefixFile" "$filter" "$fileName"

                for destinationFilePath in "${destinationFilePathes_list[@]}"; do
                    echo "$destinationFilePath"

                    tmpDir=$(dirname "$destinationFilePath")
                    if [[ ! -d "$tmpDir" ]]; then
                        mkdir -p "$tmpDir"
                    fi

                    echo "Copying $sourceFilePath to $destinationFilePath"
                    cp -f "$sourceFilePath" "$destinationFilePath"

                    if [ ${#valueMapper[@]} -ne 0 ]; then
                        ReplaceVariableValues "$destinationFilePath" "valueMapper"
                    fi
                done
            
        fi
        done
        
      
    done

}

InjectDataIntoJson() {
    local JsonFilePath="$1"
    declare -A ScenarioVariables=${2#*=}
    declare -A TokenAlias=${3#*=}

    local originalJson
    originalJson=$(<"$JsonFilePath")
    local updatedJson="$originalJson"

    for key in "${!TokenAlias[@]}"; do
        tokenValue="${TokenAlias[$key]}"
        IFS='.' read -r -a props <<< "$key"
        value="${ScenarioVariables[$tokenValue]}"
        targetObject="$updatedJson"

        for ((i = 0; i < ${#props[@]} - 1; i++)); do
            targetObject=$(jq -r ".\"${props[i]}\"" <<< "$targetObject")
        done

        propName="${props[-1]}"
        updatedJson=$(jq --arg propName "$propName" --arg value "$value" ".\"${props[-2]}\".\$propName = \$value" <<< "$updatedJson")
    done

    echo "$updatedJson" > "$JsonFilePath"
}


