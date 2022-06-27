using CommandLine;
using GoPainless.FS;
using GoPainless.Options;

namespace GoPainless;

public class Program
{
    public static void Main(string[] args)
    {
        CommandLine.Parser.Default.ParseArguments(args, typeof(Initialize), typeof(InstallPackage), typeof(RemovePackage), typeof(RestorePackages), typeof(Tidy)).WithParsed(x =>
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
                PackageManagementFile.Clean();
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.RestorePackages(true, restorePackages.Update);
                packageManagementFile.WriteAsync().Wait();
                if (restorePackages.Tidy)
                {
                    PackageManagementFile.Tidy();
                }
            }
            else if (x is Build build)
            {
                PackageManagementFile.Build(build.Os!, build.Architecture!, build.Output!, build.Target!);
            }
            else if (x is Clean clean)
            {
                PackageManagementFile.Clean();
            }
            else if (x is Tidy tidy)
            {
                PackageManagementFile.Tidy();
            }
        });
    }
}