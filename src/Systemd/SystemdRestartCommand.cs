using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Systemd;

[Singleton(typeof(ISystemdCommand))]
public class SystemdRestartCommand : BaseCommand, ISystemdCommand
{
    public SystemdRestartCommand()
        : base("restart", "Restart project systemd service on remote host")
    {
    }

    protected override async Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        using var server = new Server(options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        await server.ExecuteAsync($"sudo systemctl restart {serviceName}", token);
        Console.WriteLine($"Service {project.AssemblyName} restarted!");
    }
}