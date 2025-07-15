
class ComponentsInfo {
    [bool]$IsActive = $true
}


class  HostComponentsInfo: ComponentsInfo {
    [bool]$IsMultiTenant = $false
}

class DefaultEnvBase {

    [HostComponentsInfo]$HostComp = [HostComponentsInfo]::new()
    [ComponentsInfo]$MockServiceComp = [ComponentsInfo]::new()
    [ComponentsInfo]$NgComp = [ComponentsInfo]::new()
    [ComponentsInfo]$TestsComp = [ComponentsInfo]::new()


    [string]$POSTGRES_USERNAME = "postgres"
    [string]$POSTGRES_PASSWORD = "postgres"

    [string]$NEXUS_USERNAME = "pqbi"
    [string]$NEXUS_PASSWD = "PQBI123"

    [int]$PQBI_HOST_HTTPS_PORT = 443
    [int]$PQBI_HOST_HTTPS_PORT_HOST = 44301
    [int]$PQBI_FRONT_CONTAINER_PORT = 443
    [int]$PQBI_PQSSERVICE_MOCK_PORT = 80

    [int]$NOP_SESSION_INTERVAL_IN_SECONDS = 5


    [string]$TESTS_NAME = "pqbi.e2e"
    [string]$HOST_NAME = "pqbi.host"
    [string]$FRONT_NAME = "pqbi.ng"
    [string]$PQSSERVICE_MOCK_NAME = "pqbi.mock"
    [string]$POSTGRES_DB_NAME = "pqbi.postgres"

    [string]$HOST_IMAGE_CID_FORMAT = "cid{0}.rev"
    [string]$FRONT_IMAGE_CID_FORMAT = "cid{0}.rev"		
    [string]$PQSSERVICE_MOCK_IMAGE_CID_FORMAT = "cid{0}.rev"
    [string]$HOST_POSTGRES_DB_IMAGE_CID_FORMAT = "cid{0}.rev"

    [string]$CYPRESS_VERSION = "12.5.0"
    
    [string]$TESTS_IMAGE_CID = "cypress-$($this.CYPRESS_VERSION)"


    [string]$HOST_CONTAINER = "pqbi.host"
    [string]$HOST_SEQ_CONTAINER = "pqbi.seq"
    [string]$FRONT_CONTAINER = "pqbi.ng"
    [string]$TESTS_CONTAINER = "pqbi.cypress"
    [string]$PQSSERVICE_MOCK_CONTAINER = "pqbi.mock"
    [string]$HOST_POSTGRES_DB_CONTAINER = "pqbi.postgres.db"
	

    [string]$NEXUS_REPO_URL = "nexus.elspec.local:8443"
    [string]$NEXUS_DOCKER_HOSTED = "nexus.elspec.local:8445"

    [string]$NEXUS_YARN_PROXY_URL = "https://$($this.NEXUS_REPO_URL)/repository/npm-yarn/"
    [string]$NEXUS_NPM_PROXY_URL = "https://$($this.NEXUS_REPO_URL)/repository/npm-proxy/"
    [string]$NEXUS_CYPRESS_BIN = "https://$($this.NEXUS_REPO_URL)/repository/cypress-install-bins/$($this.CYPRESS_VERSION)/cypress.zip"

    [string]$NETWORK_NAME

    [string]$PQBI_HOST_IMAGE_TEMPLATE = "{0}:{1}{2}.{3}"
    [string]$PQBI_FRONT_IMAGE_TEMPLATE = "{0}:{1}{2}.{3}"
    [string]$PQBI_PQSSERVICE_MOCK_IMAGE_TEMPLATE = "{0}:{1}{2}.{3}"
    [string]$PQBI_POSTGRES_DB_IMAGE_TEMPLATE = "{0}:{1}{2}.{3}"
    [string]$PQBI_TESTS_IMAGE_TEMPLATE = "{0}:{1}"
 
    [string]$PQBI_TESTS_IMAGE
    [string]$PQBI_HOST_IMAGE
    [string]$PQBI_FRONT_IMAGE
    [string]$PQBI_PQSSERVICE_MOCK_IMAGE
    [string]$PQBI_POSTGRES_DB_IMAGE


    [string]$TESTS_NEXUS_IMAGE_REVISION_TEMPLATE = "$($this.NEXUS_DOCKER_HOSTED)/{0}"  
    [string]$HOST_NEXUS_IMAGE_REVISION_TEMPLATE = "$($this.NEXUS_DOCKER_HOSTED)/{0}"
    [string]$FRONT_NEXUS_MAGE_REVISION_TEMPLATE = "$($this.NEXUS_DOCKER_HOSTED)/{0}"
    [string]$PQSSERVICE_NEXUS_IMAGE_REVISION_TEMPLATE = "$($this.NEXUS_DOCKER_HOSTED)/{0}"
    [string]$POSTGRES_NEXUS_IMAGE_REVISION_TEMPLATE = "$($this.NEXUS_DOCKER_HOSTED)/{0}"


    [string]$TESTS_NEXUS_IMAGE_REVISION 
    [string]$HOST_NEXUS_IMAGE_REVISION 
    [string]$FRONT_NEXUS_MAGE_REVISION 
    [string]$PQSSERVICE_NEXUS_IMAGE_REVISION 
    [string]$POSTGRES_NEXUS_IMAGE_REVISION 


    [bool]$IsPublishDockerToNexus;
    [string]$HOST_DEPLOYMENT_TYPE
    [string]$Revision

