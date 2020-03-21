FROM ubuntu:18.04

LABEL maintainer="Oliver Marco van Komen (oma@mmmi.sdu.dk)"

RUN apt update && \
    apt install -y jq curl python3 python3-pip && \
    pip3 install docker kafka-python

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

ENV PYTHON_SCRIPTS_HOME=/opt/python_scripts

COPY updaters ${PYTHON_SCRIPTS_HOME}
RUN chmod +x ${PYTHON_SCRIPTS_HOME}/*.py

# ENTRYPOINT [ "docker-entrypoint.sh" ]

CMD [ "docker-entrypoint.sh", "get_status"]