using CommandLine;

namespace GoPainless.Options;

[Verb("init")]
public class Initialize
{
    [Option('N', "name", Required = true, HelpText = "The name of the module")]
    public string? Name { get; set; }
    [Option('V', "version", Required = true, HelpText = "The version of the module")]
    public string? Version { get; set; }
}