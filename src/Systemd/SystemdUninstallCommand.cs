using System.CommandLine;
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