using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Env.OperatingSystems;

[Singleton(typeof(IOperatingSystem))]
internal class Mac : IOperatingSystem
{
    public string Workspace => "/Users/Shared";
}