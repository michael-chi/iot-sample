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
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            Console.WriteLine($"device id {deviceId} : {device.Authentication.SymmetricKey.SecondaryKey}");
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

            //  Register Direct Method
            Task.Run(() => RegisterDirectMethodAsync(device, _appSettings.IoTHubUrl)).Wait();

            //  Start Sending and Receiving Threads
            List<Task> tasks = new List<Task>();
            tasks.Add(SendD2CMessageTask(device, _appSettings.IoTHubUrl, "this is a sample"));
            tasks.Add(ReceiveD2CMessageTask(device, _appSettings.IoTHubUrl));

            Task.WaitAll(tasks.ToArray());

            Logger.Info("Running...");
            Console.ReadLine();
        }
        private static async Task RegisterDirectMethodAsync(Device device, String url){
            DeviceClient client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(device.Id, device.Authentication.SymmetricKey.SecondaryKey),
                            Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
            await client.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);
        }
        public static IConfigurationRoot ReadFromAppSettings()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json", optional: true)
                .Build();
        }
        private static async Task SendD2CMessageTask(Device device, string url, string text){
            while(true){
                await SendD2CMessageAsync(device, url, text);
                Task.Delay(1000 * 3);
            }
        }
        private static async Task SendD2CMessageAsync(Device device, string url, string text){
            DeviceClient client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(device.Id, device.Authentication.SymmetricKey.SecondaryKey),
                            Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
            var msg = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(text));
            msg.Properties.Add("ClientTime", DateTime.UtcNow.ToString());
            Logger.Info($"Sending {text}...");   
            await client.SendEventAsync(msg);
        }
        private static async Task ReceiveD2CMessageTask(Device device, string url){
            DeviceClient client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(device.Id, device.Authentication.SymmetricKey.SecondaryKey),
                            Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
            while(true){
                var message = await client.ReceiveAsync();
                if(message != null){
                    var messageData = Encoding.ASCII.GetString(message.GetBytes());
                    Logger.Info($"Received Message {messageData}");
                }

                await Task.Delay(1000 * 3);
            }
        }

        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            Logger.Info($"Received Direct Method Call[{data}]");

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("OK"), 200));
        }
    }
}
