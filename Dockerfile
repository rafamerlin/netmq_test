#to test run first a `dotnet publish -c Release`
#then docker build -t test_netmq .
#and docker run -it test_netmq


# I'm joining both these images: https://github.com/docker-library/python/blob/1b78ff417e41b6448d98d6dd6890a1f95b0ce4be/3.8/buster/Dockerfile
# and https://github.com/dotnet/dotnet-docker/blob/11a446f2826c2b8c51baa774584ff3f28ba0e88e/src/sdk/3.1/buster/amd64/Dockerfile
# Since the python one is a bit more complex, I'm just using the FROM from python and as both use buildpack-deps:buster-scm I can just build dotnet in here.
FROM python:3.8.5-buster

####
# This is the DOTNET dockerfile commands:
####

ENV \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip \
    # PowerShell telemetry for docker image usage
    POWERSHELL_DISTRIBUTION_CHANNEL=PSDocker-DotnetCoreSDK-Debian-10 \
    # Disable telemetry
    DOTNET_CLI_TELEMETRY_OPTOUT=true
    

# Install .NET CLI dependencies + libzmq3
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu63 \
        libssl1.1 \
        libstdc++6 \
        zlib1g \
        libzmq3-dev \ 
    && rm -rf /var/lib/apt/lists/*

# Install .NET Core SDK
RUN dotnet_sdk_version=3.1.401 \
    && curl -SL --output dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Sdk/$dotnet_sdk_version/dotnet-sdk-$dotnet_sdk_version-linux-x64.tar.gz \
    && dotnet_sha512='5498add9ef83da44d8f7806ca1ce335ad4193c0d3181a5abda4b65e116c7331aac37a229817ff148e4487e9734ad2438f102a0eef0049e26773a185ceb78aac4' \
    && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -ozxf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    # Trigger first run experience by running arbitrary cmd
    && dotnet help

# Install PowerShell global tool
RUN powershell_version=7.0.3 \
    && curl -SL --output PowerShell.Linux.x64.$powershell_version.nupkg https://pwshtool.blob.core.windows.net/tool/$powershell_version/PowerShell.Linux.x64.$powershell_version.nupkg \
    && powershell_sha512='580f405d26df40378f3abff3ec7e4ecaa46bb0e46bcb2b3c16eff2ead28fde5aaa55c19501f73315b454e68d98c9ef49f8887c36e7c733d7c8ea3dd70977da2f' \
    && echo "$powershell_sha512  PowerShell.Linux.x64.$powershell_version.nupkg" | sha512sum -c - \
    && mkdir -p /usr/share/powershell \
    && dotnet tool install --add-source / --tool-path /usr/share/powershell --version $powershell_version PowerShell.Linux.x64 \
    && dotnet nuget locals all --clear \
    && rm PowerShell.Linux.x64.$powershell_version.nupkg \
    && ln -s /usr/share/powershell/pwsh /usr/bin/pwsh \
    && chmod 755 /usr/share/powershell/pwsh \
    # To reduce image size, remove the copy nupkg that nuget keeps.
    && find /usr/share/powershell -print | grep -i '.*[.]nupkg$' | xargs rm

#####
#   Install python libraries
#####

RUN set -ex; \
    pip install --no-binary=:all: pyzmq

#####
#   Run dotnet app now.
#####
COPY bin/Release/netcoreapp3.1/publish/ App/
COPY test.py App/test.py

WORKDIR /App
ENTRYPOINT ["dotnet", "netmq_example.dll"]
