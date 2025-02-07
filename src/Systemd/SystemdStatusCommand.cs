using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Systemd;

[Singleton(typeof(ISystemdCommand))]
public class SystemdStatusCommand : BaseCommand, ISystemdCommand
{
    public SystemdStatusCommand()
        : base("status", "Get project systemd service status on remote host")
    {
    }

    protected override async Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        using var server = new Server(options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var output = await server.ExecuteAsync($"sudo systemctl status {serviceName} --no-pager -l", token);
        Console.WriteLine(output);
    }
}