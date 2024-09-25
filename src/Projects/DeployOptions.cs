using DotnetDeploy.Services;

namespace DotnetDeploy.Projects;

public class DeployOptions
{
    public string? Host { get; init; }
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? PrivateKey { get; init; }
    public SystemdService? Service { get; set; }
}