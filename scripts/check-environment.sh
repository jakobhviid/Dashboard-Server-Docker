#!/bin/bash

DOCKER_SOCKET_PATH=${DOCKER_SOCKET_PATH:-/var/run/docker.sock}

# Check if socket exists (-S)
if [ ! -S "$DOCKER_SOCKET_PATH" ]; then
    echo -e "\e[1;32mERROR - Docker socket has not been provided through volumes! \e[0m"
    exit 1
fi

if [ -z "$SERVER_NAME" ]; then
    echo -e "\e[1;32mERROR - 'SERVER_NAME' has not been provided! \e[0m"
    exit 1
fi
