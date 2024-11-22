using System.CommandLine;
using DotnetDeploy.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Servers;

[Singleton(typeof(ICommand))]
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