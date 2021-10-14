#!/bin/bash
# example: ./deploy-transmitter.sh arturol76-s1-benchmark standard-ds3-v2-edge-1-2-1631626847 200 arlotito/iotedgeperf-transmitter:0.4.4

HUB_NAME=$1
DEVICE_NAME=$2
deploymentManifestTemplate="./manifests/deployment.template.json"
deploymentManifest="./manifests/deployment.json"

# az login

build=$(cat ./source/BUILD)

export upstream='$upstream'
export edgeAgent='$edgeAgent'
export edgeHub='$edgeHub'
export MaxUpstreamBatchSize="${3}"
export EdgeHubRuntimeLogLevel="info"
export TransmitterRuntimeLogLevel="info"
export IMAGE="$4"
cat $deploymentManifestTemplate | envsubst > $deploymentManifest

echo "MaxUpstreamBatchSize=$MaxUpstreamBatchSize"
echo "image: $IMAGE"
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
echo "waiting 10s for the module to start..."
sleep 10
