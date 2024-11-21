using DotnetDeploy.Env;
using DotnetDeploy.Env.OperatingSystems;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;
using DotnetDeploy.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder();
hostBuilder.ConfigureServices(servers =>
{
    servers.AddSingleton<IOperatingSystem, Linux>();
    servers.AddSingleton<IOperatingSystem, Mac>();
    servers.AddSingleton<CurrentEnv>();

    servers.AddSingleton<IServiceCommand, ServiceInstallCommand>();
    servers.AddSingleton<IServiceCommand, ServiceUninstallCommand>();
    servers.AddSingleton<IServiceCommand, ServiceStatusCommand>();
    servers.AddSingleton<IServiceCommand, ServiceRestartCommand>();
    servers.AddSingleton<ICommand, ServiceCommand>();
    servers.AddSingleton<ICommand, PublishCommand>();
    servers.AddSingleton<RootCommand>();
});

var app = await hostBuilder.StartAsync();
var root = app.Services.GetRequiredService<RootCommand>();
return await root.InvokeAsync(args);