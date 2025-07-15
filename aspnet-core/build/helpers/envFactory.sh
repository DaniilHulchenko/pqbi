#!/bin/bash

PQS_ADMIN_NAME="admin"
PQS_ADMIN_PASSWORD="PQSpqs12345"

# Postgres
POSTGRES_USERNAME="postgres"
POSTGRES_PASSWORD="postgres"

# Nexus
NEXUS_USERNAME="pqbi"
NEXUS_PASSWD="PQBI123"

# Ports
PQBI_HOST_HTTPS_PORT=443
PQBI_HOST_HTTPS_PORT_HOST=44301
PQBI_FRONT_CONTAINER_PORT=443
PQBI_PQSSERVICE_MOCK_PORT=80

# Other
NOP_SESSION_INTERVAL_IN_SECONDS=5

# Names
TESTS_NAME="pqbi.e2e"
HOST_NAME="pqbi.host"
FRONT_NAME="pqbi.ng"
PQSSERVICE_MOCK_NAME="pqbi.mock"
POSTGRES_DB_NAME="pqbi.postgres"

# Image CID Formats
HOST_IMAGE_CID_FORMAT="cid%s.rev"
FRONT_IMAGE_CID_FORMAT="cid%s.rev"
PQSSERVICE_MOCK_IMAGE_CID_FORMAT="cid%s.rev"
HOST_POSTGRES_DB_IMAGE_CID_FORMAT="cid%s.rev"

# Cypress Version
CYPRESS_VERSION="12.5.0"

# Test Image CID
TESTS_IMAGE_CID="cypress-$CYPRESS_VERSION"

# Containers
HOST_CONTAINER="pqbi.host"
HOST_SEQ_CONTAINER="pqbi.seq"
FRONT_CONTAINER="pqbi.ng"
TESTS_CONTAINER="pqbi.cypress"
PQSSERVICE_MOCK_CONTAINER="pqbi.mock"
HOST_POSTGRES_DB_CONTAINER="pqbi.postgres.db"

# Nexus URLs
NEXUS_REPO_URL="nexus.elspec.local:8443"
NEXUS_DOCKER_HOSTED="nexus.elspec.local:8445"

NEXUS_YARN_PROXY_URL="https://$NEXUS_REPO_URL/repository/npm-yarn/"
NEXUS_NPM_PROXY_URL="https://$NEXUS_REPO_URL/repository/npm-proxy/"
NEXUS_CYPRESS_BIN="https://$NEXUS_REPO_URL/repository/cypress-install-bins/$CYPRESS_VERSION/cypress.zip"

# Network
NETWORK_NAME=""

# Image Templates
PQBI_TESTS_IMAGE_TEMPLATE="%s:%s"
PQBI_HOST_IMAGE_TEMPLATE="%s:%s%s.%s"
PQBI_FRONT_IMAGE_TEMPLATE="%s:%s%s.%s"
PQBI_PQSSERVICE_MOCK_IMAGE_TEMPLATE="%s:%s%s.%s"
PQBI_POSTGRES_DB_IMAGE_TEMPLATE="%s:%s%s.%s"

# Nexus Image Revision Templates
TESTS_NEXUS_IMAGE_REVISION_TEMPLATE="$NEXUS_DOCKER_HOSTED/{0}"
HOST_NEXUS_IMAGE_REVISION_TEMPLATE="$NEXUS_DOCKER_HOSTED/{0}"
FRONT_NEXUS_MAGE_REVISION_TEMPLATE="$NEXUS_DOCKER_HOSTED/{0}"
PQSSERVICE_NEXUS_IMAGE_REVISION_TEMPLATE="$NEXUS_DOCKER_HOSTED/{0}"
POSTGRES_NEXUS_IMAGE_REVISION_TEMPLATE="$NEXUS_DOCKER_HOSTED/{0}"


TESTS_NEXUS_IMAGE_REVISION=""
HOST_NEXUS_IMAGE_REVISION=""
FRONT_NEXUS_MAGE_REVISION=""
PQSSERVICE_NEXUS_IMAGE_REVISION=""
POSTGRES_NEXUS_IMAGE_REVISION=""

IsPublishDockerToNexus=""
HOST_DEPLOYMENT_TYPE=""
Revision=""

# Addresses
SERVER_ROOT_ADDRESS="https://$HOST_CONTAINER:$PQBI_HOST_HTTPS_PORT"
CLIENT_ROOT_ADDRESS="https://$FRONT_CONTAINER:$PQBI_FRONT_CONTAINER_PORT"
PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS="http://$PQSSERVICE_MOCK_CONTAINER:$PQBI_PQSSERVICE_MOCK_PORT"
CORS_ORIGIN_CONTAINER="https://*.pqbi.ng:$PQBI_FRONT_CONTAINER_PORT"

# URLs
PQS_SERVICE_REST_URL=""
NG_HTTP_APPBASE_URL=""
HOST_SERVER_ROOT_URL=""

#Docker
HOST_DOCKER_INTERNAL_IP=$(ip addr show | grep "\binet\b.*\bdocker0\b" | awk '{print $2}' | cut -d '/' -f 1)



