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
public class JobMonitor{
    public static async Task MonitorAsync(AppSettings appsettings, string jobId){
        JobResponse result;
        var jobClient = JobClient.CreateFromConnectionString(appsettings.IoTHubOwnerConnectionString);

        do
        {
            result = await jobClient.GetJobAsync(jobId);
            Logger.Info($"[{jobId}]Job Status : " + result.Status.ToString());
            Thread.Sleep(2000);
        }
        while ((result.Status != JobStatus.Completed) && 
                (result.Status != JobStatus.Failed));
        }
}