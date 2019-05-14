Commands
========

```shell
dotnet new console
dotnet add package Microsoft.Azure.Devices
dotnet add package Microsoft.Azure.Devices.Client
dotnet add package Microsoft.Extensions.Configuration -version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.Json -version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables -version 1.17.0
dotnet add package Microsoft.Extensions.Configuration.Binder -version 1.17.0
```
        <PackageReference Include="Microsoft.Extensions.Configuration" Version="2.1.1" />
        <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="2.1.1" />
        <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="2.1.1" />
        <PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="2.1.1" />
        <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="2.1.1" />
References
==========
- https://github.com/Microsoft/vscode-azure-iot-toolkit/wiki/Quickstart-.NET
- https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#read-the-telemetry-from-your-hub