#!/bin/bash

HUB_NAME=arturol76-s1-benchmark
DEVICE_NAME=standard-ds3-v2-edge-1-2-1631626847
deploymentManifestTemplate="../manifests/deployment.template.json"
deploymentManifest="../manifests/deployment.json"

# docker buildx create --use cross-platform-build
docker buildx use cross-platform-build
# az login

build=$(cat BUILD)
new_build=$(expr $build + 1)
echo BUILD=$new_build
echo $new_build > BUILD

export upstream='$upstream'
export edgeAgent='$edgeAgent'
export edgeHub='$edgeHub'
export MaxUpstreamBatchSize="200"
export BUILD=$new_build
cat $deploymentManifestTemplate | envsubst > $deploymentManifest

# export MODULE_PATH="edgeSolution/modules/source-dotnet-mqtt"
# export IMAGE_NAME=arlotito/source-mqtt
# export IMAGE_VER=0.0.16

# sudo docker buildx build -f $MODULE_PATH/Dockerfile.multi --platform linux/arm/v7,linux/arm64,linux/amd64 -t $IMAGE_NAME:$IMAGE_VER --push $MODULE_PATH

export MODULE_PATH="."
export IMAGE_NAME=arlotito/source
export IMAGE_VER=0.3.11-twin-$new_build

sudo docker buildx build -f $MODULE_PATH/Dockerfile.multi --platform linux/arm/v7,linux/arm64,linux/amd64 -t $IMAGE_NAME:$IMAGE_VER --push $MODULE_PATH

#export MODULE_PATH="edgeSolution/modules/sink"
#export IMAGE_NAME=arlotito/sink
#export IMAGE_VER=0.4.5
#sudo docker buildx build -f $MODULE_PATH/Dockerfile.multi --platform linux/arm/v7,linux/arm64,linux/amd64 -t $IMAGE_NAME:$IMAGE_VER --push $MODULE_PATH

az iot edge set-modules \
        -n $HUB_NAME \
        -d $DEVICE_NAME \
        --content $deploymentManifest