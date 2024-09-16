using System.CommandLine;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Commands;

public class ServiceInstallCommand : CliCommand
{
    public ServiceInstallCommand()
        : base("install", "Install project remote host service")
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
        var remoteRoot = "/var/dotnet-apps";
        var remoteAppFolder = Path.Combine(remoteRoot, project.AssemblyName);
        var remoteAppFile = Path.Combine(remoteAppFolder, project.AssemblyName);
        var serviceName = $"{project.AssemblyName}.service";
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);
        var servicePath = Path.Combine(project.Folder, "bin", serviceName);
        File.WriteAllText(servicePath, $"""
        [Unit]
        Description={project.AssemblyName}

        [Service]
        WorkingDirectory={remoteAppFolder}
        ExecStart={remoteAppFile}
        Restart=always
        RestartSec=10
        KillSignal=SIGINT
        SyslogIdentifier={project.AssemblyName}
        Environment=ASPNETCORE_ENVIRONMENT=Production

        [Install]
        WantedBy=multi-user.target
        """);

        await sftpClient.UploadFileAsync(servicePath, remoteServiceFile, true, cancellationToken: token);
        var enableProcess = await sshClient.ExecuteAsync($"systemctl enable {remoteServiceFile}", cancellationToken: token);
        await enableProcess.WaitForExitAsync(token);
        var startProcess = await sshClient.ExecuteAsync($"systemctl start {serviceName}", cancellationToken: token);
        await startProcess.WaitForExitAsync(token);
    }
}