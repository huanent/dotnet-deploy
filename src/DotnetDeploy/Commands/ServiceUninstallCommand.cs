using System.CommandLine;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Commands;

public class ServiceUninstallCommand : CliCommand
{
    public ServiceUninstallCommand()
        : base("uninstall", "Uninstall project remote host service")
    {

        Options.Add(new CliOption<string?>("--host")
        {
            Description = "Target host name or domain",
        });

        Options.Add(new CliOption<string?>("--password")
        {
            Description = "Host password"
        });

        Options.Add(new CliOption<string?>("--project")
        {
            Description = "Project path"
        });

        SetAction(async (parseResult, token) =>
        {
            var host = parseResult.GetValue<string>("--host");
            var password = parseResult.GetValue<string>("--password");
            var project = parseResult.GetValue<string>("--project");
            await ExecuteAsync(host, password, project, token);
        });
    }

    private static async Task ExecuteAsync(string? host, string? password, string? projectPath, CancellationToken token)
    {
        var project = new Project(projectPath);

        var sshSettings = new SshClientSettings(host ?? project.Options.Host)
        {
            UserName = project.Options.UserName ?? "root",
            Credentials = [new PasswordCredential(password ?? project.Options.Password)]
        };

        using var sshClient = new SshClient(sshSettings);
        using var sftpClient = await sshClient.OpenSftpClientAsync();
        var serviceName = $"{project.AssemblyName}.service";
        var stopProcess = await sshClient.ExecuteAsync($"systemctl stop {serviceName}", cancellationToken: token);
        await stopProcess.WaitForExitAsync(token);
        var disableProcess = await sshClient.ExecuteAsync($"systemctl disable {serviceName}", cancellationToken: token);
        await disableProcess.WaitForExitAsync(token);
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);
        await sftpClient.DeleteFileAsync(remoteServiceFile, token);
    }
}