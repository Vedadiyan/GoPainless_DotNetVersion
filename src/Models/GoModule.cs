namespace GoPainless.Models;

public class GoModule
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public Dictionary<string, GoPackage> Packages {get; set;}
    public GoModule() {
        Packages = new Dictionary<string, GoPackage>();
    }
}