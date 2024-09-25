using System.CommandLine;
using DotnetDeploy.Projects;
using DotnetDeploy.Services;

var serviceCommand = new CliCommand("service", "Manager project remote host service");
serviceCommand.Subcommands.Add(new ServiceInstallCommand());
serviceCommand.Subcommands.Add(new ServiceUninstallCommand());
serviceCommand.Subcommands.Add(new ServiceRestartCommand());
serviceCommand.Subcommands.Add(new ServiceStatusCommand());

var rootCommand = new CliRootCommand("A dotnet application deploy tools")
{
    new PublishCommand(),
    serviceCommand
};

return await rootCommand.Parse(args).InvokeAsync();
