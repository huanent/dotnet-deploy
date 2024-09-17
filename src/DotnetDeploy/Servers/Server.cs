using System.CommandLine;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Servers;

internal class Server : IDisposable
{
    private readonly string? host;
    internal readonly string? username;
    internal string? password;
    internal SshClient? SshClient { get; set; }
    internal SftpClient? SftpClient { get; set; }

    public Server(ParseResult parseResult, DeployOptions options)
    {
        host = parseResult.GetValue<string>(Constants.HOST_PARAMETER);
        if (string.IsNullOrWhiteSpace(host))
        {
            host = options.Host;
        }

        username = parseResult.GetValue<string>(Constants.USERNAME_PARAMETER);
        if (string.IsNullOrWhiteSpace(username))
        {
            username = options.UserName;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            username = "root";
        }

        password = parseResult.GetValue<string>(Constants.PASSWORD_PARAMETER);
        if (string.IsNullOrWhiteSpace(password))
        {
            password = options.Password;
        }
    }

    internal async Task ConnectAsync(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

        var sshSettings = new SshClientSettings(host)
        {
            UserName = username!,
            Credentials = [new PasswordCredential(password)],
            AutoConnect = false
        };

        SshClient = new SshClient(sshSettings);
        await SshClient.ConnectAsync(token);
        SftpClient = await SshClient.OpenSftpClientAsync(token);
    }

    public void Dispose()
    {
        SshClient?.Dispose();
    }
}