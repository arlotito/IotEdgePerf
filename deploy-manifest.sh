#!/bin/bash
# ./deploy-only.sh arturol76-s1-benchmark standard-ds3-v2-edge-1-2-1631626847 200

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
export BUILD=$new_build
cat $deploymentManifestTemplate | envsubst > $deploymentManifest


az iot edge set-modules \
        -n $HUB_NAME \
        -d $DEVICE_NAME \
        --content $deploymentManifest

# restart edgeHub
az iot hub invoke-module-method --method-name 'RestartModule' -n $HUB_NAME -d $DEVICE_NAME -m '$edgeAgent' --method-payload \
'
    {
        "schemaVersion": "1.0",
        "id": "edgeHub"
    }
'

# restart source
az iot hub invoke-module-method --method-name 'RestartModule' -n $HUB_NAME -d $DEVICE_NAME -m '$edgeAgent' --method-payload \
'
    {
        "schemaVersion": "1.0",
        "id": "source"
    }
'

# wait
sleep 10
