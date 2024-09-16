using System.CommandLine;
using System.IO.Compression;
using DotnetDeploy.Projects;
using Tmds.Ssh;

namespace DotnetDeploy.Commands;

public class PublishCommand : CliCommand
{
    public PublishCommand()
        : base("publish", "Publish project to remote host")
    {

        Options.Add(new CliOption<string?>("--host")
        {
            Description = "Target host name or domain",
        });

        Options.Add(new CliOption<string?>("--password")
        {
            Description = "Host password"
        });

        Options.Add(new CliOption<string?>("--project")
        {
            Description = "Project path"
        });

        SetAction(async (parseResult, token) =>
        {
            var host = parseResult.GetValue<string>("--host");
            var password = parseResult.GetValue<string>("--password");
            var project = parseResult.GetValue<string>("--project");
            await ExecuteAsync(host, password, project, token);
        });
    }

    private static async Task ExecuteAsync(string? host, string? password, string? projectPath, CancellationToken token)
    {
        var project = new Project(projectPath);
        var binFolder = Path.Combine(project.Folder, "bin");
        var publishPath = Path.Combine(binFolder, "__publish__");
        if (Directory.Exists(publishPath)) Directory.Delete(publishPath, true);

        await ProcessHelper.RunCommandAsync(
            "dotnet",
            ["publish", "-o", publishPath, "-r", "linux-x64", "--self-contained", project.Folder],
            token
        );

        var zipPath = Path.Combine(binFolder, "publish.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(publishPath, zipPath);
        Directory.Delete(publishPath, true);

        var sshSettings = new SshClientSettings(host ?? project.Options.Host)
        {
            UserName = project.Options.UserName ?? "root",
            Credentials = [new PasswordCredential(password ?? project.Options.Password)]
        };

        using var sshClient = new SshClient(sshSettings);
        using var sftpClient = await sshClient.OpenSftpClientAsync();
        var remoteRoot = "/var/dotnet-apps";
        var remoteAppFolder = Path.Combine(remoteRoot, project.AssemblyName);
        var remoteZipFile = Path.Combine(remoteRoot, $"{project.AssemblyName}.zip");

        if ((await sftpClient.GetAttributesAsync(remoteRoot, cancellationToken: token)) == null)
        {
            await sftpClient.CreateDirectoryAsync(remoteRoot, cancellationToken: token);
        }

        await sftpClient.UploadFileAsync(zipPath, remoteZipFile, true, cancellationToken: token);
        var process = await sshClient.ExecuteAsync($"unzip -o -d {remoteAppFolder} {remoteZipFile}", cancellationToken: token);
        await process.WaitForExitAsync(token);
    }
}