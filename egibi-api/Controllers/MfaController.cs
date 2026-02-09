#nullable disable
using egibi_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class MfaController : ControllerBase
    {
        private readonly MfaService _mfaService;

        public MfaController(MfaService mfaService)
        {
            _mfaService = mfaService;
        }

        private int GetUserId()
        {
            var sub = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            return int.Parse(sub);
        }

        // =============================================
        // GET /api/mfa/status
        // =============================================
        /// <summary>
        /// Returns the current MFA status for the authenticated user.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var (isMfaEnabled, remainingCodes) = await _mfaService.GetStatusAsync(GetUserId());
            return Ok(new
            {
                isMfaEnabled,
                remainingRecoveryCodes = remainingCodes
            });
        }

        // =============================================
        // POST /api/mfa/setup
        // =============================================
        /// <summary>
        /// Initiates MFA setup. Returns the shared secret and QR code URI.
        /// The user must verify a code via /api/mfa/confirm before MFA is active.
        /// </summary>
        [HttpPost("setup")]
        public async Task<IActionResult> Setup()
        {
            var (sharedKey, qrUri, error) = await _mfaService.BeginSetupAsync(GetUserId());

            if (error != null)
                return BadRequest(new { error });

            return Ok(new
            {
                sharedKey,
                qrUri
            });
        }

        // =============================================
        // POST /api/mfa/confirm
        // =============================================
        /// <summary>
        /// Confirms MFA setup by verifying a TOTP code from the user's authenticator app.
        /// Returns recovery codes on success.
        /// </summary>
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] MfaCodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { error = "Verification code is required." });

            var (recoveryCodes, error) = await _mfaService.ConfirmSetupAsync(GetUserId(), request.Code);

            if (error != null)
                return BadRequest(new { error });

            return Ok(new
            {
                message = "MFA has been enabled successfully.",
                recoveryCodes
            });
        }

        // =============================================
        // POST /api/mfa/disable
        // =============================================
        /// <summary>
        /// Disables MFA for the authenticated user. Requires password confirmation.
        /// </summary>
        [HttpPost("disable")]
        public async Task<IActionResult> Disable([FromBody] MfaDisableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Password))
                return BadRequest(new { error = "Password is required to disable MFA." });

            var (success, error) = await _mfaService.DisableAsync(GetUserId(), request.Password);

            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = "MFA has been disabled." });
        }
    }

    // =============================================
    // REQUEST MODELS
    // =============================================

    public class MfaCodeRequest
    {
        public string Code { get; set; }
    }

    public class MfaDisableRequest
    {
        public string Password { get; set; }
    }
}
