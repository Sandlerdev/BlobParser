FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS installer-env





COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

ENV table_name FTEG_Data
ENV storageconnection ConnectionToFiles
ENV SQLAZURECONNSTR_SQLConnectionString SQLDBConnectionString
ENV AzureWebJobsStorage StorageForFunction


COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]