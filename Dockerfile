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

#FROM mcr.microsoft.com/dotnet/runtime:3.1-buster-slim

RUN aspnetcore_version=3.1.12 \
    && curl -SL --output aspnetcore.tar.gz https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/$aspnetcore_version/aspnetcore-runtime-$aspnetcore_version-linux-arm64.tar.gz \
    && aspnetcore_sha512='ad0f2bec8852037da08d8399ce200f5dde852453f0098b6c9b6451c1050fb7ff49a2fcedcf91f027af758782dfd5016b411d7c74bf8f3f1a19a93a129e48cb1a' \
    && echo "$aspnetcore_sha512  aspnetcore.tar.gz" | sha512sum -c - \
    && tar -ozxf aspnetcore.tar.gz -C /usr/share/dotnet ./shared/Microsoft.AspNetCore.App \
    && rm aspnetcore.tar.gz


ENV DOTNET_PROGRAM_HOME=/opt/DashboardServer

COPY --from=build /build/out ${DOTNET_PROGRAM_HOME}

# Copy necessary scripts + configuration
COPY scripts /tmp/
RUN chmod +x /tmp/*.sh && \
    mv /tmp/* /usr/bin && \
    rm -rf /tmp/*

CMD [ "docker-entrypoint.sh" ]