#!/bin/bash
showHelp() {
# `cat << EOF` This means that cat should stop reading when EOF is detected
cat << EOF  
Usage: ./deploy-transmitter.sh
  -n  <iot-hub-hostname>        IoT HUB hostname
  -d  <device-name>             iot edge device name
  -b  <size>                    (optional) this value will be assigned to edgeHub's MaxUpstreamBatchSize env var.
                                If provided, it will override the env variable $MaxUpstreamBatchSize
  -i  <image-uri:tag>           image URI with

with Log Analytics:  
  -m                            (optional) deploys the metrics-collector
  -w  <workspace-name>          (required if '-m' is used) log analytics workspace name
  -g  <workspace-rf>            (required if '-m' is used) log analytics workspace resource group
  -f  <interval>                (required if '-m' is used) metrics scraping interval (in seconds)
  
Examples:

deploy transmitter only:
    ./deploy-transmitter.sh -n myIotHub -d myEdgeDevice -i arlotito/iotedgeperf-transmitter:0.5.0 

deploy transmitter and metrics-collector:
    ./deploy-transmitter.sh -n myIotHub -d myEdgeDevice -i arlotito/iotedgeperf-transmitter:0.5.0 -m -w myLogWs -g myLogWsRg -f 2

deploy transmitter and set MaxUpstreamBatchSize=200:
    ./deploy-transmitter.sh -n myIotHub -d myEdgeDevice -i arlotito/iotedgeperf-transmitter:0.5.0 -b 200

Prerequisites:
    - az cli with iot extension (https://github.com/Azure/azure-iot-cli-extension)

Note:
This script uses the azure CLI with the IoT extension.
If not already signed-in, do 'az login' and select the tenant/subscription where you want to operate on.
EOF
}

# default values
MetricsCollector="false"

while getopts "hmn:d:b:i:m:e:w:g:f:" args; do
    case "${args}" in
        h ) showHelp;;
        n ) HUB_NAME="${OPTARG}";;
        d ) DEVICE_NAME="${OPTARG}";;
        b ) MaxUpstreamBatchSizeArg="${OPTARG}";;
        i ) TRANSMITTER_IMAGE_URI="${OPTARG}";;
        e ) EnvFile="${OPTARG}";;
        m ) MetricsCollector="true";;
        w ) LOG_ANALYTICS_WS_NAME="${OPTARG}";;
        g ) LOG_ANALYTICS_WS_RG="${OPTARG}";;
        f ) METRICS_COLLECTOR_FREQUENCY="${OPTARG}";;
        \? ) echo "Unknown option: -$OPTARG" >&2; echo; showHelp; exit 1;;
        :  ) echo "Missing option argument for -$OPTARG" >&2; echo; showHelp; exit 1;;
        *  ) echo "Unimplemented option: -$OPTARG" >&2; echo; showHelp; exit 1;;
    esac
done
shift $((OPTIND-1))

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

if [ ! "$HUB_NAME" ] || [ ! "$DEVICE_NAME" ] || [ ! "$TRANSMITTER_IMAGE_URI" ];
then
    echo -e "${RED}ERROR: required parameter is missing${NC}"
    echo "Please see help: $0 -h"
    exit 1
fi

if [ "$MetricsCollector" = "true" ];
then
    if [ ! "$LOG_ANALYTICS_WS_NAME" ] || [ ! "$LOG_ANALYTICS_WS_RG" ] || [ ! "$METRICS_COLLECTOR_FREQUENCY" ];
    then
        echo -e "${RED}ERROR: required parameter is missing${NC}"
        echo "Please see help: $0 -h"
        exit 1
    fi
fi

echo
echo "Settings:"
echo "transmitter image uri:        $TRANSMITTER_IMAGE_URI"
echo "IoT HUB name:                 $HUB_NAME"
echo "Device name:                  $DEVICE_NAME"
echo "deploy metrics-collector:     $MetricsCollector"

# selects deployment manifest with/without metrics collector depending on MetricsCollector flag
if [ "$MetricsCollector" = "true" ]
then
    echo "Log Analytics Workspace name: $LOG_ANALYTICS_WS_NAME"
    echo "Log Analytics Workspace rg:   $LOG_ANALYTICS_WS_RG"
    echo "Metrics scraping interval:    $METRICS_COLLECTOR_FREQUENCY"

    deploymentManifestTemplate="./manifests/deployment.metrics-collector.template.json"
    
    export LOG_ANALYTICS_IOT_HUB_RESOURCE_ID=$(az iot hub show -n $HUB_NAME --query id -o tsv)
    export LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show -n $LOG_ANALYTICS_WS_NAME -g $LOG_ANALYTICS_WS_RG --query customerId -o tsv)
    export LOG_ANALYTICS_SHARED_KEY=$(az monitor log-analytics workspace get-shared-keys -n $LOG_ANALYTICS_WS_NAME -g $LOG_ANALYTICS_WS_RG --query primarySharedKey -o tsv)
else
    deploymentManifestTemplate="./manifests/deployment.template.json"
fi
deploymentManifest="./manifests/deployment.json"

echo "Deployment manifest template: $deploymentManifestTemplate"

# do not touch
export upstream='$upstream'
export edgeAgent='$edgeAgent'
export edgeHub='$edgeHub'
export TRANSMITTER_IMAGE_URI=$TRANSMITTER_IMAGE_URI
export METRICS_COLLECTOR_FREQUENCY=$METRICS_COLLECTOR_FREQUENCY

# if provided, the command line parameter overrides the env variable
if [ "$MaxUpstreamBatchSizeArg" ]
then
    export MaxUpstreamBatchSize=$MaxUpstreamBatchSizeArg
fi

if [ ! "$MaxUpstreamBatchSize" ]; 
then
    echo -e "${RED}ERROR: MaxUpstreamBatchSize not found${NC}"
    echo "Please make sure to either export it:"
    echo '  export MaxUpstreamBatchSize=<value>'
    echo 'or to pass it over via the "-b" parameter:'
    echo '  -b <value>'
    exit 1
fi

# do the env vars expansion
cat $deploymentManifestTemplate | envsubst > $deploymentManifest

# do the deployment
echo "deploying manifest '$deploymentManifest'..."
result=$(az iot edge set-modules \
    -n $HUB_NAME \
    -d $DEVICE_NAME \
    --content $deploymentManifest)

# remove deployment manifest
rm $deploymentManifest

# restart edgeHub
echo "restarting edgeHub module..."
result=$(az iot hub invoke-module-method --method-name 'RestartModule' -n $HUB_NAME -d $DEVICE_NAME -m '$edgeAgent' --method-payload \
'
    {
        "schemaVersion": "1.0",
        "id": "edgeHub"
    }
')

# restart source
echo "restarting source module..."
result=$(az iot hub invoke-module-method --method-name 'RestartModule' -n $HUB_NAME -d $DEVICE_NAME -m '$edgeAgent' --method-payload \
'
    {
        "schemaVersion": "1.0",
        "id": "source"
    }
')

# wait
echo "waiting 20s for the module to start..."
sleep 20
