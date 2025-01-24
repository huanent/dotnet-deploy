using System.CommandLine;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Servers;

public class Server : IDisposable
{
    private string? arch;
    private readonly string? host;
    public readonly string? username;
    private readonly string? password;
    private readonly string? privateKey;
    private ServerConnection connection;
    public static string RootDirectory => "/var/dotnet-apps";
    public ServerConnection Connection => connection ?? throw new Exception("Server not Initialized");
    public string Arch => arch ?? throw new Exception("Server not Initialized");

    public Server(string host, ParseResult parseResult, HostDeployOptions options)
    {
        this.host = host;

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
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            privateKey = options.PrivateKey;
        }
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        arch = await ExecuteAsync("arch", token);
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

        Console.WriteLine($"Connecting server {host} with {username}");
        connection = new ServerConnection(host, username ?? "root", credentials);
        await connection.ConnectAsync(token);
        Console.WriteLine($"Server {host} connected!");
    }

    public async Task UploadFileAsync(string localPath, string remotePath, CancellationToken token)
    {
        var folder = Path.GetDirectoryName(remotePath);
        var folderExist = await ExistFolderAsync(folder, token);

        if (!folderExist)
        {
            await Connection.SftpClient.CreateDirectoryAsync(folder!, true, cancellationToken: token);
        }

        var fileExist = await ExistFileAsync(remotePath, token);

        if (fileExist)
        {
            await Connection.SftpClient.DeleteFileAsync(remotePath, token);
        }

        await Connection.SftpClient.UploadFileAsync(localPath, remotePath, token);
    }

    public async Task<string?> ExecuteAsync(string command, CancellationToken token)
    {
        var process = await Connection.SshClient.ExecuteAsync(command, cancellationToken: token);
        var (output, error) = await process.ReadToEndAsStringAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(error);
        }

        return output?.Trim();
    }

    public async Task<bool> ExistFileAsync(string path, CancellationToken token)
    {
        try
        {
            await ExecuteAsync($"sudo test -f {path}", token);
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
            await ExecuteAsync($"sudo test -d {path}", token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}