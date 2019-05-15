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

        //  Entry point
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

            //  Register Desired Property
            Task.Run( () => RegisterDesiredPropertyHandlerAsync(device, _appSettings.IoTHubUrl)).Wait();
            //  Register Direct Method
            Task.Run(() => RegisterDirectMethodAsync(device)).Wait();

            //  Start Sending and Receiving Threads
            List<Task> tasks = new List<Task>();
            tasks.Add(SendD2CMessageTask(device, "this is a sample"));
            tasks.Add(ReceiveD2CMessageTask(device));

            Task.WaitAll(tasks.ToArray());

            Logger.Info("Running...");
            Console.ReadLine();
        }
        //  Create Device Client
        private static DeviceClient CreateDeviceClient(string url, string id, string key){
            DeviceClient client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(id, key),
                Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            return client;
        }
        //  Register Desired Property Handler
        private static async Task RegisterDesiredPropertyHandlerAsync(Device device, string url){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, device).ConfigureAwait(false);
        }
        //  Register Direct Method
        private static async Task RegisterDirectMethodAsync(Device device){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            await client.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);
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
                Task.Delay(1000 * 3);
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
        private static async Task ReceiveD2CMessageTask(Device device){
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            while(true)
            {
                try
                {
                    Logger.Info($"[{device.Id}]Receiving C2D message...");
                    var message = await client.ReceiveAsync(TimeSpan.FromSeconds(3));
                    if(message != null){
                        var messageData = Encoding.ASCII.GetString(message.GetBytes());
                        Logger.Info($"[{device.Id}]Received Message {messageData}");

                        await client.CompleteAsync(message);
                    }
                    else
                    {
                        Logger.Info("$[{device.Id}]No incoming C2D messages");
                    }
                }
                catch(Exception exp){
                    Logger.Error($"[{device.Id}]Exception while calling ReceiveD2CMessageTask():[{exp.Message}]");
                }
                await Task.Delay(1000 * 3);
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
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;
            DeviceClient client = CreateDeviceClient(_appSettings.IoTHubUrl, device.Id, device.Authentication.SymmetricKey.SecondaryKey);
            
            //  Update Reported Properties
            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }
    }
}
