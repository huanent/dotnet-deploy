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

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);
        var host = parseResult.GetValue<string>(Constants.HOST_PARAMETER);
        if (string.IsNullOrWhiteSpace(host)) host = project.Options.Host;
        if (string.IsNullOrWhiteSpace(host)) throw new Exception("Host can not empty");
        var options = project.Options.Get(host);

        Console.WriteLine(project.AssemblyName);
        Console.WriteLine(project.RootDirectory);
        Console.WriteLine(project.WorkDirectory);
        Console.WriteLine(project.CsprojFile);
        Console.WriteLine(host);

        var optionsJson = JsonSerializer.Serialize(options, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(optionsJson);

        var systemdService = new SystemdService(project);
        Console.WriteLine(systemdService.ToString());
    }
}