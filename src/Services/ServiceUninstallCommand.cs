using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

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
        using var server = new Server(parseResult, project.Options);
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