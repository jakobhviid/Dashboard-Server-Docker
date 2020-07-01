# About
This image is a part of the CFEI kafka / zookeeper stack.
It sends real time docker information to [Docker-Dashboard-Interface](https://github.com/jakobhviid/Dashboard-Server-Interface)

When the image is started, it sends different sorts of information:
* **Overview** docker information about both the running and stopped containers on which the image is running. This information is similar to the information docker displays with the command 'docker ps' / 'docker container ls'
* **Stats** docker information about all the running containers on which the image is running. This information is similar to the information docker displats with the command 'docker stats' / 'docker container stats'
* **Logs** (not implemented yet)

The image will send docker information atleast every 15 minutes, however it will also send if something important happens, e.g. a container stopping, unhealthy or if a container uses more ressources then the allowed threshold (defined through enviorment variables).

# How to use
This docker-compose file show the deployment of the dashboard-server container.

```
version: "3"

services:
  server:
    image: cfei/docker-dashboard-server
    container_name: dashboard-server
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      SERVER_NAME: CfeiServer1
      CHECK_INTERVAL_SECONDS: 5
```

# Configuration
##### Required environment variables

- `DOCKER SOCKET`: This enables necessary communications with the Docker Engine API. It is required to provide the host docker socket through volumes ('/var/run/docker.sock:/var/run/docker.sock'). The image will fail if it is not provided.

- `SERVER_NAME`: Defines the name of the server which will be displayed in the [Docker-Dashboard-Interface](https://github.com/jakobhviid/Dashboard-Server-Interface). This has to be unique from other docker dashboard servers. Required.

##### Optional environment variables

- `KAFKA_URL`: Comma seperated list of one or more kafka urls. It will default to cfei's own kafka cluster 'kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092'.

- `PROCESSES_TO_START`: With this variable it is possible to define what processes inside the image should start. So if the value of this variable is 'overviewdata,commandserver' statsdata will not be sent. The available processes are 'overviewdata', 'statsdata' & 'commandserver'. By default all processes will be started and is the recommended setting.

- `CHECK_INTERVAL_SECONDS`: The image uses two different timers. The first is a send interval which is 15 minutes, the second is a check interval. The check interval defines how often the image checks if some of the containers have changed. The lower the interval the more ressources this image will use. It defaults to 15 seconds.

**Tolerances**
When the image collects docker stats data it will only send data if these tolerances are exceeded.

- `CPU_MEM_TOLERANCE_PERCENT`: Defines the tolerances of cpu and memory usage in percent. E.g. if set to 30, the image will only send data if containers cpu or memory difference between last read and current read is over 30 percent. Defaults to 35.

- `NET_INPUT_TOLERANCE_BYTES`: Defines the tolerances of network download in bytes. E.g. if set to 1000, the image will only send data if containers has downloaded more than 1000 bytes since the last read. Defaults to 500.

- `NET_OUTPUT_TOLERANCE_BYTES`: Defines the tolerances of network upload in bytes. E.g. if set to 1000, the image will only send data if containers has uploaded more than 1000 bytes since the last read. Defaults to 500.

- `DISK_INPUT_TOLERANCE_BYTES`: Defines the tolerances of disk read in bytes. E.g. if set to 1000, the image will only send data if containers has read more than 1000 bytes since the last read. Defaults to 500.

- `DISK_OUTPUT_TOLERANCE_BYTES`: Defines the tolerances of disk write in bytes. E.g. if set to 1000, the image will only send data if containers has written more than 1000 bytes since the last read. Defaults to 500.
