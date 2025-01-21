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
        assemblyName = ProcessHelper.RunCommandAsync(
            "dotnet",
            ["msbuild", CsprojFile, "-getProperty:AssemblyName"],
            token
        ).Result;

        await BindOptionsAsync(token);
    }

    private async Task BindOptionsAsync(CancellationToken token)
    {
        var userSecretId = await ProcessHelper.RunCommandAsync(
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
        BindEnvironment(deploy, options.Service);

        if (options.Hosts != null)
        {
            foreach (var item in options.Hosts)
            {
                var hostSection= deploy.GetSection($"Hosts:{item.Key}");
                BindEnvironment(hostSection, item.Value.Service);
            }
        }
    }

    private void BindEnvironment(IConfigurationSection section, Services.SystemdService? systemdService)
    {
        var environment = section.GetSection("Service:Service:Environment").Get<Dictionary<string, string>>();
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