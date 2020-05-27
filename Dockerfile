FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

COPY ./DashboardServer/DashboardServer.csproj /build/DashboardServer.csproj
RUN dotnet nuget add source https://ci.appveyor.com/nuget/docker-dotnet-hojfmn6hoed7
RUN dotnet restore ./build/DashboardServer.csproj

COPY ./DashboardServer /build/

WORKDIR /build/

RUN dotnet publish ./DashboardServer.csproj -c ${BUILDCONFIG} -o out /p:Version=${VERSION}

FROM ubuntu:18.04

LABEL Maintainer="Oliver Marco van Komen"

ENV PROGRAM_HOME=/opt/DashboardServer
ENV ASPNETCORE_ENVIRONMENT=Production

RUN apt update && \
    apt install -y wget && wget https://packages.microsoft.com/config/ubuntu/19.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg --purge packages-microsoft-prod && dpkg -i packages-microsoft-prod.deb && \
    apt update && \
    apt install aspnetcore-runtime-3.1 -y

COPY --from=build /build/out ${PROGRAM_HOME}

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

CMD [ "docker-entrypoint.sh" ]