using CommandLine;
using GoPainless.FS;
using GoPainless.Options;

namespace GoPainless;

public class Program
{
    public static void Main(string[] args)
    {
        CommandLine.Parser.Default.ParseArguments(args, typeof(Initialize), typeof(InstallPackage), typeof(RemovePackage), typeof(RestorePackages)).WithParsed(x =>
        {
            if (x is Initialize initialize)
            {
                PackageManagementFile packageManagementFile = new PackageManagementFile(initialize.Name!, initialize.Version!);
                packageManagementFile.GenerateModFile(initialize.Name!);
                packageManagementFile.Create();
            }
            else if (x is InstallPackage installPackage)
            {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.AddPackage(installPackage.Url!, installPackage.Name!, installPackage.Private, installPackage.Force, installPackage.Recursive);
                packageManagementFile.WriteAsync().Wait();
            }
            else if (x is RemovePackage removePackage)
            {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.DeletePackage(removePackage.Name!);
                packageManagementFile.WriteAsync().Wait();
            }
            else if (x is RestorePackages restorePackages)
            {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.RestorePackages(true);
                packageManagementFile.WriteAsync().Wait();
            }
        });
    }
}