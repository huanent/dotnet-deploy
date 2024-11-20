using System.CommandLine;

namespace DotnetDeploy.Commands;

internal class RootCommand(IEnumerable<ICommand> commands)
{
    public async Task<int> InvokeAsync(string[] args)
    {
        var rootCommand = new CliRootCommand("A dotnet application deploy tools");
        foreach (var command in commands)
        {
            rootCommand.Add((CliCommand)command);
        }
        return await rootCommand.Parse(args).InvokeAsync();
    }
}