    [string]$SERVER_ROOT_ADDRESS = "https://$($this.HOST_CONTAINER):$($this.PQBI_HOST_HTTPS_PORT)"
    [string]$CLIENT_ROOT_ADDRESS = "https://$($this.FRONT_CONTAINER):$($this.PQBI_FRONT_CONTAINER_PORT)"
    [string]$PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS = "http://$($this.PQSSERVICE_MOCK_CONTAINER):$($this.PQBI_PQSSERVICE_MOCK_PORT)"
    [string]$CORS_ORIGIN_CONTAINER = "https://pqbi.ng:$($this.PQBI_FRONT_CONTAINER_PORT)"


    [string]$PQS_SERVICE_REST_URL 
    [string]$NG_HTTP_APPBASE_URL 
    [string]$HOST_SERVER_ROOT_URL

    [string]$HOST_DOCKER_INTERNAL_IP = "host.docker.internal"


    DefaultEnvBase($revision, $cidNo, $tag,$isMultiTenant) {


        $this.HostComp = [HostComponentsInfo]::new()
        $this.MockServiceComp = [ComponentsInfo]::new()
        $this.NgComp = [ComponentsInfo]::new()
        $this.TestsComp = [ComponentsInfo]::new()

        $this.TestsComp.IsActive = $false
        $this.HostComp.IsMultiTenant = $isMultiTenant

        $HOST_IMAGE_CID = $this.HOST_IMAGE_CID_FORMAT -f $cidNo
        $FRONT_IMAGE_CID = $this.FRONT_IMAGE_CID_FORMAT -f $cidNo
        $PQSSERVICE_MOCK_IMAGE_CID = $this.PQSSERVICE_MOCK_IMAGE_CID_FORMAT -f $cidNo
        $HOST_POSTGRES_DB_IMAGE_CID = $this.HOST_POSTGRES_DB_IMAGE_CID_FORMAT -f $cidNo
        

        $this.IsPublishDockerToNexus = $true
        $this.Revision = $revision

        $this.NETWORK_NAME = "pqbi.network.cid_.rev$($this.Revision)"

        $this.PQBI_TESTS_IMAGE = $this.PQBI_TESTS_IMAGE_TEMPLATE -f $this.TESTS_NAME, $this.TESTS_IMAGE_CID
        $this.PQBI_HOST_IMAGE = $this.PQBI_HOST_IMAGE_TEMPLATE -f $this.HOST_NAME, $HOST_IMAGE_CID, $this.Revision, $tag
        $this.PQBI_FRONT_IMAGE = $this.PQBI_FRONT_IMAGE_TEMPLATE -f $this.FRONT_NAME, $FRONT_IMAGE_CID, $this.Revision, $tag
        $this.PQBI_PQSSERVICE_MOCK_IMAGE = $this.PQBI_PQSSERVICE_MOCK_IMAGE_TEMPLATE -f $this.PQSSERVICE_MOCK_NAME, $PQSSERVICE_MOCK_IMAGE_CID, $this.Revision, $tag
        $this.PQBI_POSTGRES_DB_IMAGE = $this.PQBI_POSTGRES_DB_IMAGE_TEMPLATE -f $this.POSTGRES_DB_NAME, $HOST_POSTGRES_DB_IMAGE_CID, $this.Revision, $tag

        $this.TESTS_NEXUS_IMAGE_REVISION = $this.TESTS_NEXUS_IMAGE_REVISION_TEMPLATE -f $this.PQBI_TESTS_IMAGE
        $this.HOST_NEXUS_IMAGE_REVISION = $this.HOST_NEXUS_IMAGE_REVISION_TEMPLATE -f $this.PQBI_HOST_IMAGE
        $this.FRONT_NEXUS_MAGE_REVISION = $this.FRONT_NEXUS_MAGE_REVISION_TEMPLATE -f $this.PQBI_FRONT_IMAGE
        $this.PQSSERVICE_NEXUS_IMAGE_REVISION = $this.PQSSERVICE_NEXUS_IMAGE_REVISION_TEMPLATE -f $this.PQBI_PQSSERVICE_MOCK_IMAGE
        $this.POSTGRES_NEXUS_IMAGE_REVISION = $this.POSTGRES_NEXUS_IMAGE_REVISION_TEMPLATE -f $this.PQBI_POSTGRES_DB_IMAGE
    }
}

class  TestingEnv: DefaultEnvBase {
    TestingEnv($revision, $cidNo,$isMultiTenant) : base($revision, $cidNo, "testing",$isMultiTenant) {

        $this.TestsComp.IsActive = $true
        $this.MockServiceComp.IsActive = $true
        $this.TestsComp.IsActive = $true


        $this.PQS_SERVICE_REST_URL = "$($this.PQBI_PQSSERVICE_MOCK_ROOT_ADDRESS)/session/sendRequest/"
        
        $this.HOST_DEPLOYMENT_TYPE = "Development"

        $this.NG_HTTP_APPBASE_URL = $this.CLIENT_ROOT_ADDRESS
        $this.HOST_SERVER_ROOT_URL = $this.SERVER_ROOT_ADDRESS
        
    }
}


class DeploymentEnv : DefaultEnvBase {
    DeploymentEnv($revision, $pqsServiceUrl, $cidNo, $env,$isMultiTenant) : base($revision, $cidNo, $env,$isMultiTenant) {
        
        $this.TestsComp.IsActive = $false
        $this.MockServiceComp.IsActive = $false
        
        $this.PQS_SERVICE_REST_URL = "$pqsServiceUrl/PQS5/rpqz/"
        
        $this.HOST_DEPLOYMENT_TYPE = $env


        $myIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias 'Ethernet*').IPAddress
        Write-Host "local machine ip $myIp"

        $this.NG_HTTP_APPBASE_URL = "https://$($myIp):$($this.PQBI_FRONT_CONTAINER_PORT)"
        $this.HOST_SERVER_ROOT_URL = "https://$($myIp):$($this.PQBI_HOST_HTTPS_PORT_HOST)"
    }
}