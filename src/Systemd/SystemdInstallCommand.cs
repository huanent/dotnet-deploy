using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Systemd;

[Singleton(typeof(ISystemdCommand))]
public class SystemdInstallCommand : BaseCommand, ISystemdCommand
{
    public SystemdInstallCommand()
        : base("install", "Install project systemd service on remote host")
    {
    }

    protected override async Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        using var server = new Server(options);
        await server.InitializeAsync(token);
        var serviceName = $"{project.AssemblyName}.service";
        var servicePath = Path.Combine(project.WorkDirectory, serviceName);

        var service = new SystemdService(project);
        service.Service?.Add("User", server.Username);

        File.WriteAllText(servicePath, service.ToString());
        var serviceFilePath = await server.UploadFileAsync(servicePath, serviceName, token);
        var serviceFileTargetPath = Path.Combine("/etc/systemd/system", serviceName);
        await server.ExecuteAsync($"sudo ln -s {serviceFilePath} {serviceFileTargetPath}", token);
        await server.ExecuteAsync($"sudo systemctl enable {serviceFileTargetPath}", token);
        await server.ExecuteAsync($"sudo systemctl start {serviceName}", token);
        Console.WriteLine($"Service {project.AssemblyName} installed!");
    }
}