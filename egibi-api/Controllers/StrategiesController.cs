using System.Text.Json;
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Models.Strategy;
using egibi_api.Services.Backtesting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EgibiCoreLibrary.Models;

namespace egibi_api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StrategiesController : ControllerBase
{
    private readonly EgibiDbContext _db;
    private readonly BacktestExecutionService _backtestService;
    private readonly ILogger<StrategiesController> _logger;

    public StrategiesController(
        EgibiDbContext db,
        BacktestExecutionService backtestService,
        ILogger<StrategiesController> logger)
    {
        _db = db;
        _backtestService = backtestService;
        _logger = logger;
    }


    // ═══════════════════════════════════════════════════════
    //  STRATEGY CRUD
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Get all strategies.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RequestResponse>> GetAll()
    {
        var strategies = await _db.Strategies
            .Include(s => s.Account)
                .ThenInclude(a => a!.Connection)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.IsSimple,
                s.IsActive,
                s.AccountId,
                AccountName = s.Account != null ? s.Account.Name : null,
                ConnectionName = s.Account != null && s.Account.Connection != null ? s.Account.Connection.Name : null,
                s.CreatedAt,
                s.UpdatedAt,
                BacktestCount = s.Backtests.Count
            })
            .ToListAsync();

        return Ok(new RequestResponse { ResponseCode = 200, ResponseData = strategies });
    }

    /// <summary>
    /// Get a single strategy with its rules configuration.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RequestResponse>> Get(int id)
    {
        var strategy = await _db.Strategies
            .Include(s => s.Account)
                .ThenInclude(a => a!.Connection)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (strategy == null)
            return NotFound(new RequestResponse { ResponseCode = 404, ResponseMessage = "Strategy not found." });

        StrategyConfigurationDto? config = null;
        if (!string.IsNullOrEmpty(strategy.RulesConfiguration))
        {
            config = JsonSerializer.Deserialize<StrategyConfigurationDto>(strategy.RulesConfiguration);
        }

        return Ok(new RequestResponse
        {
            ResponseCode = 200,
            ResponseData = new
            {
                strategy.Id,
                strategy.Name,
                strategy.Description,
                strategy.IsSimple,
                strategy.IsActive,
                strategy.AccountId,
                strategy.StrategyClassName,
                strategy.CreatedAt,
                strategy.UpdatedAt,
                Configuration = config
            }
        });
    }

    /// <summary>
    /// Create a new simple (UI-built) strategy.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RequestResponse>> Create([FromBody] CreateStrategyRequest request)
    {
        var strategy = new Data.Entities.Strategy
        {
            Name = request.Name,
            Description = request.Description,
            IsSimple = true,
            AccountId = request.Configuration?.AccountId,
            RulesConfiguration = request.Configuration != null
                ? JsonSerializer.Serialize(request.Configuration)
                : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.Strategies.Add(strategy);
        await _db.SaveChangesAsync();

        return Ok(new RequestResponse
        {
            ResponseCode = 200,
            ResponseMessage = "Strategy created successfully.",
            ResponseData = new { strategy.Id }
        });
    }

    /// <summary>
    /// Update an existing strategy.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RequestResponse>> Update(int id, [FromBody] CreateStrategyRequest request)
    {
        var strategy = await _db.Strategies.FindAsync(id);
        if (strategy == null)
            return NotFound(new RequestResponse { ResponseCode = 404, ResponseMessage = "Strategy not found." });

        strategy.Name = request.Name;
        strategy.Description = request.Description;
        strategy.AccountId = request.Configuration?.AccountId;
        strategy.RulesConfiguration = request.Configuration != null
            ? JsonSerializer.Serialize(request.Configuration)
            : null;
        strategy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new RequestResponse { ResponseCode = 200, ResponseMessage = "Strategy updated." });
    }

    /// <summary>
    /// Delete a strategy and its backtests.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<RequestResponse>> Delete(int id)
    {
        var strategy = await _db.Strategies
            .Include(s => s.Backtests)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (strategy == null)
            return NotFound(new RequestResponse { ResponseCode = 404, ResponseMessage = "Strategy not found." });

        _db.Strategies.Remove(strategy);
        await _db.SaveChangesAsync();

        return Ok(new RequestResponse { ResponseCode = 200, ResponseMessage = "Strategy deleted." });
    }


    // ═══════════════════════════════════════════════════════
    //  BACKTESTING
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Execute a backtest for a strategy.
    /// </summary>
    [HttpPost("{id}/backtest")]
    public async Task<ActionResult<RequestResponse>> RunBacktest(int id, [FromBody] BacktestRequestDto request)
    {
        request.StrategyId = id;

        try
        {
            var result = await _backtestService.RunBacktestAsync(request);

            return Ok(new RequestResponse
            {
                ResponseCode = 200,
                ResponseMessage = result.Warnings.Count > 0 ? "Backtest completed with warnings." : "Backtest completed.",
                ResponseData = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new RequestResponse { ResponseCode = 404, ResponseMessage = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new RequestResponse { ResponseCode = 400, ResponseMessage = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest execution failed for strategy {StrategyId}", id);
            return StatusCode(500, new RequestResponse
            {
                ResponseCode = 500,
                ResponseMessage = "Backtest execution failed. Check logs for details."
            });
        }
    }

    /// <summary>
    /// Get backtest history for a strategy.
    /// </summary>
    [HttpGet("{id}/backtests")]
    public async Task<ActionResult<RequestResponse>> GetBacktests(int id)
    {
        var backtests = await _db.Backtests
            .Where(b => b.StrategyId == id)
            .Include(b => b.BacktestStatus)
            .OrderByDescending(b => b.ExecutedAt)
            .Select(b => new
            {
                b.Id,
                b.Name,
                Status = b.BacktestStatus.Name,
                b.StartDate,
                b.EndDate,
                b.InitialCapital,
                b.FinalCapital,
                b.TotalReturnPct,
                b.TotalTrades,
                b.WinRate,
                b.MaxDrawdownPct,
                b.SharpeRatio,
                b.ExecutedAt
            })
            .ToListAsync();

        return Ok(new RequestResponse { ResponseCode = 200, ResponseData = backtests });
    }

    /// <summary>
    /// Get full backtest result (including equity curve and trade log).
    /// </summary>
    [HttpGet("backtests/{backtestId}")]
    public async Task<ActionResult<RequestResponse>> GetBacktestDetail(int backtestId)
    {
        var backtest = await _db.Backtests.FindAsync(backtestId);
        if (backtest == null)
            return NotFound(new RequestResponse { ResponseCode = 404, ResponseMessage = "Backtest not found." });

        BacktestResultDto? result = null;
        if (!string.IsNullOrEmpty(backtest.ResultJson))
        {
            result = JsonSerializer.Deserialize<BacktestResultDto>(backtest.ResultJson);
        }

        return Ok(new RequestResponse { ResponseCode = 200, ResponseData = result });
    }


    // ═══════════════════════════════════════════════════════
    //  DATA VERIFICATION
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Verify data availability before running a backtest.
    /// Returns coverage status, stored candle count, and whether auto-fetch is available.
    /// </summary>
    [HttpPost("verify-data")]
    public async Task<ActionResult<RequestResponse>> VerifyData([FromBody] DataVerificationRequestDto request)
    {
        try
        {
            var result = await _backtestService.VerifyDataAsync(
                request.Symbol, request.Source, request.Interval,
                request.StartDate, request.EndDate);

            return Ok(new RequestResponse { ResponseCode = 200, ResponseData = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data verification failed");
            return StatusCode(500, new RequestResponse
            {
                ResponseCode = 500,
                ResponseMessage = "Data verification failed. Check logs for details."
            });
        }
    }


    // ═══════════════════════════════════════════════════════
    //  DATA COVERAGE (for UI dropdowns)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Get available data coverage from QuestDB.
    /// Optional symbol filter: /api/strategies/data-coverage?symbol=BTC-USD
    /// </summary>
    [HttpGet("data-coverage")]
    public async Task<ActionResult<RequestResponse>> GetDataCoverage([FromQuery] string? symbol)
    {
        var coverage = await _backtestService.GetDataCoverageAsync(symbol);
        return Ok(new RequestResponse { ResponseCode = 200, ResponseData = coverage });
    }

    /// <summary>
    /// Get distinct symbols available in QuestDB.
    /// </summary>
    [HttpGet("available-symbols")]
    public async Task<ActionResult<RequestResponse>> GetAvailableSymbols()
    {
        var symbols = await _backtestService.GetAvailableSymbolsAsync();
        return Ok(new RequestResponse { ResponseCode = 200, ResponseData = symbols });
    }
}


// ═══════════════════════════════════════════════════════════
//  REQUEST MODEL
// ═══════════════════════════════════════════════════════════

public class CreateStrategyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StrategyConfigurationDto? Configuration { get; set; }
}

public class DataVerificationRequestDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}