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
        var host = parseResult.GetValue<string>(Constants.HOST_PARAMETER);
        if (string.IsNullOrWhiteSpace(host)) host = project.Options.Host;
        if (string.IsNullOrWhiteSpace(host)) throw new Exception("Host can not empty");
        var options = project.Options.Get(host);
        using var server = new Server(host, parseResult, options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var output = await server.ExecuteAsync($"systemctl status {serviceName} --no-pager -l", token);
        Console.WriteLine(output);
    }
}