using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
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
public class DeviceTwinJob{
    private string _deviceId = null;
    static JobClient _jobClient;
    public DeviceTwinJob(TwinPropertyOptions opts, AppSettings appsettings){
        _deviceId = opts.Query;

        if(_jobClient == null){
            _jobClient = JobClient.CreateFromConnectionString(appsettings.IoTHubOwnerConnectionString);
        }
    }
    public async Task<string> StartTwinUpdateJobAsync(TwinPropertyOptions opts)
    {
        string jobId = Guid.NewGuid().ToString();
        Twin twin = new Twin(opts.DeviceId);
        // twin.Tags = new TwinCollection();
        // twin.Tags["Building"] = "43";
        // twin.Tags["Floor"] = "3";
        // twin.ETag = "*";

        twin.Properties.Desired["LocationUpdate"] = DateTime.UtcNow;

        JobResponse createJobResponse = _jobClient.ScheduleTwinUpdateAsync(
            jobId,
            opts.Query, 
            twin, 
            DateTime.UtcNow, 
            (long)TimeSpan.FromMinutes(2).TotalSeconds).Result;

        return jobId;
    }
    public async Task<int> RunTwinJobAsync(TwinPropertyOptions opts){
        var jobId = await StartTwinUpdateAsync(opts);

        //  TODO:
        return 0;
    } 
    private async Task<string> StartTwinUpdateAsync(TwinPropertyOptions opts)
    {
        return "";
    }
}