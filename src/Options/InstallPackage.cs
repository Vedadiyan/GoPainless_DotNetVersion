using CommandLine;

namespace GoPainless.Options;

[Verb("install")]
public class InstallPackage
{
    [Option('U', "url", Required = true, HelpText = "The URL to the git repository")]
    public string? Url { get; set; }
    [Option('N', "name", Required = true, HelpText = "A friendly name for version tracking")]
    public string? Name { get; set; }
    [Option('P', "private", Required = false, HelpText = "Specifies whether the provided URL points to a private repository")]
    public bool Private { get; set; }
    [Option("force", Required = false, HelpText = "Force updating existing package")]
    public bool Force { get; set; }
}