using egibi_api.Models;
using egibi_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly StorageService _storageService;

    public StorageController(StorageService storageService)
    {
        _storageService = storageService;
    }

    // --- Configuration ---

    [HttpGet("config")]
    public async Task<ActionResult<StorageConfig>> GetConfig()
    {
        var config = await _storageService.GetConfigAsync();
        return Ok(config);
    }

    [HttpPut("config")]
    public async Task<ActionResult<StorageConfig>> UpdateConfig([FromBody] StorageConfig config)
    {
        if (config.ThresholdPercent < 10 || config.ThresholdPercent > 95)
            return BadRequest("Threshold must be between 10% and 95%");

        if (config.KeepMonths < 1 || config.KeepMonths > 60)
            return BadRequest("Keep months must be between 1 and 60");

        if (config.AutoArchiveIntervalHours < 1 || config.AutoArchiveIntervalHours > 168)
            return BadRequest("Auto-archive interval must be between 1 and 168 hours");

        var saved = await _storageService.SaveConfigAsync(config);
        return Ok(saved);
    }

    // --- Status ---

    [HttpGet("status")]
    public async Task<ActionResult<StorageStatusResponse>> GetStatus()
    {
        var status = await _storageService.GetStatusAsync();
        return Ok(status);
    }

    // --- Partitions ---

    [HttpGet("partitions")]
    public async Task<ActionResult<PartitionListResponse>> GetPartitions()
    {
        var partitions = await _storageService.GetPartitionsAsync();
        return Ok(partitions);
    }

    [HttpPost("archive")]
    public async Task<ActionResult<OperationResult>> ArchivePartitions([FromBody] ArchiveRequest request)
    {
        var result = await _storageService.ArchivePartitionsAsync(request);
        return Ok(result);
    }

    [HttpPost("restore")]
    public async Task<ActionResult<OperationResult>> RestorePartition([FromBody] RestoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartitionName))
            return BadRequest("Partition name is required");

        var result = await _storageService.RestorePartitionAsync(request.PartitionName);
        return Ok(result);
    }

    // --- Cleanup ---

    [HttpPost("cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupTokens()
    {
        var result = await _storageService.CleanupOidcTokensAsync();
        return Ok(result);
    }

    // --- Backups ---

    [HttpGet("backups")]
    public async Task<ActionResult<List<BackupInfo>>> GetBackups()
    {
        var backups = await _storageService.GetBackupsAsync();
        return Ok(backups);
    }

    [HttpPost("backup")]
    public async Task<ActionResult<OperationResult>> BackupPostgres()
    {
        var result = await _storageService.BackupPostgresAsync();
        return Ok(result);
    }

    // --- Logs ---

    [HttpGet("log")]
    public async Task<ActionResult<List<ArchiveLogEntry>>> GetLog([FromQuery] int limit = 50)
    {
        var log = await _storageService.GetLogAsync(limit);
        return Ok(log);
    }
}
