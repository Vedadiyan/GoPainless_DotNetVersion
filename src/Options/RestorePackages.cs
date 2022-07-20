using CommandLine;

namespace GoPainless.Options;

[Verb("restore")]
public class RestorePackages
{
    [Option("update", Required = false, HelpText = "Updates existing package")]
    public bool Update { get; set; }
    [Option("update-global", Required = false, HelpText = "Updates global package")]
    public bool UpdateGlobal { get; set; }
    [Option("tidy", Required = false, HelpText = "Runs `go tidy` after restoring packages")]
    public bool Tidy { get; set; }
}