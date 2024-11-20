using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

public class ServiceRestartCommand : BaseCommand, IServiceCommand
{
    public ServiceRestartCommand()
        : base("restart", "Restart project service on remote host")
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
        await server.ExecuteAsync($"systemctl restart {serviceName}", token);
        Console.WriteLine($"Service {project.AssemblyName} restarted!");
    }
}