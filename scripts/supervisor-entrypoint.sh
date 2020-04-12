#!/bin/bash

# Exit if any command has a non-zero exit status (Exists if a command returns an exception, like it's a programming language)
# Prevents errors in a pipeline from being hidden. So if any command fails, that return code will be used as the return code of the whole pipeline
set -eo pipefail

check-environment.sh

# How often information should be fetched and sent to kafka
export INTERVAL=${INTERVAL:-10}

IFS=', ' read -r -a processes <<<"$PROCESSES_TO_START"
echo "${processes[0]}"
if [[ " ${processes[@]} " =~ "get_status" ]]; then
    supervisorctl start get_status
fi

if [[ " ${processes[@]} " =~ "command_server" ]]; then
    if [[ -z "${ADVERTISED_COMMAND_URL}" ]]; then
        echo -e "\e[1;32mERROR - If Command server should be started 'ADVERTISED_COMMAND_URL' must be provided! \e[0m"
        exit 1
    fi
    supervisorctl start command_server
fi