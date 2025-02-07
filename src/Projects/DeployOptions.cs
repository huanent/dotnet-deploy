using System.CommandLine;
using System.Reflection.Metadata;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Systemd;

namespace DotnetDeploy.Projects;

public class DeployOptions : HostDeployOptions
{
    public Dictionary<string, HostDeployOptions>? Hosts { get; set; }

    public HostDeployOptions Get(string host, ParseResult parseResult)
    {
        HostDeployOptions? subHost = null;

        if (Hosts != null)
        {
            subHost = Hosts?.FirstOrDefault(f => f.Value?.Host == host || f.Key == host).Value;
        }

        var result = new HostDeployOptions
        {
            Host = subHost?.Host ?? Host,
            Password = parseResult.GetValue<string>(Constants.PASSWORD_PARAMETER) ?? subHost?.Password ?? Password,
            PrivateKey = parseResult.GetValue<string>(Constants.PRIVATE_KEY_PARAMETER) ?? subHost?.PrivateKey ?? PrivateKey,
            UserName = parseResult.GetValue<string>(Constants.USERNAME_PARAMETER) ?? subHost?.UserName ?? UserName ?? "root",
            IncludeFiles = parseResult.GetValue<string[]?>(Constants.INCLUDE_FILES_PARAMETER) ?? subHost?.IncludeFiles ?? IncludeFiles,
            BeforeCommand = parseResult.GetValue<string>(Constants.BEFORE_COMMAND_PARAMETER) ?? subHost?.BeforeCommand ?? BeforeCommand,
            AfterCommand = parseResult.GetValue<string>(Constants.AFTER_COMMAND_PARAMETER) ?? subHost?.AfterCommand ?? AfterCommand,
            Systemd = subHost?.Systemd ?? Systemd
        };

        return result;
    }
}

public class HostDeployOptions
{
    public string? Host { get; init; }
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? PrivateKey { get; init; }
    public string[]? IncludeFiles { get; set; }
    public string? BeforeCommand { get; set; }
    public string? AfterCommand { get; set; }
    public SystemdService? Systemd { get; set; }
}