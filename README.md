# Blob Parser

## Solution Architecture
This solution was created as a sample architecture that would simplify the ingestion of data from FactoryTalk Edge Gateway into Azure IoT Hub and subsequently other Azure Resources.  

This solution can certainly function as is, however, changes to resources can be achieved by editing the associated ARM Template file used to deploy the resources.  

![High-Level Architecture](./out/diagrams/solution/solution.png)

The architecture in this solution was selected to meet the following requirements:

1. All Data received by the IoTHub will be added to long term storage.
2. Data received will be made available to clients via interface to well known SQL endpoint.
3. Data in SQL DB will have a variable time to live (hence the need for long term storage).
4. In the future it may be necessary to route this data to other storage and or services.

## Deployment

This repo contains an ARM Template which can be used to deploy this solution.  You can clone the repo and modify the template to meet your needs or simply click the button below to initiate a deployment with the pre-configured values.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FSandlerdev%2FBlobParser%2Fmaster%2FARMTemplates%2FazureDeploy.json)

*The Deployment generally takes 5 to 10 minutes, once complete you should be presented with a button that will take you to your resource group.*

Once complete the resource group will look similar to this:
![RG deployment](.\images\resourcegroup.png)

*Note there may be other objects in the resource group as well.  The deployment logic will create a storage account and an Azure Container instance in order to execute an Azure CLI Script.  These Resources will automatically be deleted after approximately one hour.*

## Configuring an IOT Hub Device

After deployment is complete you will need to configure one or more IoT Hub Devices.
![Create Device](.\images\CreateIOTHubDevice.png)

Once the device is created, click on the new device, and copy its Connection String to use in the FTEG Configuration.

![Get Connection String](.\images\GetIOTDeviceCS.png)

After you have configured your device in IoT Hub, you need to configure the FactoryTalk Edge Gateway to send data to this application.  
![FTEG Config](.\images\FTEG.png)

See FactoryTalk Edge Gateway documentation for more details.
## Blob Parser Function Overview
The Azure Function in this solution was created in Visual Studio Code and targets .Net Core (C#).  The Function uses a Blob Trigger, which causes the function to be called each time a new blob is written to the storage account.  The logic in the function validates the file type, then parses the data and inserts the data into the SQL DB.

Data in blob is populated by IoT Hub Routing - data originates from FTEG, giving us a well known data format.

*It should be noted that while this function will be triggered by any file being written to the "Iotdata" container in the solution's Storage Account, it is expecting data formatted consistently with what is currently sent from FTEG.*
## Local Build and Debugging

To run or debug this Azure Function locally you will need Visual Studio 2019 or VSCode installed.  See the following article for more info:
[Develop Azure Functions Using VS Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs-code?tabs=csharp)

### Connection Strings etc

Local debugging uses a *local.settings.json* file for connection strings etc.  In the published Azure Function these same parameters will be retrieved from the App Settings of the Function Host or App Service.

To prevent "Secrets" from being stored in source control the local.settings.json file has not been included in this repository.   As a result it must be created in the local build environment.

The file should be formatted as shown below and use the keys shown below (key values must be updated)

---

```local.settings.json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "<ConnectionString of Storage Account to store AZ Function MetaData>",
        "storageconnection": "<ConnectionString of Storage Account to watch for IoT Data>",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet"

    },
    "ConnectionStrings": {
        "SQLConnectionString": "<ConnectionString for AzureSQL DB to write the data to.>"
    }
}

```
