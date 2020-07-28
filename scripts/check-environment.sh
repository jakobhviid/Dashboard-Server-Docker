#!/bin/bash

. helpers.sh

DOCKER_SOCKET_PATH=${DOCKER_SOCKET_PATH:-/var/run/docker.sock}

# Check if socket exists (-S)
if [ ! -S "$DOCKER_SOCKET_PATH" ]; then
    echo -e "\e[1;31mERROR - Docker socket has not been provided through volumes! \e[0m"
    exit 1
fi

if [ -z "$DASHBOARDS_SERVER_NAME" ]; then
    echo -e "\e[1;31mERROR - 'DASHBOARDS_SERVER_NAME' has not been provided! \e[0m"
    exit 1
fi

# If it has been provided, we should use sasl for kafka connection
if ! [[ -z "${DASHBOARDS_KERBEROS_PUBLIC_URL}" ]]; then
    echo "INFO - 'DASHBOARDS_KERBEROS_PUBLIC_URL' environment variable has been provided. Testing required environment variables for Kafka Kerberos SASL authentication"

    if [[ -z "${DASHBOARDS_KERBEROS_REALM}" ]]; then
        echo -e "\e[1;31mERROR - Missing 'DASHBOARDS_KERBEROS_REALM' environment variable. This is required to enable Kafka Kerberos SASL authentication \e[0m"
        exit 1
    fi

    # If they haven't provided their own keytab in volumes, it is tested if they have provided the necessary environment variables to download the keytab from an API
    if [[ -z "${DASHBOARDS_KERBEROS_PRINCIPAL}" ]]; then
        if [[ -z "${DASHBOARDS_KERBEROS_API_URL}" ]]; then
            echo -e "\e[1;31mERROR - One of either 'DASHBOARDS_KERBEROS_PRINCIPAL' or 'DASHBOARDS_KERBEROS_API_URL' must be supplied! It is required to enable Kafka Kerberos SASL authentication \e[0m"
            exit 1
        else # the user wants to use cfei kerberos api to get keytabs

            if [[ -z "${DASHBOARDS_KERBEROS_API_SERVICE_USERNAME}" ]]; then
                echo -e "\e[1;31mERROR - Missing 'DASHBOARDS_KERBEROS_API_SERVICE_USERNAME' environment variable. This is required to enable Kafka Kerberos SASL authentication \e[0m"
                exit 1
            fi

            if [[ -z "${DASHBOARDS_KERBEROS_API_SERVICE_PASSWORD}" ]]; then
                echo -e "\e[1;31mERROR - Missing 'DASHBOARDS_KERBEROS_API_SERVICE_PASSWORD' environment variable. This is required to enable Kafka Kerberos SASL authentication \e[0m"
                exit 1
            fi

            export DASHBOARDS_KERBEROS_PRINCIPAL="$DASHBOARDS_KERBEROS_API_SERVICE_USERNAME"@"$DASHBOARDS_KERBEROS_REALM"
            # response will be 'FAIL' if it can't connect or if the url returned an error
            response=$(curl --fail --connect-timeout 5 --retry 5 --retry-delay 5 --retry-max-time 30 --retry-connrefused --max-time 5 -X POST -H "Content-Type: application/json" -d "{\"username\":\""$DASHBOARDS_KERBEROS_API_SERVICE_USERNAME"\", \"password\":\""$DASHBOARDS_KERBEROS_API_SERVICE_PASSWORD"\"}" "$DASHBOARDS_KERBEROS_API_URL" -o "$KEYTAB_LOCATION" && echo "INFO - Using the keytab from the API and a principal name of '"$DASHBOARDS_KERBEROS_API_SERVICE_USERNAME"'@'"$DASHBOARDS_KERBEROS_REALM"'" || echo "FAIL")
            if [ "$response" == "FAIL" ]; then
                echo -e "\e[1;31mERROR - Kerberos API did not succeed when fetching keytab. See curl error above for further details \e[0m"
                exit 1
            else
                echo "INFO - Successfully communicated with kerberos and logged in"
            fi
        fi

    else # The user has supplied their own principals
        # test if a keytab has been provided and if it's in the expected directory
        if ! [[ -f "${KEYTAB_LOCATION}" ]]; then
            echo -e "\e[1;31mERROR - Missing kerberos keytab file '"$KEYTAB_LOCATION"'. This is required to enable kerberos when 'DASHBOARDS_KERBEROS_PRINCIPAL' is supplied. Provide it with a docker volume or docker mount \e[0m"
            exit 1
        else
            echo "INFO - Using the supplied keytab and the principal from environment variable 'DASHBOARDS_KERBEROS_PRINCIPAL' "
        fi
    fi

    if [ -z "$DASHBOARDS_BROKER_KERBEROS_SERVICE_NAME" ]; then
        echo "INFO - 'DASHBOARDS_BROKER_KERBEROS_SERVICE_NAME' has not been provided. Defaulting to 'kafka' "
    fi

    # configuring krb5.conf files so acl-manager can communicate with the kerberos server and ensure the provided keytab is correct
    configure_kerberos_server_in_krb5_file "$DASHBOARDS_KERBEROS_REALM" "$DASHBOARDS_KERBEROS_PUBLIC_URL"

else
    echo "INFO - Missing 'DASHBOARDS_KERBEROS_PUBLIC_URL' environment variable. Will not enable Kafka Kerberos SASL authentication"
fi
