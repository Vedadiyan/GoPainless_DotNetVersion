using CommandLine;
using GoPainless.FS;
using GoPainless.Options;

namespace GoPainless;

public class Program {
    public static void Main(string[] args) {
        CommandLine.Parser.Default.ParseArguments(args).WithParsed(x=> {
            if (x is Initialize initialize) {
                PackageManagementFile packageManagementFile = new PackageManagementFile(initialize.Name!, initialize.Version!);
                packageManagementFile.GenerateModFile();
            }
            else if (x is InstallPackage installPackage) {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.AddPackage(installPackage.Url!, installPackage.Name!, installPackage.Private, installPackage.Force);
                packageManagementFile.WriteAsync().Wait();
            }
            else if (x is RemovePackage removePackage) {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.DeletePackage(removePackage.Name!);
                packageManagementFile.WriteAsync().Wait();
            }
            else if (x is RestorePackages restorePackages) {
                PackageManagementFile packageManagementFile = new PackageManagementFile();
                packageManagementFile.RestorePackages();
                packageManagementFile.WriteAsync().Wait();
            }
        });
    }
}