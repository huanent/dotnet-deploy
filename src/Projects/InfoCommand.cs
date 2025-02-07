using System.CommandLine;
using System.Text.Json;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Systemd;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Projects;

[Singleton(typeof(ICommand))]
public class InfoCommand : BaseCommand, ICommand
{
    public InfoCommand()
        : base("info", "Show Current deploy task info")
    {
        Options.Add(new CliOption<string[]?>(Constants.INCLUDE_FILES_PARAMETER)
        {
            Description = "Copy the specified project file or directory to output directory",
        });
    }

    protected override Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        Console.WriteLine(project.AssemblyName);
        Console.WriteLine(project.RootDirectory);
        Console.WriteLine(project.WorkDirectory);
        Console.WriteLine(project.CsprojFile);
        Console.WriteLine(options.Host);

        var optionsJson = JsonSerializer.Serialize(options, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(optionsJson);

        var systemdService = new SystemdService(project);
        Console.WriteLine(systemdService.ToString());
        return Task.CompletedTask;
    }
}