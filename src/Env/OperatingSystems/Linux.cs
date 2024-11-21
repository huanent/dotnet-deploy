namespace DotnetDeploy.Env.OperatingSystems;

internal class Linux : IOperatingSystem
{
    public string Workspace => "/var";
}