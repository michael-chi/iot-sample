using System.Threading.Tasks;
using CommandLine;
[Verb("twin", HelpText = "Update Device Twin.")]
public class TwinPropertyOptions
{
    [Option( "type", Required = true, HelpText = "PropertyType, available options: DesiredProperty, Tags")]
    public string PropertyType { get; set; }

    [Option('q', "query", Required = false, HelpText = "Query, query devices that match this query")]
    public string Query { get; set; }

    [Option('t', "timeout", Required = false, HelpText = "Timeout in Seconds, Job creation will be expired if exceed this period of time.")]
    public int Timeout { get; set; } = 5;

    [Option('s', "values", Required = true, HelpText = "New Property Name/Value pairs")]
    public string Values { get; set; }      
           
}