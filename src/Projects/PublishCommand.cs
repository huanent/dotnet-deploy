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
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);
        using var server = new Server(parseResult, project.Options);
        await server.InitializeAsync(token);
        string archivePath = await Publish(project, token);
        await UploadAsync(project.AssemblyName, server, archivePath, token);

        try
        {
            await server.ExecuteAsync($"systemctl restart {project.AssemblyName}", token);
            Console.WriteLine($"Service {project.AssemblyName} restarted");
        }
        catch
        {
            Console.WriteLine($"Service {project.AssemblyName} not found,install systemd a service is recommend!");
        }
    }

    private static async Task UploadAsync(string assemblyName, Server server, string archivePath, CancellationToken token)
    {
        Console.WriteLine($"Uploading publish files to server");
        var remoteAppDirectory = Path.Combine(server.RootDirectory, assemblyName);
        var remoteArchiveFile = Path.Combine(server.RootDirectory, $"{assemblyName}.tar.gz");
        await server.UploadFileAsync(archivePath, remoteArchiveFile, token);
        await server.SftpClient.CreateDirectoryAsync(remoteAppDirectory, true, cancellationToken: token);
        await server.ExecuteAsync($"tar --overwrite -xzvf {remoteArchiveFile} -C {remoteAppDirectory}", token);
        Console.WriteLine($"Publish files uploaded!");
    }

    private static async Task<string> Publish(Project project, CancellationToken token)
    {
        var publishPath = Path.Combine(project.WorkDirectory, "publish");
        if (Directory.Exists(publishPath)) Directory.Delete(publishPath, true);

        Console.WriteLine($"publishing project {project.AssemblyName}");

        await ProcessHelper.RunCommandAsync(
            "dotnet",
            ["publish", "-o", publishPath, "-r", "linux-x64", "--self-contained", project.RootDirectory],
            token
        );

        var archivePath = Path.Combine(project.WorkDirectory, "publish.tar.gz");
        if (File.Exists(archivePath)) File.Delete(archivePath);

        using (var archiveStream = File.OpenWrite(archivePath))
        {
            using var gzipStream = new GZipStream(archiveStream, CompressionLevel.SmallestSize);
            await TarFile.CreateFromDirectoryAsync(publishPath, gzipStream, false, token);
        };

        Console.WriteLine($"Project {project.AssemblyName} published!");
        return archivePath;
    }
}