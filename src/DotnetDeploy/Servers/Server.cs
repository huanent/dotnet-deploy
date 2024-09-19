using System.CommandLine;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Servers;

internal class Server : IDisposable
{
    private SshClient? sshClient;
    private SftpClient? sftpClient;
    private readonly string? host;
    internal readonly string? username;
    internal string? password;
    internal string? privateKey;
    internal string RootDirectory => "/var/dotnet-apps";
    internal SshClient SshClient => sshClient ?? throw new Exception("Server not Initialized");
    internal SftpClient SftpClient => sftpClient ?? throw new Exception("Server not Initialized");

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

        privateKey = parseResult.GetValue<string>(Constants.PRIVATE_KEY_PARAMETER);
        if (string.IsNullOrWhiteSpace(password))
        {
            privateKey = options.PrivateKey;
        }
    }

    internal async Task InitializeAsync(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

        var credentials = new List<Credential>();

        if (!string.IsNullOrWhiteSpace(privateKey))
        {
            credentials.Add(new PrivateKeyCredential(privateKey, password));
        }
        else if (!string.IsNullOrWhiteSpace(password))
        {
            credentials.Add(new PasswordCredential(password));
        }

        var sshSettings = new SshClientSettings(host)
        {
            UserName = username!,
            Credentials = credentials,
            AutoConnect = false
        };

        Console.WriteLine($"Connecting server {host} with {username}");
        sshClient = new SshClient(sshSettings);
        await SshClient.ConnectAsync(token);
        Console.WriteLine($"Server {host} connected!");
        sftpClient = await SshClient.OpenSftpClientAsync(token);
    }

    public async Task UploadFileAsync(string localPath, string remotePath, CancellationToken token)
    {
        var folder = Path.GetDirectoryName(remotePath);
        var folderExist = await ExistFolderAsync(folder, token);

        if (!folderExist)
        {
            await SftpClient.CreateDirectoryAsync(folder!, true, cancellationToken: token);
        }

        var fileExist = await ExistFileAsync(remotePath, token);

        if (fileExist)
        {
            await SftpClient.DeleteFileAsync(remotePath, token);
        }

        await SftpClient.UploadFileAsync(localPath, remotePath, token);
    }

    public async Task<string> ExecuteAsync(string command, CancellationToken token)
    {
        var process = await SshClient.ExecuteAsync(command, cancellationToken: token);
        var (output, error) = await process.ReadToEndAsStringAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(error);
        }

        return output;
    }

    public async Task<bool> ExistFileAsync(string path, CancellationToken token)
    {
        try
        {
            await ExecuteAsync($"test -f {path}", token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistFolderAsync(string path, CancellationToken token)
    {
        try
        {
            await ExecuteAsync($"test -d {path}", token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        SshClient?.Dispose();
    }
}