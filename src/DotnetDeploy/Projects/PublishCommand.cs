using System.CommandLine;
using System.IO.Compression;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Projects;

public class PublishCommand : BaseCommand
{
    public PublishCommand()
        : base("publish", "Publish project to remote host")
    {
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        var project = new Project(projectPath);
        var server = new Server(parseResult, project.Options);
        await server.ConnectAsync(token);
        var binFolder = Path.Combine(project.Folder, "bin");
        var publishPath = Path.Combine(binFolder, "__publish__");
        if (Directory.Exists(publishPath)) Directory.Delete(publishPath, true);

        Console.WriteLine($"publishing project {project.AssemblyName}");
        await ProcessHelper.RunCommandAsync(
            "dotnet",
            ["publish", "-o", publishPath, "-r", "linux-x64", "--self-contained", project.Folder],
            token
        );

        var zipPath = Path.Combine(binFolder, "publish.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(publishPath, zipPath);
        Directory.Delete(publishPath, true);
        Console.WriteLine($"Project {project.AssemblyName} published!");

        var remoteRoot = "/var/dotnet-apps";
        var remoteAppFolder = Path.Combine(remoteRoot, project.AssemblyName);
        var remoteZipFile = Path.Combine(remoteRoot, $"{project.AssemblyName}.zip");

        if ((await server.SftpClient.GetAttributesAsync(remoteRoot, cancellationToken: token)) == null)
        {
            await server.SftpClient.CreateDirectoryAsync(remoteRoot, cancellationToken: token);
        }

        Console.WriteLine($"Uploading publish files to server");
        await server.SftpClient.UploadFileAsync(zipPath, remoteZipFile, true, cancellationToken: token);
        var process = await server.SshClient.ExecuteAsync($"unzip -o -d {remoteAppFolder} {remoteZipFile}", cancellationToken: token);
        await process.WaitForExitAsync(token);
         Console.WriteLine($"Publish files uploaded!");
    }
}