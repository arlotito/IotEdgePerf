#!/bin/bash

# docker buildx create --use cross-platform-build
docker buildx use cross-platform-build

# get current build number
build=$(cat BUILD)

# increment build number
new_build=$(expr $build + 1)
echo BUILD=$new_build
echo $new_build > BUILD

export DOCKERFILE_PATH="."
export IMAGE_NAME="arlotito/profiler"
export IMAGE_TAG="0.3.$new_build"

echo Building and pushing to "$IMAGE_NAME:$IMAGE_TAG"

sudo docker buildx build -f $DOCKERFILE_PATH/Dockerfile.multi \
        --platform linux/arm/v7,linux/arm64,linux/amd64 \
        -t $IMAGE_NAME:$IMAGE_TAG \
        --push \
        $DOCKERFILE_PATH