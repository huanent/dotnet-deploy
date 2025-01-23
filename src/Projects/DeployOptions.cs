using DotnetDeploy.Systemd;

namespace DotnetDeploy.Projects;

public class DeployOptions : HostDeployOptions
{
    public string? Host { get; init; }
    public Dictionary<string, HostDeployOptions>? Hosts { get; set; }

    public HostDeployOptions Get(string host)
    {
        HostDeployOptions? subHost = null;
        Hosts?.TryGetValue(host, out subHost);

        var result = new HostDeployOptions
        {
            Password = subHost?.Password ?? Password,
            PrivateKey = subHost?.PrivateKey ?? PrivateKey,
            UserName = subHost?.UserName ?? UserName,
            IncludeFiles = subHost?.IncludeFiles ?? IncludeFiles,
            Systemd = subHost?.Systemd ?? Systemd
        };

        return result;
    }
}

public class HostDeployOptions
{
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? PrivateKey { get; init; }
    public string[]? IncludeFiles { get; set; }
    public SystemdService? Systemd { get; set; }
}