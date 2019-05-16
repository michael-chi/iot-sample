using System.Net;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices;
using System.IO;
using System.Text;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
namespace Nestle
{
    class Program
    {
        //  Local Variables
        private static AppSettings _appSettings = null;
        private static DeviceClient _client = null;

        //  Ensure device exists in IoT Hub, if not exist, create one.
        //  Returns Device reference
        private static async Task<Device> EnsureDeviceAsync(string deviceId){
            var connectionString = _appSettings.IoTHubOwnerConnectionString;
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            Device device;
            try
            {
                device = await registryManager.GetDeviceAsync(deviceId);

                if(device == null){
                    device = await registryManager.AddDeviceAsync(new Device(deviceId));
                }
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);  
                Logger.Error($"device id {deviceId} : {device.Authentication.SymmetricKey.SecondaryKey}");
            }
            return device;
        }
        
        //  Ensure Args is correctly passed to the program
        public static bool EnsureArgs(string[] args){
            if(args.Length < 1){
                Logger.Error($"Usage: dotnet run <deviceId>");
                return false;
            }
            return true;
        }
        //  ======================================
        //              Entry point 
        //  ======================================
        public static void Main(string[] args)
        {
            //  Initialize configuration block
            _appSettings = ReadFromAppSettings().Get<AppSettings>();

            //  Ensure args
            if(!EnsureArgs(args)){
                return;
            }

            //  Get Device Id from command line
            var deviceId = args[0];

            //  Ensure Device exists in IoT Hub
            var task = Task.Run(async () => await EnsureDeviceAsync(deviceId));
            task.Wait();
            var device = task.Result as Device;

            CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            
            //  Start Sending and Receiving Threads
            List<Task> tasks = new List<Task>();
            tasks.Add(SendD2CMessageTask(device, "this is a sample"));
            tasks.Add(ReceiveC2DMessageTask(device));

            Task.WaitAll(tasks.ToArray());

            Logger.Info("Running...");
            Console.ReadLine();
        }
        //  Retrieve entire Twins object when connected
        private static async Task RetriveTwinsAsync(Device device){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            var twin = await client.GetTwinAsync();

            Logger.Info("Retrieved full Twins.");
            Logger.Info($"\t{JsonConvert.SerializeObject(twin)}");
        }
        //  Create Device Client
        private static DeviceClient CreateDeviceClient(string url, string id, string key){
            if(_client == null){
                Logger.Info("DeviceClient not exists, recreating DeviceClient object...");
                _client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(id, key),
                    Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                _client.SetConnectionStatusChangesHandler(OnConnectionStatusChanged);
                //  For testing purpose, I am using NoRetry policy, in real case, you should define your own retry policy
                _client.SetRetryPolicy(new NoRetry());

                //  Ensure Device exists in IoT Hub
                var task = Task.Run(async () => await EnsureDeviceAsync(id));
                task.Wait();
                var device = task.Result as Device;

                //  Retrieve Full Twin Properties so that client device can re-configure itself based on backend configuration.
                Task.Run(() => RetriveTwinsAsync(device)).Wait();

                //  Register Desired Property
                Task.Run(() => RegisterDesiredPropertyHandlerAsync(device, _appSettings.IoTHubUrl)).Wait();
                
                //  Register Direct Method
                Task.Run(() => RegisterDirectMethodAsync(device)).Wait();
            }
            return _client;
        }
        //  Register Desired Property Handler
        private static async Task RegisterDesiredPropertyHandlerAsync(Device device, string url){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, device).ConfigureAwait(false);
        }
        //  Register Direct Method
        private static async Task RegisterDirectMethodAsync(Device device){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            //  Set SetTelemetryInterval method handler
            await client.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);

            //  Set defailt method handler
            await client.SetMethodDefaultHandlerAsync(DefaultMethodHandler, null);
        }
        //  Read AppSettings.Json
        public static IConfigurationRoot ReadFromAppSettings()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json", optional: true)
                .Build();
        }
        //  Send D2C message
        private static async Task SendD2CMessageTask(Device device, string text){
            while(true)
            {
                await SendD2CMessageAsync(device, text);
                await Task.Delay(1000 * 3);
            }
        }
        //  Sending thread - sends D2C messages to IoT Hub
        private static async Task SendD2CMessageAsync(Device device, string text){
            try
            {
                DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
                var msg = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(text));
                msg.Properties.Add("ClientTime", DateTime.UtcNow.ToString());
                Logger.Info($"[{device.Id}]Sending {text}...");   
                await client.SendEventAsync(msg);
            }
            catch(Exception exp){
                Logger.Error($"[{device.Id}]Exception while calling SendD2CMessageAsync():[{exp.Message}]");
            }
        }
        //  Receiving thread - receives C2D messages from IoT Hub
        private static async Task ReceiveC2DMessageTask(Device device){
            Logger.Info($"[{device.Id}]Receiving C2D message...");
            while(true)
            {
                try
                {
                    DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);

                    var message = await client.ReceiveAsync();
                    if(message != null){
                        var messageData = Encoding.ASCII.GetString(message.GetBytes());
                        Logger.Info($"[{device.Id}]Received Message {messageData}");

                        await client.CompleteAsync(message);
                    }
                }
                catch(Exception exp){
                    Logger.Error($"[{device.Id}]Exception while calling ReceiveC2DMessageTask():[{exp.Message}]");
                    Logger.Error($"\r\n================\r\n{exp.StackTrace}\r\n===============");
                }
                await Task.Delay(1000 * 3);
            }
        }
        //  Default Method Handler
        private static Task<MethodResponse> DefaultMethodHandler(MethodRequest methodRequest, object userContext)
        {
            try{
                var data = Encoding.UTF8.GetString(methodRequest.Data);
                Logger.Info($"\t>> Received Default Method Call[{data}]");

                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"Status\":\"OK\"}"), 200));
            }
            catch(Exception exp){
                Logger.Error($"Exception Direct Method:{exp.Message}");
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes($"Exception:{exp.Message}"), 500));
            }
        }
        //  Direct Method Handler
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            try{
                var data = Encoding.UTF8.GetString(methodRequest.Data);
                Logger.Info($"Received Direct Method Call[{data}]");

                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"Status\":\"OK\"}"), 200));
            }
            catch(Exception exp){
                Logger.Error($"Exception Direct Method:{exp.Message}");
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes($"Exception:{exp.Message}"), 500));
            }
        }

        //  Desired Property Handler
        private static async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Logger.Info($"\tDesired property changed:{desiredProperties.ToJson()}");
            Device device = userContext as Device;
            Logger.Info("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["status_updated_time"] = DateTime.Now;
            reportedProperties["status_updated"] = true;

            //  Simulate status update failed/succeed
            if(new Random().Next(1, 100) >= 80){
                reportedProperties["status"] = "ok";
            }else{
                reportedProperties["status"] = "failed";
            }
            
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            
            //  Update Reported Properties
            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        //  Connection Status Changed Handler
        public static void OnConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason){
            if(status != ConnectionStatus.Connected){
                if(reason == ConnectionStatusChangeReason.No_Network ||
                        reason == ConnectionStatusChangeReason.Retry_Expired ||
                        reason == ConnectionStatusChangeReason.Device_Disabled ||
                        reason == ConnectionStatusChangeReason.Bad_Credential ||
                        reason == ConnectionStatusChangeReason.Expired_SAS_Token) {
                    Logger.Info($"Connection status changed:{status.ToString()}");
                    Logger.Info($"\tReason:{reason.ToString()}");
                    Logger.Info($"==> Disposing DeviceClient");
                    if(_client != null)
                        _client = null;
                }
            }

        }
    }
}
