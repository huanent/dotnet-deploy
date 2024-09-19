using Microsoft.Extensions.Configuration;

namespace DotnetDeploy.Projects;

internal class Project
{
    public string CsprojFile { get; init; }
    public string Folder { get; init; }
    public string AssemblyName { get; init; }

    private readonly DeployOptions options = new();

    public DeployOptions Options => options;

    public Project(string? path)
    {
        CsprojFile = DiscoverProjectFile(path);
        Folder = Path.GetDirectoryName(CsprojFile)!;

        AssemblyName = ProcessHelper.RunCommandAsync(
           "dotnet",
           ["msbuild", CsprojFile, "-getProperty:AssemblyName"],
           default
       ).Result;

        var userSecretId = ProcessHelper.RunCommandAsync(
            "dotnet",
            ["msbuild", CsprojFile, "-getProperty:UserSecretsId"],
            default
        ).Result;

        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Folder);
        builder.AddJsonFile("appsettings.json", true, true);
        if (!string.IsNullOrWhiteSpace(userSecretId)) builder.AddUserSecrets(userSecretId, true);
        var configurationRoot = builder.Build();
        configurationRoot.GetSection("deploy").Bind(options);
    }

    private static string DiscoverProjectFile(string? path)
    {
        path ??= Environment.CurrentDirectory;

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path, Environment.CurrentDirectory);
        }

        if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                throw new Exception("Project folder not csproj file");
            }

            if (files.Length > 1)
            {
                throw new Exception("Project folder contains more that one csproj file");

            }

            return files.First();
        }

        throw new Exception("csproj file not found");
    }

}