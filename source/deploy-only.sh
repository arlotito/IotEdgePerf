#!/bin/bash

HUB_NAME=arturol76-s1-benchmark
DEVICE_NAME=standard-ds3-v2-edge-1-2-1631626847
deploymentManifestTemplate="./manifests/deployment.no-metrics.twin.template.json"
deploymentManifest="./manifests/deployment.no-metrics.twin.json"

# az login

build=$(cat BUILD)
echo $build > BUILD

export upstream='$upstream'
export edgeAgent='$edgeAgent'
export edgeHub='$edgeHub'
export BUILD=$new_build
cat $deploymentManifestTemplate | envsubst > $deploymentManifest

az iot edge set-modules \
        -n $HUB_NAME \
        -d $DEVICE_NAME \
        --content $deploymentManifest