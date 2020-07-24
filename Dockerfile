# ---- dotnet build stage ----
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && echo "deb http://download.mono-project.com/repo/debian stretch main" | tee /etc/apt/sources.list.d/mono-official.list \
    && apt-get update && apt-get install -y mono-devel default-jre build-essential libssl-dev libsasl2-2 libsasl2-dev libsasl2-modules-gssapi-mit wget unzip

# lib folder contains librdkafka zip
COPY ./lib/ /
# installing librdkafka manually
RUN unzip librdkafka-1.4.4.zip && \
    cd librdkafka-1.4.4 && \
    ./configure && \
    make && \
    make install

WORKDIR /build/

COPY ./DashboardServer/DashboardServer.csproj ./DashboardServer.csproj
RUN dotnet nuget add source https://ci.appveyor.com/nuget/docker-dotnet-hojfmn6hoed7 && \
    dotnet restore ./DashboardServer.csproj

COPY ./DashboardServer ./

RUN dotnet build -c ${BUILDCONFIG} -o out && dotnet publish ./DashboardServer.csproj -c ${BUILDCONFIG} -o out /p:Version=${VERSION}

# ---- final stage ----

FROM ubuntu:20.04

LABEL Maintainer="Oliver Marco van Komen"

ENV PROGRAM_HOME=/opt/DashboardServer
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CONF_FILES=/conf

# installing aspnet core runtime for ubuntu
RUN apt-get update && \
    apt-get install -y wget && wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg --purge packages-microsoft-prod && dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install aspnetcore-runtime-3.1 curl -y

# installing kerberos client libraries
RUN export DEBIAN_FRONTEND=noninteractive && apt-get install -y krb5-user

# Kafka SASL directory (keytab is placed here)
RUN mkdir /sasl/ && mkdir ${CONF_FILES}
COPY ./configuration/ ${CONF_FILES}/

ENV KEYTAB_LOCATION=/sasl/dashboards.service.keytab

COPY --from=build /build/out ${PROGRAM_HOME}

# Installing dependencies for librdkafka
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get -y install krb5-user kstart \
    libsasl2-2 libsasl2-modules-gssapi-mit libsasl2-modules \
    && apt-get autoremove

# Replacing Nuget Confluent librdkafka build (with redist dependency) with the manually installed librdkafka library.
RUN rm -f ${PROGRAM_HOME}/runtimes/linux-x64/native/librdkafka.so
COPY --from=build /usr/local/lib/librdkafka*.so* ${PROGRAM_HOME}/runtimes/linux-x64/native/

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

CMD [ "docker-entrypoint.sh" ]