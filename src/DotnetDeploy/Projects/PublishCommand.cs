using System.CommandLine;
using System.Formats.Tar;
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

        var archivePath = Path.Combine(binFolder, "publish.tar.gz");
        if (File.Exists(archivePath)) File.Delete(archivePath);

        using (var archiveStream = File.OpenWrite(archivePath))
        {
            using var gzipStream = new GZipStream(archiveStream, CompressionLevel.SmallestSize);
            await TarFile.CreateFromDirectoryAsync(publishPath, gzipStream, false, token);
            Directory.Delete(publishPath, true);
        };

        Console.WriteLine($"Project {project.AssemblyName} published!");

        var remoteRoot = "/var/dotnet-apps";
        var remoteAppFolder = Path.Combine(remoteRoot, project.AssemblyName);
        var remoteArchiveFile = Path.Combine(remoteRoot, $"{project.AssemblyName}.tar.gz");
        Console.WriteLine($"Uploading publish files to server");
        await server.UploadFileAsync(archivePath, remoteArchiveFile, token);
        await server.SftpClient.CreateDirectoryAsync(remoteAppFolder, true, cancellationToken: token);
        await server.ExecuteAsync($"tar --overwrite -xzvf {remoteArchiveFile} -C {remoteAppFolder}", token);
        Console.WriteLine($"Publish files uploaded!");
    }
}