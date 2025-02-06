using System.CommandLine;
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