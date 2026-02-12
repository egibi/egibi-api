#nullable disable
using System.Security.Claims;
using egibi_api.Authorization;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public class AccessRequestController : ControllerBase
    {
        private readonly AccessRequestService _accessRequestService;
        private readonly ILogger<AccessRequestController> _logger;

        public AccessRequestController(
            AccessRequestService accessRequestService,
            ILogger<AccessRequestController> logger)
        {
            _accessRequestService = accessRequestService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        // =============================================
        // GET PENDING REQUESTS
        // =============================================

        [HttpGet("pending")]
        public async Task<RequestResponse> GetPendingRequests()
        {
            try
            {
                var requests = await _accessRequestService.GetPendingRequestsAsync();

                var dtos = requests.Select(r => new AccessRequestDto
                {
                    Id = r.Id,
                    Email = r.Email,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReviewedAt = r.ReviewedAt
                }).ToList();

                return new RequestResponse(dtos, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending access requests");
                return new RequestResponse(null, 500, "Failed to retrieve access requests", new ResponseError(ex));
            }
        }

        // =============================================
        // GET ALL REQUESTS
        // =============================================

        [HttpGet("all")]
        public async Task<RequestResponse> GetAllRequests()
        {
            try
            {
                var requests = await _accessRequestService.GetAllRequestsAsync();

                var dtos = requests.Select(r => new AccessRequestDto
                {
                    Id = r.Id,
                    Email = r.Email,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Status = r.Status,
                    DenialReason = r.DenialReason,
                    CreatedAt = r.CreatedAt,
                    ReviewedByUserId = r.ReviewedByUserId,
                    ReviewedAt = r.ReviewedAt
                }).ToList();

                return new RequestResponse(dtos, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all access requests");
                return new RequestResponse(null, 500, "Failed to retrieve access requests", new ResponseError(ex));
            }
        }

        // =============================================
        // APPROVE REQUEST
        // =============================================

        [HttpPost("{id}/approve")]
        public async Task<RequestResponse> ApproveRequest(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (user, error) = await _accessRequestService.ApproveRequestAsync(id, adminId);

                if (error != null && user == null)
                    return new RequestResponse(null, 400, error);

                return new RequestResponse(
                    user != null ? new { userId = user.Id, email = user.Email } : null,
                    200,
                    error ?? "Access request approved. User account created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve access request {RequestId}", id);
                return new RequestResponse(null, 500, "Failed to approve access request", new ResponseError(ex));
            }
        }

        // =============================================
        // DENY REQUEST
        // =============================================

        [HttpPost("{id}/deny")]
        public async Task<RequestResponse> DenyRequest(int id, [FromBody] DenyAccessRequestBody body)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (success, error) = await _accessRequestService.DenyRequestAsync(id, adminId, body?.Reason);

                if (!success)
                    return new RequestResponse(null, 400, error);

                return new RequestResponse(null, 200, "Access request denied.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deny access request {RequestId}", id);
                return new RequestResponse(null, 500, "Failed to deny access request", new ResponseError(ex));
            }
        }
    }

    // =============================================
    // DTOs
    // =============================================

    public class AccessRequestDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; }
        public string DenialReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class DenyAccessRequestBody
    {
        public string Reason { get; set; }
    }
}
