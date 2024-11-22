using System.CommandLine;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Services;

[Singleton(typeof(IServiceCommand))]
public class ServiceStatusCommand : BaseCommand, IServiceCommand
{
    public ServiceStatusCommand()
        : base("status", "Get project service status")
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
        var output = await server.ExecuteAsync($"systemctl status {serviceName} --no-pager -l", token);
        Console.WriteLine(output);
    }
}