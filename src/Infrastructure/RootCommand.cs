using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Infrastructure;

[Singleton]
internal class RootCommand(IEnumerable<ICommand> commands)
{
    public async Task<int> InvokeAsync(string[] args)
    {
        var rootCommand = new System.CommandLine.RootCommand("A dotnet application deploy tools");

        foreach (var command in commands)
        {
            rootCommand.Add((System.CommandLine.Command)command);
        }
        return await rootCommand.Parse(args).InvokeAsync();
    }
}