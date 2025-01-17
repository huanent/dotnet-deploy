using DotnetDeploy.Services;

namespace DotnetDeploy.Projects;

public class DeployOptions : HostDeployOptions
{
    public string? Host { get; init; }
    public Dictionary<string, HostDeployOptions>? Hosts { get; set; }
}

public class HostDeployOptions
{
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? PrivateKey { get; init; }
    public SystemdService? Service { get; set; }
}