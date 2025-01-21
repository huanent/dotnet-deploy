using System.CommandLine;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Services;

[Singleton(typeof(IServiceCommand))]
public class ServiceUninstallCommand : BaseCommand, IServiceCommand
{
    public ServiceUninstallCommand()
        : base("uninstall", "Uninstall project remote host service")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);
        var host = parseResult.GetValue<string>(Constants.HOST_PARAMETER);
        if (string.IsNullOrWhiteSpace(host)) host = project.Options.Host;
        if (string.IsNullOrWhiteSpace(host)) throw new Exception("Host can not empty");
        var options = project.Options.Get(host);
        using var server = new Server(host, parseResult, options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);

        try
        {
            await server.ExecuteAsync($"systemctl stop {serviceName}", token);
        }
        catch
        {
            Console.WriteLine($"Service {project.AssemblyName} not running!");
        }

        try
        {
            await server.ExecuteAsync($"systemctl disable {serviceName}", token);
        }
        catch
        {
            Console.WriteLine($"Service {project.AssemblyName} not enabled!");
        }

        await server.SftpClient.DeleteFileAsync(remoteServiceFile, token);
        Console.WriteLine($"Service {project.AssemblyName} uninstalled!");
    }
}