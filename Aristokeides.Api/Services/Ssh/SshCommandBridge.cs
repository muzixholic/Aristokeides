using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FxSsh.Services;
using Microsoft.Extensions.Logging;

namespace Aristokeides.Api.Services.Ssh;

public class SshCommandBridge
{
    private readonly ILogger<SshCommandBridge> _logger;

    public SshCommandBridge(ILogger<SshCommandBridge> logger)
    {
        _logger = logger;
    }

    public async Task RunGitCommandAsync(SessionChannel channel, string commandName, string repoPath, SshSessionState state)
    {
        _logger.LogInformation("Running {Command} for repository {RepoPath} by user {Username}", commandName, repoPath, state.Username);

        string physicalRepoPath = Path.Combine(Directory.GetCurrentDirectory(), "Repositories", repoPath + ".git");

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"{commandName.Substring(4)} \"{physicalRepoPath}\"", // e.g. upload-pack "..."
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process? process;
        try
        {
            process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start git process.");
                channel.SendClose(1);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while starting git process.");
            channel.SendClose(1);
            return;
        }

        channel.DataReceived += (sender, args) =>
        {
            try
            {
                process.StandardInput.BaseStream.Write(args);
                process.StandardInput.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to git process standard input.");
            }
        };
        
        channel.EofReceived += (sender, args) =>
        {
            try
            {
                process.StandardInput.Close();
            }
            catch { }
        };
        
        channel.CloseReceived += (sender, args) =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
        };

        try
        {
            var stdoutTask = CopyStreamToChannelAsync(process.StandardOutput.BaseStream, channel);
            var stderrTask = CopyStreamToChannelExtendedAsync(process.StandardError.BaseStream, channel);

            await process.WaitForExitAsync();
            
            try { process.StandardInput.Close(); } catch { }

            await Task.WhenAll(stdoutTask, stderrTask);
            
            channel.SendClose((uint)process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while piping git process.");
            channel.SendClose(1);
        }
        finally
        {
            try { process.Dispose(); } catch { }
        }
    }

    private async Task CopyStreamToChannelAsync(Stream source, SessionChannel channel)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            channel.SendData(data);
        }
    }

    private async Task CopyStreamToChannelExtendedAsync(Stream source, SessionChannel channel)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            // Since FxSsh SessionChannel might not have SendExtendedData out of the box easily,
            // we will fallback to standard SendData for stderr messages to client.
            channel.SendData(data);
        }
    }
}
