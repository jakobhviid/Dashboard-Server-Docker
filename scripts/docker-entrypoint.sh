#!/bin/bash

# Exit if any command has a non-zero exit status (Exists if a command returns an exception, like it's a programming language)
# Prevents errors in a pipeline from being hidden. So if any command fails, that return code will be used as the return code of the whole pipeline
set -eo pipefail

# TODO - Start a http server which can take commands from the outside (run a container, stop a container etc)

check-environment.sh

# The first parameter is the url to curl
function docker_curl() {
    return curl --max-time "${CURL_TIMEOUT}" --silent --unix-socket "$DOCKER_SOCK" "$1" || return 1
}

# How often information should be fetched and sent to kafka
INTERVAL=${INTERVAL:-10}

function get_status() {

    echo "INFO - Monitoring Status Every " "$INTERVAL" " Seconds"
    while true; do
        docker-status.py
        sleep $INTERVAL
    done

}

function health() {
    # TODO - Auto restart unhealthy containers
    echo "INFO - Monitoring Health Every " "$INTERVAL" " Seconds"
    return 0
}

# Check if the function exists
if declare -f "$1" >/dev/null; then
    # call the function
    "$@"
else
    # Show a helpful error
    echo "ERROR - '$1' is not a known function name" >&2
    exit 1
fi
