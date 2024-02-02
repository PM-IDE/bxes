FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY ./bxes ./bxes
RUN dotnet build ./bxes/src/csharp/Bxes.IntegrationTests -c Release -r linux-x64  \
    --self-contained true -o /app/bxes/src/csharp/Bxes.IntegrationTests/bin/Release/net8.0 

RUN dotnet build ./bxes/src/csharp/Bxes.Console -c Release -r linux-x64  \
    --self-contained true -o /app/bxes/src/csharp/Bxes.Console/bin/Release/net8.0 

RUN mv ./bxes/test_data /app/bxes/test_data
RUN mv ./bxes/src/python /app/bxes/src/python

FROM --platform=linux/amd64 ficus_base:latest as run

RUN apt-get update
RUN apt-get install -y gpg
RUN apt-get install -y wget
RUN wget -O - https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o microsoft.asc.gpg
RUN mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
RUN wget https://packages.microsoft.com/config/ubuntu/22.04/prod.list
RUN mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
RUN chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
RUN chown root:root /etc/apt/sources.list.d/microsoft-prod.list

RUN apt-get update
RUN apt-get install -y dotnet-sdk-8.0

WORKDIR /pmide/ficus/src/python
RUN $python -m pip install .

COPY --from=build /app /app
WORKDIR /app

ENV PYTHONPATH=$python
WORKDIR /app/bxes/src/csharp/Bxes.IntegrationTests/bin/Release/net8.0
ENTRYPOINT dotnet vstest Bxes.IntegrationTests.dll