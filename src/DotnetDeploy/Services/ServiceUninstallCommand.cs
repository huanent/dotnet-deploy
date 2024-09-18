using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

public class ServiceUninstallCommand : BaseCommand
{
    public ServiceUninstallCommand()
        : base("uninstall", "Uninstall project remote host service")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        var project = new Project(projectPath);
        var server = new Server(parseResult, project.Options);
        await server.ConnectAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        await server.ExecuteAsync($"systemctl stop {serviceName}", token);
        await server.ExecuteAsync($"systemctl disable {serviceName}", token);
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);
        await server.SftpClient.DeleteFileAsync(remoteServiceFile, token);
        Console.WriteLine($"Service {project.AssemblyName} uninstalled!");
    }
}