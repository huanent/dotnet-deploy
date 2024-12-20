using System.CommandLine;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Services;

[Singleton(typeof(IServiceCommand))]
public class ServiceInstallCommand : BaseCommand, IServiceCommand
{
    public ServiceInstallCommand()
        : base("install", "Install project remote host service")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);
        using var server = new Server(parseResult, project.Options);
        await server.InitializeAsync(token);
        var remoteAppDirectory = Path.Combine(server.RootDirectory, project.AssemblyName);
        var remoteAppFile = Path.Combine(remoteAppDirectory, project.AssemblyName);
        var serviceName = $"{project.AssemblyName}.service";
        var remoteServiceFile = Path.Combine("/etc/systemd/system", serviceName);
        var servicePath = Path.Combine(project.WorkDirectory, serviceName);

        var service = new SystemdService
        {
            Unit = new Dictionary<string, object> {
                {"Description", project.AssemblyName}
            },
            Service = new Dictionary<string, object>{
                {"WorkingDirectory", remoteAppDirectory},
                {"ExecStart", remoteAppFile},
                {"Restart", "always"},
                {"RestartSec", "10"},
                {"KillSignal", "SIGINT"},
                {"SyslogIdentifier", project.AssemblyName},
                {
                    "Environment",
                    new Dictionary<string,string>{
                        { "ASPNETCORE_ENVIRONMENT","Production"}
                    }
                },
            },
            Install = new Dictionary<string, object> {
                {"WantedBy", "multi-user.target"},
            }
        };

        service.Merge(project.Options?.Service);

        File.WriteAllText(servicePath, service.ToString());
        await server.UploadFileAsync(servicePath, remoteServiceFile, token);
        await server.ExecuteAsync($"systemctl enable {remoteServiceFile}", token);
        await server.ExecuteAsync($"systemctl start {serviceName}", token);
        Console.WriteLine($"Service {project.AssemblyName} installed!");
    }
}