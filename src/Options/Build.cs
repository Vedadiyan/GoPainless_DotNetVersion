using CommandLine;

namespace GoPainless.Options;

[Verb("build")]
public class Build
{
    [Option('R', "os", Required = true, HelpText = "Specifies the target operating system")]
    public string? Os { get; set; }
    [Option('A', "arch", Required = true, HelpText = "Sepcifies the target architecture")]
    public string? Architecture { get; set; }
    [Option('O', "output", Required = true, HelpText = "Specifies the output")]
    public string? Output { get; set; }
    [Option('T', "target", Required = true, HelpText = "Specifies the entrypoint")]
    public string? Target { get; set; }
}