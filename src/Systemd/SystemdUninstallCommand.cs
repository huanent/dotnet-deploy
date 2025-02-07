using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using DotnetDeploy.Systemd;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Services;

[Singleton(typeof(ISystemdCommand))]
public class SystemdUninstallCommand : BaseCommand, ISystemdCommand
{
    public SystemdUninstallCommand()
        : base("uninstall", "Uninstall project systemd service on remote host")
    {
    }

    protected override async Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        using var server = new Server(options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";

        try
        {
            await server.ExecuteAsync($"sudo systemctl stop {serviceName}", token);
        }
        catch
        {
            Console.WriteLine($"Service {project.AssemblyName} not running!");
        }

        try
        {
            await server.ExecuteAsync($"sudo systemctl disable {serviceName}", token);
        }
        catch
        {
            Console.WriteLine($"Service {project.AssemblyName} not enabled!");
        }

        var serviceFilePath = Path.Combine(Server.RootDirectory, serviceName);
        await server.Connection.SftpClient.DeleteFileAsync(serviceFilePath, token);
        Console.WriteLine($"Service {project.AssemblyName} uninstalled!");
    }
}