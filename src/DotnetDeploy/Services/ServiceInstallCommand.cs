using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Services;

public class ServiceInstallCommand : BaseCommand
{
    public ServiceInstallCommand()
        : base("install", "Install project remote host service")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        var project = new Project(projectPath);
        var server = new Server(parseResult, project.Options);
        await server.ConnectAsync(token);
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

        await server.UploadFileAsync(servicePath, remoteServiceFile, token);
        await server.ExecuteAsync($"systemctl enable {remoteServiceFile}", token);
        await server.ExecuteAsync($"systemctl start {serviceName}", token);
        Console.WriteLine($"Service {project.AssemblyName} installed!");
    }
}