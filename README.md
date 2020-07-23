# About
This image is a part of the CFEI kafka / zookeeper stack.
It sends real time docker information to [Docker-Dashboard-Interface](https://github.com/jakobhviid/Dashboard-Interface-Docker)

When the image is started, it sends different sorts of information:
* **Overview** docker information about both the running and stopped containers on which the image is running. This information is similar to the information docker displays with the command 'docker ps' / 'docker container ls'
* **Stats** docker information about all the running containers on which the image is running. This information is similar to the information docker displats with the command 'docker stats' / 'docker container stats'
* **Logs** (not implemented yet)

The image will send docker information atleast every 15 minutes, however it will also send if something important happens, e.g. a container stopping, unhealthy or if a container uses more ressources then the allowed threshold (defined through enviorment variables).

# How to use
This docker-compose file shows the deployment of the dashboard-server container.

```
version: "3"

services:
  server:
    image: cfei/dashboard-server
    container_name: dashboard-server
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      DASHBOARDS_SERVER_NAME: CfeiServer1
```

# Configuration
#### Required environment variables

- `DASHBOARDS_DOCKER SOCKET`: This enables necessary communications with the Docker Engine API. It is required to provide the host docker socket through volumes ('/var/run/docker.sock:/var/run/docker.sock'). The image will fail if it is not provided.

- `DASHBOARDS_SERVER_NAME`: Defines the name of the server which will be displayed in the [Docker-Dashboard-Interface](https://github.com/jakobhviid/Dashboard-Server-Interface). This has to be unique from other docker dashboard servers. Required.

#### Optional environment variables

- `DASHBOARDS_KAFKA_URL`: Comma seperated list of one or more kafka urls. It will default to cfei's own kafka cluster 'kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092'.

- `DASHBOARDS_KERBEROS_PUBLIC_URL`: Public DNS of the kerberos server to use. Required if the Kafka URL has SASL authentication.
  
- `DASHBOARDS_KERBEROS_REALM`: The realm to use on the kerberos server. Required if the Kafka URL has SASL authentication.
    
- `DASHBOARDS_KERBEROS_API_URL`: The URL to use when dashboard server fetches its keytab from the kerberos server. The URL has to point to an HTTP POST Endpoint. The image will then supply the values of 'DASHBOARDS_KERBEROS_API_SERVICE_USERNAME' and 'DASHBOARDS_KERBEROS_API_SERVICE_PASSWORD' to the request.
  
- `DASHBOARDS_KERBEROS_API_SERVICE_USERNAME`: The username to use when fetching keytab on 'DASHBOARDS_KERBEROS_API_URL'.
  
- `DASHBOARDS_KERBEROS_API_SERVICE_PASSWORD`: The password to use when fetching keytab on 'DASHBOARDS_KERBEROS_API_URL'.
  
- `DASHBOARDS_BROKER_KERBEROS_SERVICE_NAME`: This should be the principal name of the service keytab the kafka broker(s) uses. So if kafka uses a keytab with a principal name of 'kafka'. This environment variable should be 'kafka'.

- `DASHBOARDS_KERBEROS_PRINCIPAL`: The principal that dashboard server should use from the kerberos server. Required if you want to supply your own zookeeper keytab through volumes.

- `DASHBOARDS_PROCESSES_TO_START`: With this variable it is possible to define what processes inside the image should start. So if the value of this variable is 'overviewdata,commandserver' statsdata will not be sent. The available processes are 'overviewdata', 'statsdata' & 'commandserver'. By default all processes will be started and is the recommended setting.

- `DASHBOARDS_CHECK_INTERVAL_SECONDS`: The image uses two different timers. The first is a send interval which is 15 minutes, the second is a check interval. The check interval defines how often the image checks if some of the containers have changed. The lower the interval the more ressources this image will use but it will also detect health issues quicker. So for critical images lower it to meet uptime requirements. It defaults to 15 seconds.


**Tolerances**
When the image collects docker stats data it will only send data if these tolerances are exceeded.

- `DASHBOARDS_CPU_MEM_TOLERANCE_PERCENT`: Defines the tolerances of cpu and memory usage in percent. E.g. if set to 30, the image will only send data if containers cpu or memory difference between last read and current read is over 30 percent. Defaults to 35.

- `DASHBOARDS_NET_INPUT_TOLERANCE_BYTES`: Defines the tolerances of network download in bytes. E.g. if set to 1000, the image will only send data if containers has downloaded more than 1000 bytes since the last read. Defaults to 500.

- `DASHBOARDS_NET_OUTPUT_TOLERANCE_BYTES`: Defines the tolerances of network upload in bytes. E.g. if set to 1000, the image will only send data if containers has uploaded more than 1000 bytes since the last read. Defaults to 500.

- `DASHBOARDS_DISK_INPUT_TOLERANCE_BYTES`: Defines the tolerances of disk read in bytes. E.g. if set to 1000, the image will only send data if containers has read more than 1000 bytes since the last read. Defaults to 500.

- `DASHBOARDS_DISK_OUTPUT_TOLERANCE_BYTES`: Defines the tolerances of disk write in bytes. E.g. if set to 1000, the image will only send data if containers has written more than 1000 bytes since the last read. Defaults to 500.
