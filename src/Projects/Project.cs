using DotnetDeploy.Infrastructure;
using DotnetDeploy.Systemd;
using Microsoft.Extensions.Configuration;

namespace DotnetDeploy.Projects;

public class Project : IDisposable
{
    private readonly DeployOptions options = new();
    private string? assemblyName;

    public DeployOptions Options => options;
    public string CsprojFile { get; init; }
    public string RootDirectory { get; init; }
    public string WorkDirectory { get; init; }
    public string AssemblyName => assemblyName ?? throw new Exception("Project not initialized");

    public Project(string? path)
    {
        CsprojFile = DiscoverProjectFile(path);
        RootDirectory = Path.GetDirectoryName(CsprojFile)!;
        WorkDirectory = Path.Combine(RootDirectory, "bin", "__dotnet_deploy_temp__");
        if (!Directory.Exists(WorkDirectory)) Directory.CreateDirectory(WorkDirectory);
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        assemblyName = await Executor.RunAsync(
            "dotnet",
            ["msbuild", CsprojFile, "-getProperty:AssemblyName"],
             token
        );

        await BindOptionsAsync(token);
    }

    private async Task BindOptionsAsync(CancellationToken token)
    {
        var userSecretId = await Executor.RunAsync(
            "dotnet",
            ["msbuild", CsprojFile, "-getProperty:UserSecretsId"],
            token
        );

        var builder = new ConfigurationBuilder();
        builder.SetBasePath(RootDirectory);
        builder.AddJsonFile("appsettings.json", true, true);
        builder.AddJsonFile("appsettings.deploy.json", true, true);
        if (!string.IsNullOrWhiteSpace(userSecretId)) builder.AddUserSecrets(userSecretId, true);
        var configurationRoot = builder.Build();
        var deploy = configurationRoot.GetSection("Deploy");
        deploy.Bind(options);
        BindEnvironment(deploy, options.Systemd);

        if (options.Hosts != null)
        {
            foreach (var item in options.Hosts)
            {
                var hostSection = deploy.GetSection($"Hosts:{item.Key}");
                BindEnvironment(hostSection, item.Value.Systemd);
            }
        }
    }

    private static void BindEnvironment(IConfigurationSection section, SystemdService? systemdService)
    {
        var environment = section.GetSection("Systemd:Service:Environment").Get<Dictionary<string, string>>();
        if (environment != null && systemdService != null && systemdService.TryGetValue("Service", out var service))
        {
            if (service != null) service["Environment"] = environment;
        }
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

        if (System.IO.Directory.Exists(path))
        {
            var files = System.IO.Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);

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

    public void Dispose()
    {
        if (Directory.Exists(WorkDirectory))
        {
            Directory.Delete(WorkDirectory, true);
        }
    }
}