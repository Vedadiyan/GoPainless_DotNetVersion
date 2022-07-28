using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GoPainless.Models;

namespace GoPainless.FS;

public enum OperatingSystems
{
    LINUX,
    WINDOWS,
    MAC
}

public class PackageManagementFile
{
    private const string packageManagementFileName = "package.json";
    private readonly GoModule goModule;
    private static readonly OperatingSystems os;
    private static readonly string goPainlessFileName;
    static PackageManagementFile()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            os = OperatingSystems.LINUX;
            goPainlessFileName = "go-painless";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            os = OperatingSystems.WINDOWS;
            goPainlessFileName = "go-painless.exe";
        }
        else { 
            os = OperatingSystems.MAC;
            goPainlessFileName = "go-painless.dmg";
        }
    }
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
                bool dirExists = Directory.Exists(packagePath);
                if (update && dirExists)
                {
                    Console.WriteLine("Deleteing {0}", packagePath);
                    delete(packagePath);
                    dirExists = false;
                }
                if (!dirExists)
                {
                    if (getPrivatePackage(uri, name, recursive))
                    {
                        goPackage = new GoPackage
                        {
                            Uri = uri,
                            Private = @private
                        };
                        goModule.Packages.Add(name, goPackage);
                        run("go", "mod tidy", packagePath);
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
    public void RestorePackages(bool recursive, bool update = false)
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
                bool dirExists = Directory.Exists(packagePath);
                if (update && dirExists)
                {
                    Console.WriteLine("Deleteing {0}", packagePath);
                    delete(packagePath);
                    dirExists = false;
                }
                if (!dirExists)
                {

                    getPrivatePackage(i.Value.Uri!, i.Key);
                    if (File.Exists(Path.Combine(packagePath, "package.json")))
                    {
                        GenerateModFile(i.Key);
                        run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "go-painless", "bin", goPainlessFileName), $"restore", Path.Combine(packageFolder, i.Key));
                    }
                }
                run("go", "mod tidy", packagePath);
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
    }
    public static void Tidy()
    {
        run("go", "mod tidy");
    }
    public static void Build(string goos, string goarch, string output, string target)
    {
        Environment.SetEnvironmentVariable("GOOS", goos);
        Environment.SetEnvironmentVariable("GOARCH", goarch);
        run("go", $"build -o {output} {target}");
    }
    public static void Clean()
    {
        if (File.Exists("go.mod"))
        {
            File.Delete("go.mod");
        }
        if (File.Exists("go.sum"))
        {
            File.Delete("go.sum");
        }
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
            run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "go-painless", "bin", goPainlessFileName), $"restore", Path.Combine(packageFolder, name));
        }
        return true;
    }

    private static bool run(string fileName, string args, string? workingDirectory = null)
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
    private void delete(string path, string basePath = "")
    {
        string _path = Path.Combine(basePath, path);
        string[] files = Directory.GetFiles(_path);
        foreach (var file in files)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(_path, file));
            fileInfo.Attributes = FileAttributes.Normal;
            File.Delete(file);
        }
        string[] directories = Directory.GetDirectories(_path);
        foreach (var directory in directories)
        {
            delete(directory, _path);
        }
        Directory.Delete(_path);
    }
}