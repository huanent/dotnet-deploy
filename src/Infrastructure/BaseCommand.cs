using System.CommandLine;

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

        SetAction(ExecuteAsync);
    }

    protected abstract Task ExecuteAsync(ParseResult parseResult, CancellationToken token);
}