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
    public void GenerateModFile()
    {
        if (!run("go", $"mod init {goModule.Name}"))
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
    public void AddPackage(string uri, string name, bool @private, bool update = false)
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
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string packageFolder = Path.Combine(appDataFolder, ".go.painless");
                string packagePath = Path.Combine(packageFolder, name);
                if (update || !Directory.Exists(packagePath))
                {
                    if (getPrivatePackage(uri, name))
                    {
                        goPackage = new GoPackage
                        {
                            Uri = packagePath,
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
                        Uri = packagePath,
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
    public void RestorePackages()
    {
        GenerateModFile();
        foreach (var i in goModule.Packages)
        {
            if (!i.Value.Private)
            {
                getPackage(i.Value.Uri!);

            }
            else
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string packageFolder = Path.Combine(appDataFolder, ".go.painless");
                string packagePath = Path.Combine(packageFolder, i.Key);
                if (!Directory.Exists(packagePath))
                {
                    getPrivatePackage(i.Value.Uri!, i.Key);
                }
            }
        }
    }
    public async Task WriteAsync()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string packageFolder = Path.Combine(appDataFolder, ".go.painless");
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
    }
    private bool getPackage(string url)
    {
        return run("go", $"get {url}");
    }
    private bool getPrivatePackage(string url, string name)
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string packageFolder = Path.Combine(appDataFolder, ".go.painless\\");
        if (!Directory.Exists(packageFolder))
        {
            Directory.CreateDirectory(packageFolder);
        }
        return run("git", $"clone {url} {name}", packageFolder);
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