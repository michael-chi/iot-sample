Environment Setup
=================

## Install .Net Core 2.1 SDK on Ubuntu
```shell
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo add-apt-repository universe
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.1
```

## Install Required .Net Core packages (Optional)

```shell
dotnet new console
dotnet add package Microsoft.Azure.Devices
dotnet add package Microsoft.Azure.Devices.Client
dotnet add package Microsoft.Extensions.Configuration --version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables --version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.Binder --version 1.17.0
```


IoT Client Sample
=================

## Run Sample
-   Add AppSettings.Json with below configuration

```json
{
    "IoTHubOwnerConnectionString":  "<IoT Hub Owner Connection String>" ,
    "IoTHubUrl":"<IoT Hub URL>"
}
```

-   Build and Run this sample
```shell
cd IoTClient
dotnet restore
dotnet run device001
```

Device Job
==========

-   Sample command line

    -   Calling a Direct Method

```shell
dotnet run directmethod -q "FROM devices WHERE DeviceId='test001'" -t 5 -m SetTelemetryInterval -b test123
```

```
dotnet run directmethod -q "FROM devices WHERE Properties.Desired.Test=123" -t 5 -m SetTelemetryInterval -b test123
```

References
==========

## Environment Setup
[Install .Net Core Runtime](https://docs.microsoft.com/zh-tw/dotnet/core/linux-prerequisites?tabs=netcore2x)

[Install .Net Core 2.1 SDK](https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-2.1.300)

[Azure IoT Toolkit Quick Start](https://github.com/Microsoft/vscode-azure-iot-toolkit/wiki/Quickstart-.NET)

[.Net Core Sample](https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#read-the-telemetry-from-your-hub)


[Execute .Net Core App on Ubuntu](https://stackoverflow.com/questions/46843863/how-to-run-net-core-console-app-on-linux)


## Device Job
[Device Job using .Net](https://docs.microsoft.com/zh-tw/azure/iot-hub/iot-hub-csharp-csharp-schedule-jobs)