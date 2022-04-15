using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GoPainless.Models;

namespace GoPainless.FS;

public class PackageManagementFile
{
    private const string packageManagementFileName = "package.json";
    private readonly GoModule goModule;
    public PackageManagementFile(string name, string version)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(name);
        }
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException(version);
        }
        goModule = new GoModule
        {
            Name = name,
            Version = version
        };
    }
    public PackageManagementFile()
    {
        string goModuleJson = File.ReadAllText(packageManagementFileName);
        goModule = JsonSerializer.Deserialize<GoModule>(goModuleJson) ?? throw new Exception("Corrupted file");
    }
    public void GenerateModFile(string name)
    {
        if (!run("go", $"mod init {name}"))
        {
            throw new Exception("Could not create mod file");
        }
    }
    public void Create()
    {
        if (File.Exists(packageManagementFileName))
        {
            throw new Exception("File already exists");
        }
        string goModuleJson = JsonSerializer.Serialize(goModule);
        File.WriteAllText(packageManagementFileName, goModuleJson);
    }
    public void AddPackage(string uri, string name, bool @private, bool update = false, bool recursive = false)
    {
        if (!goModule.Packages.TryGetValue(name, out GoPackage? goPackage))
        {
            if (!@private)
            {
                if (getPackage(uri))
                {
                    goPackage = new GoPackage
                    {
                        Uri = uri,
                        Private = @private
                    };
                    goModule.Packages.Add(name, goPackage);
                    return;
                }

            }
            else
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string packageFolder = Path.Combine(appDataFolder, "go-painless", "packages");
                string packagePath = Path.Combine(packageFolder, name);
                if (update || !Directory.Exists(packagePath))
                {
                    if (getPrivatePackage(uri, name, recursive))
                    {
                        goPackage = new GoPackage
                        {
                            Uri = uri,
                            Private = @private
                        };
                        goModule.Packages.Add(name, goPackage);
                        return;
                    }
                }
                else
                {
                    goPackage = new GoPackage
                    {
                        Uri = uri,
                        Private = @private
                    };
                    goModule.Packages.Add(name, goPackage);
                    return;
                }
            }
        }
        else
        {
            throw new Exception("Another package with the same name already exists");
        }
    }
    public void DeletePackage(string name)
    {
        if (goModule.Packages.TryGetValue(name, out GoPackage? goPackage))
        {
            goModule.Packages.Remove(name);
        }
    }
    public void RestorePackages(bool recursive)
    {
        GenerateModFile(goModule.Name!);
        foreach (var i in goModule.Packages)
        {
            if (!i.Value.Private)
            {
                getPackage(i.Value.Uri!);

            }
            else
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string packageFolder = Path.Combine(appDataFolder, "go-painless", "packages");
                string packagePath = Path.Combine(packageFolder, i.Key);
                if (!Directory.Exists(packagePath))
                {
                    getPrivatePackage(i.Value.Uri!, i.Key, File.Exists(Path.Combine(packagePath, "package.json")));
                }
            }
        }
    }
    public async Task WriteAsync()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string packageFolder = Path.Combine(appDataFolder, "go-painless", "packages");
        string[] goModFile = await File.ReadAllLinesAsync("go.mod");
        List<string> buffer = new List<string>();
        StringBuilder output = new StringBuilder();
        foreach (var i in goModFile)
        {
            string tmp = i.TrimStart();
            if (tmp.StartsWith("replace"))
            {
                string[] split = tmp.Split("=>");
                if (split.Length != 2)
                {
                    throw new Exception("Malformed go.mod file");
                }
                buffer.Add(split[0].Split()[1]);
            }
            else
            {
                string? line = buffer.SingleOrDefault(x => i.Contains(x));
                if (line != null)
                {
                    buffer.Remove(line);
                }
                else
                {
                    output.AppendLine(i);
                }
            }
        }
        foreach (var i in goModule.Packages.Where(x => x.Value.Private == true))
        {
            output.AppendLine(@$"replace {i.Key} => ""{Path.Combine(packageFolder, i.Key).Replace("\\", "\\\\")}""");
            output.AppendLine(@$"require {i.Key} v1.0.0");
        }
        await File.WriteAllTextAsync("go.mod", output.ToString());
        await File.WriteAllTextAsync(packageManagementFileName, JsonSerializer.Serialize(goModule, new JsonSerializerOptions { WriteIndented = true }));
        run("go", "mod tidy");
    }
    private bool getPackage(string url)
    {
        return run("go", $"get {url}");
    }
    private bool getPrivatePackage(string url, string name, bool recursive = false)
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string packageFolder = Path.Combine(appDataFolder, "go-painless", "packages");
        if (!Directory.Exists(packageFolder))
        {
            Directory.CreateDirectory(packageFolder);
        }
        run("git", $"clone {url} {name}", packageFolder);
        if (recursive)
        {
            GenerateModFile(name);
            run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "go-painless", "bin", "go-painless.exe"), $"restore", Path.Combine(packageFolder, name));
        }
        run("go", "mod tidy", Path.Combine(packageFolder, name));
        return true;
    }

    private bool run(string fileName, string args, string? workingDirectory = null)
    {
        string currentDirectory = Environment.CurrentDirectory;
        ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName, args);
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        if (workingDirectory != null)
        {
            processStartInfo.WorkingDirectory = workingDirectory;

        }
        Process? process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new Exception("Process could not start");
        }
        bool hasErrors = false;
        process.EnableRaisingEvents = true;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        };
        process.WaitForExit();
        Environment.CurrentDirectory = currentDirectory;
        return !hasErrors;
    }
}