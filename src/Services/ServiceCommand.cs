using System.CommandLine;

namespace DotnetDeploy.Servers;

public class ServiceCommand : CliCommand, ICommand
{
    public ServiceCommand(IEnumerable<IServiceCommand> commands) : base(
        "service", "Manager project remote host service")
    {
        foreach (var command in commands)
        {
            Add((CliCommand)command);
        }
    }
}