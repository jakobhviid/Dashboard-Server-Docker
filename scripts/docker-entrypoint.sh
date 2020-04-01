#!/bin/bash

# Exit if any command has a non-zero exit status (Exists if a command returns an exception, like it's a programming language)
# Prevents errors in a pipeline from being hidden. So if any command fails, that return code will be used as the return code of the whole pipeline
set -eo pipefail
# turn on bash's job control
set -m

check-environment.sh

# How often information should be fetched and sent to kafka
export INTERVAL=${INTERVAL:-10}

/usr/bin/supervisord &
sleep 3
IFS=', ' read -r -a processes <<<"$PROCESSES_TO_START"
echo "${processes[0]}"
if [[ " ${processes[@]} " =~ "get_status" ]]; then
    supervisorctl start get_status
fi

if [[ " ${processes[@]} " =~ "command_server" ]]; then
    supervisorctl start command_server
fi

fg

# function get_status() {
#     echo "INFO - Monitoring Status Every" "$INTERVAL" "Seconds"
#     if [[ "$DATA_UPLOAD" == kafka ]]; then
#         "$UPDATERS_HOME"/docker_status_kafka.py "$INTERVAL" >>/output.txt 2>&1
#     fi
# }

# function command_server() {
#     echo "INFO - Starting command server"
#     "$SERVER_HOME"/command_server.py >>/output.txt 2>&1
# }

# # Check if the function exists
# for argument in "$@"; do
#     if declare -f "$argument" >/dev/null; then
#         # call the function
#         echo "INFO - Running '$argument'"
#         "$argument" &
#     else
#         echo "INFO - Supplied command '$argument' is not a valid command for this container (ignore this if you are debugging the container)"
#         exec "$argument"
#         break
#     fi
# done
