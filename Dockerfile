## Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /source

EXPOSE 80
EXPOSE 443

LABEL group="astroid"
LABEL project="astroid"
LABEL type="build"

COPY . .

RUN apt-get install --yes curl
RUN curl --silent --location https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get install --yes nodejs

# Change the Directory
WORKDIR /source/

# aspnet-core
RUN dotnet restore src/Astroid.Web/Astroid.Web.csproj
RUN dotnet publish src/Astroid.Web/Astroid.Web.csproj --output /astroid/ --configuration Release

COPY src/Astroid.Web/wwwroot /astroid

# copy devcert.pfx to /astroid
COPY src/Astroid.Web/devcert.pfx /astroid

## Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

# Change the Directory
WORKDIR /astroid

LABEL group="astroid"
LABEL project="astroid"
LABEL type="runtime"

COPY --from=builder /astroid .
ENTRYPOINT ["dotnet", "Astroid.Web.dll"]