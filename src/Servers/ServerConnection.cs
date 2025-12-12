using Tmds.Ssh;

namespace DotnetDeploy.Servers;

public class ServerConnection(string host, string username, List<Credential> credentials) : IDisposable
{
    private SshClient? sshClient;
    private SftpClient? sftpClient;

    public SshClient SshClient => sshClient ?? throw new Exception("Server not connected");
    public SftpClient SftpClient => sftpClient ?? throw new Exception("Server not connected");

    public async Task ConnectAsync(CancellationToken token)
    {
        var sshSettings = new SshClientSettings(host)
        {
            UserName = username!,
            Credentials = credentials,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            AutoConnect = false,
            UserKnownHostsFilePaths = [],
            UpdateKnownHostsFileAfterAuthentication = true,
            HostAuthentication = (context, token) => ValueTask.FromResult(true),
        };

        sshClient = new SshClient(sshSettings);
        await SshClient.ConnectAsync(token);
        sftpClient = await SshClient.OpenSftpClientAsync(token);
    }

    public void Dispose()
    {
        sshClient?.Dispose();
        sftpClient?.Dispose();
    }
}