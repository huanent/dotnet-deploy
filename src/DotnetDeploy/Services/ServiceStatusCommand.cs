using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

public class ServiceStatusCommand : BaseCommand
{
    public ServiceStatusCommand()
        : base("status", "Get project service status")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        var project = new Project(projectPath);
        var server = new Server(parseResult, project.Options);
        await server.ConnectAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var output = await server.ExecuteAsync($"systemctl status {serviceName} --no-pager -l", token);
        Console.WriteLine(output);
    }
}