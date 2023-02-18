 # https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY McServerApi/*.csproj ./McServerApi/
RUN dotnet restore

# copy everything else and build app
COPY McServerApi/. ./McServerApi/
WORKDIR /source/McServerApi
RUN dotnet publish -c release -o /app
RUN cp -r /source/McServerApi/__mc_server_template /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./
RUN mkdir -p __mc_maps
RUN mkdir -p __jar_cache
RUN mkdir -p __del_mc_maps

RUN apt update
RUN apt install -y openjdk-17-jre-headless
RUN apt clean

ENV Config__ApiPort=8080
EXPOSE 8080 25565
ENTRYPOINT ["dotnet", "McServerApi.dll"]