function initialize()
{

    local revision=$1
    local cidNo=$2
    local tag=$(echo "$3" | tr '[:upper:]' '[:lower:]')
    local isMultiTenant=$4


    HOST_IMAGE_CID=$(printf "$HOST_IMAGE_CID_FORMAT" "$cidNo")
    FRONT_IMAGE_CID=$(printf "$FRONT_IMAGE_CID_FORMAT" "$cidNo")
    PQSSERVICE_MOCK_IMAGE_CID=$(printf "$PQSSERVICE_MOCK_IMAGE_CID_FORMAT" "$cidNo")
    HOST_POSTGRES_DB_IMAGE_CID=$(printf "$HOST_POSTGRES_DB_IMAGE_CID_FORMAT" "$cidNo")

    IsPublishDockerToNexus=true
    Revision="$revision"
    NETWORK_NAME="pqbi.network.cid_.rev$Revision"

    PQBI_TESTS_IMAGE=$(printf "$PQBI_TESTS_IMAGE_TEMPLATE" "$TESTS_NAME" "$TESTS_IMAGE_CID")
    PQBI_HOST_IMAGE=$(printf "$PQBI_HOST_IMAGE_TEMPLATE" "$HOST_NAME" "$HOST_IMAGE_CID" "$Revision" "$tag")
    # echo "$PQBI_HOST_IMAGE"
    PQBI_FRONT_IMAGE=$(printf "$PQBI_FRONT_IMAGE_TEMPLATE" "$FRONT_NAME" "$FRONT_IMAGE_CID" "$Revision" "$tag")
    PQBI_PQSSERVICE_MOCK_IMAGE=$(printf "$PQBI_PQSSERVICE_MOCK_IMAGE_TEMPLATE" "$PQSSERVICE_MOCK_NAME" "$PQSSERVICE_MOCK_IMAGE_CID" "$Revision" "$tag")
    PQBI_POSTGRES_DB_IMAGE=$(printf "$PQBI_POSTGRES_DB_IMAGE_TEMPLATE" "$POSTGRES_DB_NAME" "$HOST_POSTGRES_DB_IMAGE_CID" "$Revision" "$tag")

    TESTS_NEXUS_IMAGE_REVISION=$(printf "$TESTS_NEXUS_IMAGE_REVISION_TEMPLATE" "$PQBI_TESTS_IMAGE")
    HOST_NEXUS_IMAGE_REVISION=$(printf "$HOST_NEXUS_IMAGE_REVISION_TEMPLATE" "$PQBI_HOST_IMAGE")
    FRONT_NEXUS_MAGE_REVISION=$(printf "$FRONT_NEXUS_MAGE_REVISION_TEMPLATE" "$PQBI_FRONT_IMAGE")
    PQSSERVICE_NEXUS_IMAGE_REVISION=$(printf "$PQSSERVICE_NEXUS_IMAGE_REVISION_TEMPLATE" "$PQBI_PQSSERVICE_MOCK_IMAGE")
    POSTGRES_NEXUS_IMAGE_REVISION=$(printf "$POSTGRES_NEXUS_IMAGE_REVISION_TEMPLATE" "$PQBI_POSTGRES_DB_IMAGE")

}

function createTestingEnv()
{
    local revision=$1
    local cidNo=$2
    local isMultiTenant=$4

    initialize "$revision" "$cidNo" "testing" "$isMultiTenant"

    PQS_SERVICE_REST_URL="$PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS/session/sendRequest/"

    HOST_DEPLOYMENT_TYPE="Development"

    NG_HTTP_APPBASE_URL="$CLIENT_ROOT_ADDRESS"
    tmp="$SERVER_ROOT_ADDRESS"
    HOST_SERVER_ROOT_URL="$SERVER_ROOT_ADDRESS"

    echo "xxxx"
}


function createDeploymentEnv()
{
    local revision=$1
    local pqsServiceUrl=$2
    local cid=$3
    local env=$4
    local isMultiTenant=$5

    initialize "$revision" "$cid" "$env" "$isMultiTenant"

    # $this.TestsComp.IsActive = $false
    # $this.MockServiceComp.IsActive = $false
        
    PQS_SERVICE_REST_URL="$pqsServiceUrl/PQS5/rpqz/"

    HOST_DEPLOYMENT_TYPE="$env"

    # myIp=$(ip addr show | grep -oP '(?<=inet\s)\d+(\.\d+){3}' | head -1)
    myIp=$(hostname -I | awk '{print $1}')

    echo "local machine ip $myIp"


    NG_HTTP_APPBASE_URL="https://$myIp:$PQBI_FRONT_CONTAINER_PORT"
    HOST_SERVER_ROOT_URL="https://$myIp:$PQBI_HOST_HTTPS_PORT_HOST"
    echo "HOST_SERVER_ROOT_URL = $HOST_SERVER_ROOT_URL"
}


function createEnv() {
   local revision=$1
   local pqsServiceUrl=$2
   local env=$3
   local cid=$4
   local isMultiTenant=$5

   case $env in
        "testing")
            # Execute code for option 1
            createTestingEnv "$revision" "$cid" "$isMultiTenant"
            ;;
        *)
            # Default case if none of the options match
            createDeploymentEnv "$revision" "$pqsServiceUrl" "$cid" "$env" "$isMultiTenant"
            ;;
    esac
}