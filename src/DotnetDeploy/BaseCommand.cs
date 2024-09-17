using System.CommandLine;

namespace DotnetDeploy;

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
            Description = "Host username"
        });

        Options.Add(new CliOption<string?>(Constants.PASSWORD_PARAMETER)
        {
            Description = "Host password"
        });

        Options.Add(new CliOption<string?>(Constants.PROJECT_PARAMETER)
        {
            Description = "Project path"
        });

        SetAction(ExecuteAsync);
    }

    protected abstract Task ExecuteAsync(ParseResult parseResult, CancellationToken token);
}