using System.Diagnostics;

namespace egibi_api.Configuration;

/// <summary>
/// Ensures Docker development infrastructure (PostgreSQL, QuestDB) is running.
/// Only used in Development environment — automatically starts containers
/// via docker compose if they aren't already running.
/// </summary>
public static class DevInfrastructure
{
    private const string PostgresContainer = "egibi-postgres";
    private const string QuestDbContainer = "egibi-questdb";
    private const int HealthTimeoutSeconds = 60;

    /// <summary>
    /// Checks if dev containers are running and starts them if needed.
    /// Resolves docker-compose.yml from the solution root (one level up from egibi-api).
    /// </summary>
    public static async Task EnsureDatabasesAsync(string contentRootPath)
    {
        // docker-compose.yml lives in the repo root (parent of egibi-api/)
        var composeFile = Path.GetFullPath(Path.Combine(contentRootPath, "..", "docker-compose.yml"));

        if (!File.Exists(composeFile))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  [DevInfra] docker-compose.yml not found at: {composeFile}");
            Console.WriteLine($"  [DevInfra] Skipping auto-start. Start databases manually.");
            Console.ResetColor();
            return;
        }

        if (!await IsDockerAvailableAsync())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [DevInfra] Docker not found or not running. Start Docker Desktop and try again.");
            Console.ResetColor();
            return;
        }

        var pgRunning = await IsContainerRunningAsync(PostgresContainer);
        var qdbRunning = await IsContainerRunningAsync(QuestDbContainer);

        if (pgRunning && qdbRunning)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [DevInfra] PostgreSQL and QuestDB are already running.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  [DevInfra] Starting database containers...");
        Console.ResetColor();

        var startResult = await RunProcessAsync("docker", $"compose -f \"{composeFile}\" up -d", timeoutMs: 120_000);

        if (startResult.ExitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [DevInfra] docker compose failed (exit {startResult.ExitCode}):");
            Console.WriteLine($"  {startResult.StdErr}");
            Console.ResetColor();
            return;
        }

        // Wait for both containers to be healthy
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  [DevInfra] Waiting for containers to be healthy...");
        Console.ResetColor();

        var healthy = await WaitForHealthyAsync(HealthTimeoutSeconds);

        if (healthy)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [DevInfra] PostgreSQL ✓  QuestDB ✓  — Ready.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [DevInfra] Timed out waiting for healthy containers. Continuing anyway...");
            Console.ResetColor();
        }
    }

    private static async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var result = await RunProcessAsync("docker", "info", timeoutMs: 5000);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsContainerRunningAsync(string containerName)
    {
        try
        {
            var result = await RunProcessAsync("docker",
                $"ps -q -f name={containerName} -f status=running", timeoutMs: 5000);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for TCP ports to accept connections instead of relying on docker inspect.
    /// This is faster and avoids Go template escaping issues on Windows.
    /// </summary>
    private static async Task<bool> WaitForHealthyAsync(int timeoutSeconds)
    {
        var sw = Stopwatch.StartNew();
        var pgReady = false;
        var qdbReady = false;

        while (sw.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (!pgReady)
                pgReady = await IsTcpPortOpenAsync("localhost", 5432);

            if (!qdbReady)
                qdbReady = await IsTcpPortOpenAsync("localhost", 8812);

            if (pgReady && qdbReady)
                return true;

            await Task.Delay(500);
        }

        return false;
    }

    private static async Task<bool> IsTcpPortOpenAsync(string host, int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(1000));
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, int timeoutMs = 30000)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        var completed = process.WaitForExit(timeoutMs);

        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            return new ProcessResult(-1, "", "Process timed out");
        }

        return new ProcessResult(process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private record ProcessResult(int ExitCode, string StdOut, string StdErr);
}