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
        Options.Add(new CliOption<string[]?>(Constants.INCLUDE_FILES_PARAMETER)
        {
            Description = "Copy the specified project file or directory to output directory",
        });
    }

    protected override async Task ExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        var projectPath = parseResult.GetValue<string>(Constants.PROJECT_PARAMETER);
        using var project = new Project(projectPath);
        await project.InitializeAsync(token);
        var publishPath = await PublishAsync(project, token);
        var includeFiles = parseResult.GetValue<string[]>(Constants.INCLUDE_FILES_PARAMETER);
        if (includeFiles != null) IncludeFiles(project, publishPath, includeFiles);
        var archivePath = await CompressAsync(project, publishPath, token);
        using var server = new Server(parseResult, project.Options);
        await server.InitializeAsync(token);
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

    private static async Task<string> PublishAsync(Project project, CancellationToken token)
    {
        var publishPath = Path.Combine(project.WorkDirectory, "publish");
        if (Directory.Exists(publishPath)) Directory.Delete(publishPath, true);

        Console.WriteLine($"publishing project {project.AssemblyName}");

        await ProcessHelper.RunCommandAsync(
            "dotnet",
            ["publish", "-o", publishPath, "-r", "linux-x64", "--self-contained", project.RootDirectory],
            token
        );

        return publishPath;
    }

    private static async Task<string> CompressAsync(Project project, string publishPath, CancellationToken token)
    {
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

    private static void IncludeFiles(Project project, string publishPath, string[] paths)
    {
        foreach (var path in paths)
        {
            if (Path.IsPathRooted(path)) throw new Exception($"Can not include absolute path '{path}'");
            var sourcePath = Path.Combine(project.RootDirectory, path);
            if (File.Exists(sourcePath))
            {
                var targetPath = Path.Combine(publishPath, path);
                CopyFile(sourcePath, targetPath);
            }

            var files = Directory.GetFiles(project.RootDirectory, path, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(project.RootDirectory, file);
                var targetPath = Path.Combine(publishPath, relativePath);
                CopyFile(file, targetPath);
            }
        }
    }

    private static void CopyFile(string source, string target)
    {
        var directory = Path.GetDirectoryName(target);
        if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.Copy(source, target);
    }
}