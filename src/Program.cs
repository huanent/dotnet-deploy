using System.Reflection;
using DotnetDeploy.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder();
hostBuilder.ConfigureServices(servers =>
{
    servers.AddFromAssemblies(Assembly.GetExecutingAssembly());
});

var app = await hostBuilder.StartAsync();
var root = app.Services.GetRequiredService<RootCommand>();
return await root.InvokeAsync(args);