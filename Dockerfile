# ---- dotnet build stage ----
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

WORKDIR /build/

COPY ./DashboardServer/DashboardServer.csproj ./DashboardServer.csproj
RUN dotnet restore ./DashboardServer.csproj

COPY ./DashboardServer ./

RUN dotnet build -c ${BUILDCONFIG} -o out && dotnet publish ./DashboardServer.csproj -c ${BUILDCONFIG} -o out /p:Version=${VERSION}

# ---- final stage ----
FROM omvk97/dotnet-kerberos-auth

ENV DOTNET_PROGRAM_HOME=/opt/DashboardServer

COPY --from=build /build/out ${DOTNET_PROGRAM_HOME}

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

CMD [ "docker-entrypoint.sh" ]