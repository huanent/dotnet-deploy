using DotnetDeploy.Services;

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
            Service = subHost?.Service ?? Service
        };
        
        return result;
    }
}

public class HostDeployOptions
{
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? PrivateKey { get; init; }
    public SystemdService? Service { get; set; }
}