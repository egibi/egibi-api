using System.Diagnostics;
using System.Text.Json;
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services;

public class StorageService
{
    private readonly EgibiDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StorageService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string ConfigKey = "StorageConfig";
    private static readonly string LogFileName = "archive-log.json";

    public StorageService(
        EgibiDbContext db,
        IConfiguration configuration,
        ILogger<StorageService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // =========================================================================
    // Configuration
    // =========================================================================

    public async Task<StorageConfig> GetConfigAsync()
    {
        var configEntity = await _db.Set<AppConfiguration>()
            .FirstOrDefaultAsync(c => c.Name == ConfigKey);

        if (configEntity?.Description != null)
        {
            try
            {
                return JsonSerializer.Deserialize<StorageConfig>(configEntity.Description) ?? new StorageConfig();
            }
            catch
            {
                return new StorageConfig();
            }
        }

        return new StorageConfig();
    }

    public async Task<StorageConfig> SaveConfigAsync(StorageConfig config)
    {
        var configEntity = await _db.Set<AppConfiguration>()
            .FirstOrDefaultAsync(c => c.Name == ConfigKey);

        var json = JsonSerializer.Serialize(config);

        if (configEntity != null)
        {
            configEntity.Description = json;
            configEntity.LastModifiedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Set<AppConfiguration>().Add(new AppConfiguration
            {
                Name = ConfigKey,
                Description = json,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return config;
    }

    // =========================================================================
    // Storage Status
    // =========================================================================

    public async Task<StorageStatusResponse> GetStatusAsync()
    {
        var config = await GetConfigAsync();
        var response = new StorageStatusResponse { Config = config };

        // Docker volume disk info
        try
        {
            var dockerRoot = "/var/lib/docker";
            var driveInfo = GetDriveInfo(dockerRoot);
            if (driveInfo != null)
            {
                response.DockerVolume = new DiskUsageInfo
                {
                    MountPoint = driveInfo.RootDirectory.FullName,
                    TotalBytes = driveInfo.TotalSize,
                    UsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                    AvailableBytes = driveInfo.AvailableFreeSpace,
                    UsagePercent = (int)((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100)
                };
                response.ThresholdExceeded = response.DockerVolume.UsagePercent >= config.ThresholdPercent;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get Docker volume disk info");
        }

        // External disk info
        if (Directory.Exists(config.ExternalDiskPath))
        {
            try
            {
                var driveInfo = GetDriveInfo(config.ExternalDiskPath);
                if (driveInfo != null)
                {
                    response.ExternalDisk = new DiskUsageInfo
                    {
                        MountPoint = config.ExternalDiskPath,
                        TotalBytes = driveInfo.TotalSize,
                        UsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                        AvailableBytes = driveInfo.AvailableFreeSpace,
                        UsagePercent = (int)((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get external disk info");
            }

            // Count archived partitions
            var archiveDir = Path.Combine(config.ExternalDiskPath, "questdb-archive");
            if (Directory.Exists(archiveDir))
            {
                response.ArchivedPartitionCount = Directory.GetDirectories(archiveDir, "ohlc_*").Length;
            }
        }

        return response;
    }

    // =========================================================================
    // QuestDB Partitions
    // =========================================================================

    public async Task<PartitionListResponse> GetPartitionsAsync()
    {
        var config = await GetConfigAsync();
        var response = new PartitionListResponse();

        // Get hot partitions from QuestDB
        try
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-config.KeepMonths).ToString("yyyy-MM");
            var result = await QueryQuestDbAsync(
                "SELECT name, numRows, diskSize, minTimestamp, maxTimestamp, active FROM table_partitions('ohlc') ORDER BY name DESC");

            if (result != null)
            {
                var dataset = result.Value.GetProperty("dataset");
                foreach (var row in dataset.EnumerateArray())
                {
                    var elements = row.EnumerateArray().ToList();
                    var name = elements[0].GetString() ?? "";
                    var partition = new QuestDbPartition
                    {
                        Name = name,
                        RowCount = elements[1].GetInt64(),
                        DiskSizeBytes = elements[2].GetInt64(),
                        DiskSizeFormatted = FormatBytes(elements[2].GetInt64()),
                        MinTimestamp = elements[3].GetString()?[..10],
                        MaxTimestamp = elements[4].GetString()?[..10],
                        IsActive = elements[5].GetBoolean(),
                        IsArchiveEligible = !elements[5].GetBoolean() && string.Compare(name, cutoffDate, StringComparison.Ordinal) < 0
                    };
                    response.HotPartitions.Add(partition);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query QuestDB partitions");
        }

        // Get archived partitions
        var archiveDir = Path.Combine(config.ExternalDiskPath, "questdb-archive");
        if (Directory.Exists(archiveDir))
        {
            foreach (var dir in Directory.GetDirectories(archiveDir, "ohlc_*").OrderByDescending(d => d))
            {
                var dirInfo = new DirectoryInfo(dir);
                var name = dirInfo.Name.Replace("ohlc_", "");
                var size = GetDirectorySize(dir);

                response.ArchivedPartitions.Add(new ArchivedPartition
                {
                    Name = name,
                    SizeBytes = size,
                    SizeFormatted = FormatBytes(size),
                    ArchivedAt = dirInfo.CreationTimeUtc,
                    RowCount = 0 // Would need manifest lookup for exact count
                });
            }
        }

        return response;
    }

    public async Task<OperationResult> ArchivePartitionsAsync(ArchiveRequest request)
    {
        var config = await GetConfigAsync();
        var result = new OperationResult();

        if (!Directory.Exists(config.ExternalDiskPath))
        {
            result.Success = false;
            result.Message = $"External disk not available at {config.ExternalDiskPath}";
            return result;
        }

        var archiveDir = Path.Combine(config.ExternalDiskPath, "questdb-archive");
        Directory.CreateDirectory(archiveDir);

        // Determine which partitions to archive
        List<string> partitionsToArchive;

        if (request.SpecificPartitions?.Any() == true)
        {
            partitionsToArchive = request.SpecificPartitions;
        }
        else
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-config.KeepMonths).ToString("yyyy-MM");
            var partitions = await GetPartitionsAsync();
            partitionsToArchive = partitions.HotPartitions
                .Where(p => p.IsArchiveEligible)
                .Select(p => p.Name)
                .ToList();
        }

        if (!partitionsToArchive.Any())
        {
            result.Success = true;
            result.Message = "No partitions eligible for archival";
            return result;
        }

        var archived = 0;
        foreach (var partitionName in partitionsToArchive)
        {
            try
            {
                // 1. Detach the partition
                var detachResult = await QueryQuestDbAsync($"ALTER TABLE ohlc DETACH PARTITION LIST '{partitionName}'");
                if (detachResult == null)
                {
                    result.Details.Add($"Failed to detach {partitionName}");
                    continue;
                }

                // 2. Copy from QuestDB container to external disk
                var containerPath = $"egibi-questdb:/var/lib/questdb/db/ohlc/{partitionName}.detached";
                var archivePath = Path.Combine(archiveDir, $"ohlc_{partitionName}");

                var copySuccess = await DockerCpAsync(containerPath, archivePath);
                if (!copySuccess)
                {
                    // Reattach on failure
                    await QueryQuestDbAsync($"ALTER TABLE ohlc ATTACH PARTITION LIST '{partitionName}'");
                    result.Details.Add($"Failed to copy {partitionName}, reattached");
                    continue;
                }

                // 3. Remove detached files from container
                await DockerExecAsync("egibi-questdb", $"rm -rf /var/lib/questdb/db/ohlc/{partitionName}.detached");

                // 4. Log it
                await AddLogEntryAsync("archive", partitionName, true, $"Archived to {archivePath}");
                result.Details.Add($"Archived {partitionName}");
                archived++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving partition {Partition}", partitionName);
                result.Details.Add($"Error archiving {partitionName}: {ex.Message}");

                // Try to reattach
                try { await QueryQuestDbAsync($"ALTER TABLE ohlc ATTACH PARTITION LIST '{partitionName}'"); }
                catch { /* best effort */ }
            }
        }

        result.Success = archived > 0;
        result.Message = $"Archived {archived} of {partitionsToArchive.Count} partition(s)";
        return result;
    }

    public async Task<OperationResult> RestorePartitionAsync(string partitionName)
    {
        var config = await GetConfigAsync();
        var result = new OperationResult();

        var archivePath = Path.Combine(config.ExternalDiskPath, "questdb-archive", $"ohlc_{partitionName}");
        if (!Directory.Exists(archivePath))
        {
            result.Success = false;
            result.Message = $"Archived partition not found: {partitionName}";
            return result;
        }

        try
        {
            // 1. Copy to container as .detached
            var containerPath = $"egibi-questdb:/var/lib/questdb/db/ohlc/{partitionName}.detached";
            var copySuccess = await DockerCpToContainerAsync(archivePath, containerPath);
            if (!copySuccess)
            {
                result.Success = false;
                result.Message = $"Failed to copy partition {partitionName} to container";
                return result;
            }

            // 2. Attach the partition
            var attachResult = await QueryQuestDbAsync($"ALTER TABLE ohlc ATTACH PARTITION LIST '{partitionName}'");
            if (attachResult == null)
            {
                await DockerExecAsync("egibi-questdb", $"rm -rf /var/lib/questdb/db/ohlc/{partitionName}.detached");
                result.Success = false;
                result.Message = $"Failed to attach partition {partitionName}";
                return result;
            }

            await AddLogEntryAsync("restore", partitionName, true, "Restored and attached");

            result.Success = true;
            result.Message = $"Partition {partitionName} restored — data is now queryable";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring partition {Partition}", partitionName);
            result.Success = false;
            result.Message = $"Error: {ex.Message}";
        }

        return result;
    }

    // =========================================================================
    // OIDC Token Cleanup
    // =========================================================================

    public async Task<CleanupResult> CleanupOidcTokensAsync()
    {
        var result = new CleanupResult();

        try
        {
            // Count and delete expired tokens
            var expiredTokens = await _db.Database
                .ExecuteSqlRawAsync("DELETE FROM \"OpenIddictTokens\" WHERE \"ExpirationDate\" < NOW()");
            result.ExpiredTokensPruned = expiredTokens;

            // Count and delete stale authorizations
            var staleAuths = await _db.Database
                .ExecuteSqlRawAsync(
                    "DELETE FROM \"OpenIddictAuthorizations\" WHERE \"Status\" = 'revoked' " +
                    "OR (\"CreationDate\" < NOW() - INTERVAL '30 days' AND \"Status\" != 'valid')");
            result.StaleAuthorizationsPruned = staleAuths;

            // Vacuum
            await _db.Database.ExecuteSqlRawAsync("VACUUM ANALYZE");
            result.VacuumCompleted = true;

            result.Message = $"Pruned {expiredTokens} tokens and {staleAuths} authorizations";

            await AddLogEntryAsync("cleanup", "oidc-tokens", true, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up OIDC tokens");
            result.Message = $"Error: {ex.Message}";
        }

        return result;
    }

    // =========================================================================
    // PostgreSQL Backup
    // =========================================================================

    public async Task<OperationResult> BackupPostgresAsync()
    {
        var config = await GetConfigAsync();
        var result = new OperationResult();

        if (!Directory.Exists(config.ExternalDiskPath))
        {
            result.Success = false;
            result.Message = $"External disk not available at {config.ExternalDiskPath}";
            return result;
        }

        var backupDir = Path.Combine(config.ExternalDiskPath, "postgres-archive");
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFile = Path.Combine(backupDir, $"egibi_pg_backup_{timestamp}.sql.gz");

        try
        {
            // pg_dump from container, pipe through gzip
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"docker exec egibi-postgres pg_dump -U postgres egibi_app_db | gzip > '{backupFile}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && File.Exists(backupFile))
            {
                var size = new FileInfo(backupFile).Length;
                result.Success = true;
                result.Message = $"Backup created: {Path.GetFileName(backupFile)} ({FormatBytes(size)})";

                // Prune old backups
                var backups = Directory.GetFiles(backupDir, "egibi_pg_backup_*.sql.gz")
                    .OrderByDescending(f => f)
                    .ToList();

                if (backups.Count > config.MaxPostgresBackups)
                {
                    var toRemove = backups.Skip(config.MaxPostgresBackups);
                    foreach (var old in toRemove)
                    {
                        File.Delete(old);
                        result.Details.Add($"Pruned old backup: {Path.GetFileName(old)}");
                    }
                }

                await AddLogEntryAsync("backup", "postgresql", true, result.Message);
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                result.Success = false;
                result.Message = $"Backup failed: {error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backing up PostgreSQL");
            result.Success = false;
            result.Message = $"Error: {ex.Message}";
        }

        return result;
    }

    public async Task<List<BackupInfo>> GetBackupsAsync()
    {
        var config = await GetConfigAsync();
        var backupDir = Path.Combine(config.ExternalDiskPath, "postgres-archive");
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(backupDir)) return backups;

        foreach (var file in Directory.GetFiles(backupDir, "egibi_pg_backup_*.sql.gz").OrderByDescending(f => f))
        {
            var info = new FileInfo(file);
            backups.Add(new BackupInfo
            {
                FileName = info.Name,
                SizeBytes = info.Length,
                SizeFormatted = FormatBytes(info.Length),
                CreatedAt = info.CreationTimeUtc
            });
        }

        return backups;
    }

    // =========================================================================
    // Archive Log
    // =========================================================================

    public async Task<List<ArchiveLogEntry>> GetLogAsync(int limit = 50)
    {
        var config = await GetConfigAsync();
        var logPath = Path.Combine(config.ExternalDiskPath, LogFileName);

        if (!File.Exists(logPath)) return new List<ArchiveLogEntry>();

        try
        {
            var json = await File.ReadAllTextAsync(logPath);
            var entries = JsonSerializer.Deserialize<List<ArchiveLogEntry>>(json) ?? new();
            return entries.OrderByDescending(e => e.Timestamp).Take(limit).ToList();
        }
        catch
        {
            return new List<ArchiveLogEntry>();
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<JsonElement?> QueryQuestDbAsync(string query)
    {
        try
        {
            var questDbUrl = _configuration.GetValue<string>("QuestDb:HttpUrl") ?? "http://localhost:9000";
            var client = _httpClientFactory.CreateClient();
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await client.GetAsync($"{questDbUrl}/exec?query={encodedQuery}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                // Check for QuestDB error in response
                if (doc.RootElement.TryGetProperty("error", out _))
                {
                    _logger.LogWarning("QuestDB query error: {Query} -> {Response}", query, json);
                    return null;
                }

                return doc.RootElement;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QuestDB query failed: {Query}", query);
        }

        return null;
    }

    private async Task<bool> DockerCpAsync(string containerPath, string hostPath)
    {
        return await RunProcessAsync("docker", $"cp {containerPath} {hostPath}");
    }

    private async Task<bool> DockerCpToContainerAsync(string hostPath, string containerPath)
    {
        return await RunProcessAsync("docker", $"cp {hostPath} {containerPath}");
    }

    private async Task DockerExecAsync(string container, string command)
    {
        await RunProcessAsync("docker", $"exec {container} {command}");
    }

    private async Task<bool> RunProcessAsync(string fileName, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process failed: {FileName} {Args}", fileName, arguments);
            return false;
        }
    }

    private DriveInfo? GetDriveInfo(string path)
    {
        try
        {
            // Find the drive that contains this path
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderByDescending(d => d.RootDirectory.FullName.Length)
                .FirstOrDefault(d => path.StartsWith(d.RootDirectory.FullName));
        }
        catch
        {
            return null;
        }
    }

    private long GetDirectorySize(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    private async Task AddLogEntryAsync(string action, string target, bool success, string details)
    {
        try
        {
            var config = await GetConfigAsync();
            var logPath = Path.Combine(config.ExternalDiskPath, LogFileName);

            List<ArchiveLogEntry> entries;
            if (File.Exists(logPath))
            {
                var json = await File.ReadAllTextAsync(logPath);
                entries = JsonSerializer.Deserialize<List<ArchiveLogEntry>>(json) ?? new();
            }
            else
            {
                Directory.CreateDirectory(config.ExternalDiskPath);
                entries = new();
            }

            entries.Add(new ArchiveLogEntry
            {
                Action = action,
                Target = target,
                Timestamp = DateTime.UtcNow,
                Success = success,
                Details = details
            });

            // Keep last 500 entries
            if (entries.Count > 500)
                entries = entries.OrderByDescending(e => e.Timestamp).Take(500).ToList();

            await File.WriteAllTextAsync(logPath, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write archive log");
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F0} KB";
        return $"{bytes} B";
    }
}