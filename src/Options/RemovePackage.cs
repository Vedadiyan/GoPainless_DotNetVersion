using CommandLine;

namespace GoPainless.Options;

[Verb("remove")]
public class RemovePackage
{
    [Option('N', "name", Required = true, HelpText = "The name of the package to be removed")]
    public string? Name { get; set; }
}