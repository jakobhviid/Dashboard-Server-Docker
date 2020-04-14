#!/bin/bash

DOCKER_SOCKET_PATH=${DOCKER_SOCKET_PATH:-/var/run/docker.sock}

# Check if socket exists (-S)
if [ ! -S "$DOCKER_SOCKET_PATH" ]; then
    echo -e "\e[1;32mERROR - Docker socket has not been provided! \e[0m"
    exit 1
fi

if [ -z "$SERVER_NAME" ]; then
    echo -e "\e[1;32mERROR - Server identifying name has not been provided! \e[0m"
    exit 1
fi

if [ -z "$DATA_UPLOAD" ]; then
    echo -e "\e[1;32mERROR - 'DATA_UPLOAD' must be set to either 'kafka', 'mongodb' or 'cassandra' \e[0m"
    exit 1
else
    if [ "$DATA_UPLOAD" == kafka ]; then
        if [[ (-z "$KAFKA_URLS") ]]; then
            echo -e "\e[1;32mERROR - 'KAFKA_URL' must be set! \e[0m"
            exit 1
        fi
    fi

    # if [ "$DATA_UPLOAD" == mongodb ]; then

    # fi

    # if [ "$DATA_UPLOAD" == cassandra ]; then

    # fi

    # echo "DATA_UPLOAD value is not currently supported, please contact maintainer of this image"
fi
