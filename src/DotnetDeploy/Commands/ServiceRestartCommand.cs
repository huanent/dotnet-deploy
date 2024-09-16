using System.CommandLine;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Commands;

public class ServiceRestartCommand : CliCommand
{
    public ServiceRestartCommand()
        : base("restart", "Restart project service on remote host")
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
        var serviceName = $"{project.AssemblyName}.service";
        var restartProcess = await sshClient.ExecuteAsync($"systemctl restart {serviceName}", cancellationToken: token);
        await restartProcess.WaitForExitAsync(token);
    }
}