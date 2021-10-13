#!/bin/bash

# usage:
# ./multi-arch-build-push.sh arlotito/iotedgeperf-transmitter:0.4.4

# docker buildx create --use cross-platform-build
docker buildx use cross-platform-build

## with auto-increment of build number
# build=$(cat BUILD)
# new_build=$(expr $build + 1)
# echo BUILD=$new_build
# echo $new_build > BUILD
# export IMAGE_NAME="xxx:0.4.$new_build"

export IMAGE_NAME="$1"
echo Building and pushing to "$IMAGE_NAME"

sudo docker buildx build -f ./Dockerfile.multi \
        --platform linux/arm/v7,linux/arm64,linux/amd64 \
        -t $IMAGE_NAME \
        --push \
        ./../../../