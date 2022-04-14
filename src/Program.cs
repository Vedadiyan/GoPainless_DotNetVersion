using GoPainless.FS;

namespace GoPainless;

public class Program {
    public static void Main(string[] args) {
        PackageManagementFile packageManagementFile = new PackageManagementFile("Test", "v1.0.0");
        packageManagementFile.AddPackage("https://github.com/go-redis/redis.git", "redis-go-local", true);
        packageManagementFile.WriteAsync().Wait();
    }
}