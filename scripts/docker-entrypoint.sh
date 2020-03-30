#!/bin/bash

# Exit if any command has a non-zero exit status (Exists if a command returns an exception, like it's a programming language)
# Prevents errors in a pipeline from being hidden. So if any command fails, that return code will be used as the return code of the whole pipeline
set -eo pipefail

# TODO - Start a http server which can take commands from the outside (run a container, stop a container etc)

check-environment.sh

# How often information should be fetched and sent to kafka
INTERVAL=${INTERVAL:-10}

function get_status() {
    echo "INFO - Monitoring Status Every" "$INTERVAL" "Seconds"
    if [[ "$DATA_UPLOAD" == kafka ]]; then
        python3 "$PYTHON_SCRIPTS_HOME"/docker-status-kafka.py "$INTERVAL"
    fi
}

function test() {
    echo "I am just a simple test for multiple commands"
}

# Check if the function exists
for argument in "$@"; do
    if declare -f "$argument" >/dev/null; then
        # call the function
        echo "INFO - Running '$argument'"
        "$argument" &>>/out.log &
    else
        echo "INFO - Supplied command '$argument' is not a valid command for this container (ignore this if you are debugging the container)"
        exec "$argument"
        break
    fi
done

tail -f /out.log
