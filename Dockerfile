FROM ubuntu:18.04

RUN apt update && \
    apt install -y curl python3 python3-pip

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

ENV UPDATERS_HOME=/opt/updaters
ENV SERVER_HOME=/opt/command_server

COPY updaters ${UPDATERS_HOME}
COPY command_server ${SERVER_HOME}

RUN pip3 install docker kafka-python flask

ENTRYPOINT [ "docker-entrypoint.sh" ]

CMD [ "get_status"]