using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

public class ServiceRestartCommand : BaseCommand
{
    public ServiceRestartCommand()
        : base("restart", "Restart project service on remote host")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        var project = new Project(projectPath);
        var server = new Server(parseResult, project.Options);
        await server.ConnectAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var restartProcess = await server.SshClient.ExecuteAsync($"systemctl restart {serviceName}", cancellationToken: token);
        await restartProcess.WaitForExitAsync(token);
    }
}