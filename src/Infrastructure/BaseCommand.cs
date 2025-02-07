using System.CommandLine;
using DotnetDeploy.Projects;

namespace DotnetDeploy.Infrastructure;

public abstract class BaseCommand : CliCommand
{
    public BaseCommand(string name, string description) : base(name, description)
    {
        Options.Add(new CliOption<string?>(Constants.HOST_PARAMETER)
        {
            Description = "Target host name or domain",
        });

        Options.Add(new CliOption<string?>(Constants.USERNAME_PARAMETER)
        {
            Description = "SSH username"
        });

        Options.Add(new CliOption<string?>(Constants.PASSWORD_PARAMETER)
        {
            Description = "SSH password"
        });

        Options.Add(new CliOption<string?>(Constants.PRIVATE_KEY_PARAMETER)
        {
            Description = "SSH private key"
        });

        Options.Add(new CliOption<string?>(Constants.PROJECT_PARAMETER)
        {
            Description = "Project path",
        });

        Options.Add(new CliOption<bool?>(Constants.ALL_HOSTS_PARAMETER)
        {
            Description = "Execute command for all hosts",
        });

        SetAction(ExecuteAsync);
    }

    private async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);

        var defaultHost = parseResult.GetValue<string>(Constants.HOST_PARAMETER) ?? project.Options.Host;
        if (string.IsNullOrWhiteSpace(defaultHost)) throw new Exception("Host parameter can not be empty");
        var defaultOptions = project.Options.Get(defaultHost, parseResult);
        await ExecuteAsync(defaultOptions, project, token);

        if ((parseResult.GetValue<bool?>(Constants.ALL_HOSTS_PARAMETER) ?? false) && project.Options.Hosts != null)
        {
            foreach (var item in project.Options.Hosts)
            {
                var options = project.Options.Get(item.Key, parseResult);
                if (options.Host == defaultOptions.Host) continue;
                await ExecuteAsync(options, project, token);
            }
        }
    }

    protected abstract Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token);
}