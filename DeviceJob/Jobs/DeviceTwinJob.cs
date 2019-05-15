using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
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
public class DeviceTwinJob{
    private string _deviceId = null;
    static JobClient _jobClient;
    AppSettings _appsettings = null;
    public DeviceTwinJob(TwinPropertyOptions opts, AppSettings appsettings){
        _deviceId = opts.Query;
        _appsettings = appsettings;
        if(_jobClient == null){
            _jobClient = JobClient.CreateFromConnectionString(appsettings.IoTHubOwnerConnectionString);
        }
    }
    public string StartTwinUpdateJob(TwinPropertyOptions opts)
    {
        string jobId = Guid.NewGuid().ToString();
        Twin twin = new Twin();
        twin.Tags = new TwinCollection();

        if(!string.IsNullOrEmpty(opts.Values)){
            TwinProperty [] properties = JsonConvert.DeserializeObject<TwinProperty []>(opts.Values);

            switch(opts.PropertyType){
                    case "Tags":
                        foreach(var property in properties){
                            Logger.Info($"Setting Tags:{property.Name}={property.Value}");
                            twin.Tags[property.Name] = property.Value;
                        }
                        break;
                    case "DesiredProperty":
                        foreach(var property in properties){
                            Logger.Info($"Setting DesiredProperty:{property.Name}={property.Value}");
                            twin.Properties.Desired[property.Name] = property.Value;
                        }
                        break;
                    default:
                        Logger.Error($"Unknown option:{opts.PropertyType}");
                        break;
                }
            
        }

        JobResponse createJobResponse = _jobClient.ScheduleTwinUpdateAsync(
            jobId,
            opts.Query, 
            twin, 
            DateTime.UtcNow, 
            (long)TimeSpan.FromMinutes(2).TotalSeconds).Result;

        return jobId;
    }
    public async Task<int> RunTwinJobAsync(TwinPropertyOptions opts){
        var jobId = StartTwinUpdateJob(opts);

        //  TODO:
        await JobMonitor.MonitorAsync(_appsettings, jobId);
        return 0;
    } 

}