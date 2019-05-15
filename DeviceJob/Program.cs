using System;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using CommandLine;
namespace DeviceJob
{
    class Program
    {
        static DeviceClient _client = null;
        static AppSettings _appSettings = null;
        private static DeviceClient CreateDeviceClient(string url, string id, string key){
            if(_client == null){
                _client = DeviceClient.Create(url, new DeviceAuthenticationWithRegistrySymmetricKey(id, key),
                    Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            }
            return _client;
        }
        public static IConfigurationRoot ReadFromAppSettings()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json", optional: true)
                .Build();
        }

        static void Main(string[] args)
        {
            _appSettings = ReadFromAppSettings().Get<AppSettings>();

            CommandLine.Parser.Default.ParseArguments<DirectMethodOptions, TwinPropertyOptions>(args)
                .WithParsed<DirectMethodOptions>(opts => 
                {
                    var task = Task.Run(async () => await new DirectMethodJob(opts, _appSettings).RunDirectMethodAsync(opts));
                    task.Wait();
                })
                .WithParsed<TwinPropertyOptions>(opts => 
                {
                    var task = Task.Run(async () => await new DeviceTwinJob(opts, _appSettings).RunTwinJobAsync(opts));
                    task.Wait();
                })                        
                .WithNotParsed(errs => {
                    Logger.Error($"Error:{JsonConvert.SerializeObject(errs)}");
                });
            Console.ReadLine();
        }
    }
}
