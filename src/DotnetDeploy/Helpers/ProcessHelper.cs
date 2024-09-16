using System.Diagnostics;
using System.Text;

namespace DotnetDeploy;

public static class ProcessHelper
{
    internal static async Task<string> RunCommandAsync(string program, IEnumerable<string> args, CancellationToken token)
    {
        using var process = new Process();
        process.StartInfo.FileName = program;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null) outputBuilder.Append(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null) errorBuilder.Append(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(token);
        return process.ExitCode == 0 ? outputBuilder.ToString() : errorBuilder.ToString();
    }
}