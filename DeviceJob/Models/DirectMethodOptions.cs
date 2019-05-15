using System.Threading.Tasks;
using CommandLine;
[Verb("directmethod", HelpText = "Call Direct Method.")]
public class DirectMethodOptions
{
    // [Option('j', "jobtype", Required = true, HelpText = "Job Type, available options: DirectMethod, DesiredProperty, Tags")]
    // public string JobType { get; set; }

    [Option('q', "query", Required = true, HelpText = "Query, query devices that match this query")]
    public string Query { get; set; }

    [Option('t', "timeout", Required = false, HelpText = "Timeout in Seconds, Job creation will be expired if exceed this period of time.")]
    public int Timeout { get; set; } = 5;

    [Option('m', "method", Required = true, HelpText = "Direct Method name")]
    public string Method { get; set; } 

    [Option('b', "payload", Required = false, HelpText = "Direct Method payload")]
    public string Payload { get; set; }         
}