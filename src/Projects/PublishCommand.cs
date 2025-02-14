using System.CommandLine;
using System.Formats.Tar;
using System.IO.Compression;
using DotnetDeploy.Infrastructure;
using DotnetDeploy.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDeploy.Projects;

[Singleton(typeof(ICommand))]
public class PublishCommand : BaseCommand, ICommand
{
    public PublishCommand()
        : base("publish", "Publish project to remote host")
    {
        Options.Add(new CliOption<string[]?>(Constants.INCLUDE_FILES_PARAMETER)
        {
            Description = "Copy the specified project file or directory to output directory",
        });

        Options.Add(new CliOption<bool?>(Constants.BEFORE_COMMAND_PARAMETER)
        {
            Description = "Run command before dotnet publish",
        });

        Options.Add(new CliOption<bool?>(Constants.AFTER_COMMAND_PARAMETER)
        {
            Description = "Run command after dotnet publish",
        });
    }

    protected override async Task ExecuteAsync(HostDeployOptions options, Project project, CancellationToken token)
    {
        using var server = new Server(options);
        await server.InitializeAsync(token);

        if (!string.IsNullOrWhiteSpace(options.BeforeCommand))
        {
            Console.WriteLine($"Running before command '{options.BeforeCommand}'");
            await Executor.RunAsync(options.BeforeCommand, project.RootDirectory, token);
        }

        var publishPath = await PublishAsync(project, server, token);

        if (options.IncludeFiles != null)
        {
            IncludeFiles(project, publishPath, options.IncludeFiles);
        }

        if (!string.IsNullOrWhiteSpace(options.AfterCommand))
        {
            Console.WriteLine($"Running after command '{options.AfterCommand}'");
            await Executor.RunAsync(options.AfterCommand, project.RootDirectory, token);
        }

        Console.WriteLine($"Compressing published files");
        var archivePath = await CompressAsync(project, publishPath, token);
        Console.WriteLine($"Compressed");
        var remoteAppDirectory = Path.Combine(Server.RootDirectory, project.AssemblyName);
        var remoteArchiveFile = Path.Combine(Server.RootDirectory, $"{project.AssemblyName}.tar.gz");
        Console.WriteLine($"Uploading compressed file to remote host");
        await server.UploadFileAsync(archivePath, $"{project.AssemblyName}.tar.gz", token);
        Console.WriteLine($"Uploaded");

        await server.Connection.SftpClient.CreateDirectoryAsync(remoteAppDirectory, true, cancellationToken: token);

        try
        {
            await server.ExecuteAsync($"sudo systemctl stop {project.AssemblyName}", token);
            Console.WriteLine($"Service {project.AssemblyName} stopped");
        }
        catch { }

        Console.WriteLine($"Decompressing on remote host");
        await server.ExecuteAsync($"sudo tar --overwrite -xzvf {remoteArchiveFile} -C {remoteAppDirectory}", token);
        Console.WriteLine($"Decompressed");

        try
        {
            await server.ExecuteAsync($"sudo systemctl restart {project.AssemblyName}", token);
            Console.WriteLine($"Service {project.AssemblyName} restarted");
        }
        catch
        {
            Console.WriteLine($"""
            Service {project.AssemblyName} not found!
            Install a systemd service is recommend, see 'dotnet deploy systemd -h'
            """);
        }
    }

    private static async Task<string> PublishAsync(Project project, Server server, CancellationToken token)
    {
        var publishPath = Path.Combine(project.WorkDirectory, "publish");

        if (Directory.Exists(publishPath)) Directory.Delete(publishPath, true);

        var rid = GetRid(server.Arch);
        Console.WriteLine($"Publishing project '{project.AssemblyName}' RID '{rid}'");

        await Executor.RunAsync(
            "dotnet",
            ["publish", "-o", publishPath, "-r", rid, "--self-contained", project.RootDirectory],
            project.RootDirectory,
            token
        );

        return publishPath;
    }

    private static string GetRid(string arch)
    {
        return arch switch
        {
            "x86_64" => "linux-x64",
            "aarch64" => "linux-arm64",
            _ => throw new NotSupportedException($"Not supported arch {arch}"),
        };
    }

    private static async Task<string> CompressAsync(Project project, string publishPath, CancellationToken token)
    {
        var archivePath = Path.Combine(project.WorkDirectory, "publish.tar.gz");
        if (File.Exists(archivePath)) File.Delete(archivePath);

        using (var archiveStream = File.OpenWrite(archivePath))
        {
            using var gzipStream = new GZipStream(archiveStream, CompressionLevel.SmallestSize);
            await TarFile.CreateFromDirectoryAsync(publishPath, gzipStream, false, token);
        }

        return archivePath;
    }

    private static void IncludeFiles(Project project, string publishPath, string[] paths)
    {
        foreach (var path in paths)
        {
            if (Path.IsPathRooted(path)) throw new Exception($"Can not include absolute path '{path}'");
            var sourcePath = Path.Combine(project.RootDirectory, path);
            Console.WriteLine($"Include file '{sourcePath}'");

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
        File.Copy(source, target, true);
    }
}