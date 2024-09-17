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
        var stopProcess = await server.SshClient.ExecuteAsync($"systemctl stop {serviceName}", cancellationToken: token);
        await stopProcess.WaitForExitAsync(token);
        var disableProcess = await server.SshClient.ExecuteAsync($"systemctl disable {serviceName}", cancellationToken: token);
        await disableProcess.WaitForExitAsync(token);
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);
        await server.SftpClient.DeleteFileAsync(remoteServiceFile, token);
    }
}