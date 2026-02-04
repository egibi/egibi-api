namespace egibi_api.Models;

// --- Configuration ---

public class StorageConfig
{
    public string ExternalDiskPath { get; set; } = "/mnt/egibi-external";
    public int ThresholdPercent { get; set; } = 75;
    public int KeepMonths { get; set; } = 6;
    public bool AutoArchiveEnabled { get; set; } = true;
    public int AutoArchiveIntervalHours { get; set; } = 6;
    public int MaxPostgresBackups { get; set; } = 10;
}

// --- Disk Status ---

public class StorageStatusResponse
{
    public DiskUsageInfo DockerVolume { get; set; } = new();
    public DiskUsageInfo? ExternalDisk { get; set; }
    public VolumeInfo PostgresVolume { get; set; } = new();
    public VolumeInfo QuestDbVolume { get; set; } = new();
    public int ArchivedPartitionCount { get; set; }
    public bool ThresholdExceeded { get; set; }
    public StorageConfig Config { get; set; } = new();
}

public class DiskUsageInfo
{
    public string MountPoint { get; set; } = "";
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long AvailableBytes { get; set; }
    public int UsagePercent { get; set; }
}

public class VolumeInfo
{
    public string Name { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
}

// --- QuestDB Partitions ---

public class QuestDbPartition
{
    public string Name { get; set; } = "";
    public long RowCount { get; set; }
    public long DiskSizeBytes { get; set; }
    public string DiskSizeFormatted { get; set; } = "";
    public string? MinTimestamp { get; set; }
    public string? MaxTimestamp { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchiveEligible { get; set; }
}

public class PartitionListResponse
{
    public List<QuestDbPartition> HotPartitions { get; set; } = new();
    public List<ArchivedPartition> ArchivedPartitions { get; set; } = new();
}

public class ArchivedPartition
{
    public string Name { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime ArchivedAt { get; set; }
    public long RowCount { get; set; }
}

// --- Operations ---

public class ArchiveRequest
{
    public bool Force { get; set; } = false;
    public List<string>? SpecificPartitions { get; set; }
}

public class RestoreRequest
{
    public string PartitionName { get; set; } = "";
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> Details { get; set; } = new();
}

// --- Cleanup ---

public class CleanupResult
{
    public int ExpiredTokensPruned { get; set; }
    public int StaleAuthorizationsPruned { get; set; }
    public bool VacuumCompleted { get; set; }
    public string Message { get; set; } = "";
}

// --- Backup ---

public class BackupInfo
{
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

// --- Archive Log ---

public class ArchiveLogEntry
{
    public string Action { get; set; } = "";       // "archive", "restore", "cleanup", "backup"
    public string Target { get; set; } = "";        // partition name, "oidc-tokens", "postgresql"
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; } = "";
}
