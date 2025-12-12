using DotnetDeploy.Projects;
using Microsoft.Extensions.Logging;
using Tmds.Ssh;

namespace DotnetDeploy.Servers;

public class Server(HostDeployOptions options) : IDisposable
{
    private string? arch;
    private ServerConnection connection;
    public static string RootDirectory => "/var/dotnet-apps";
    public ServerConnection Connection => connection ?? throw new Exception("Server not Initialized");
    public string Arch => arch ?? throw new Exception("Server not Initialized");
    public string Username => options.UserName;

    public async Task InitializeAsync(CancellationToken token)
    {
        var credentials = new List<Credential>();

        if (!string.IsNullOrWhiteSpace(options.PrivateKey))
        {
            credentials.Add(new PrivateKeyCredential(options.PrivateKey, options.Password));
        }
        else if (!string.IsNullOrWhiteSpace(options.Password))
        {
            credentials.Add(new PasswordCredential(options.Password));
        }

        Console.WriteLine($"Connecting server {options.Host} with {options.UserName}");
        connection = new ServerConnection(options.Host, options.UserName, credentials);
        await connection.ConnectAsync(token);
        Console.WriteLine($"Server {options.Host} connected!");
        arch = await ExecuteAsync("arch", token);
        if (!await ExistFolderAsync(RootDirectory, token))
        {
            await connection.SshClient.ExecuteAsync($"sudo mkdir '{RootDirectory}'", token);
            await connection.SshClient.ExecuteAsync($"sudo chown {Username} '{RootDirectory}'", token);
        }
    }

    public async Task<string> UploadFileAsync(string localPath, string newName, CancellationToken token)
    {
        var remotePath = Path.Combine(RootDirectory, newName);
        var fileExist = await ExistFileAsync(remotePath, token);

        if (fileExist)
        {
            await Connection.SftpClient.DeleteFileAsync(remotePath, token);
        }

        await Connection.SftpClient.UploadFileAsync(localPath, remotePath, token);
        return remotePath;
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
        connection?.Dispose();
    }
}