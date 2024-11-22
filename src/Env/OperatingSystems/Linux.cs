using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Env.OperatingSystems;

[Singleton(typeof(IOperatingSystem))]
internal class Linux : IOperatingSystem
{
    public string Workspace => "/var";
}