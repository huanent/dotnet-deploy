using System.CommandLine;
using DotnetDeploy.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Systemd;

[Singleton(typeof(ICommand))]
public class SystemdCommand : CliCommand, ICommand
{
    public SystemdCommand(IEnumerable<ISystemdCommand> commands) : base(
        "systemd", "Manager project remote host systemd service")
    {
        foreach (var command in commands)
        {
            Add((CliCommand)command);
        }
    }
}