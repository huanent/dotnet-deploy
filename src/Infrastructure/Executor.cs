using System.Runtime.InteropServices;

namespace DotnetDeploy.Infrastructure;

public static class Executor
{
    public static async Task<string?> RunAsync(string program, IEnumerable<string> args, string cwd, CancellationToken token = default)
    {
        var (response, error) = await SimpleExec.Command.ReadAsync(
           program,
           args,
           workingDirectory: cwd,
           ct: token
       );

        if (!string.IsNullOrWhiteSpace(error)) throw new Exception(error);
        return response?.Trim();
    }

    public static async Task RunAsync(string command, string cwd, CancellationToken token = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await SimpleExec.Command.RunAsync(
              "cmd.exe",
              ["/C", command],
              ct: token
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await SimpleExec.Command.RunAsync(
             "zsh",
             ["-c", command],
             ct: token
           );
        }
        else
        {
            await SimpleExec.Command.RunAsync(
             "bash",
             ["-c", command],
             ct: token
           );
        }
    }
}